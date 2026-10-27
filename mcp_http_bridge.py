"""MCP stdio bridge for the Unity MCP HTTP server.

Relays MCP stdio protocol to the Unity Editor HTTP API exposed by
jp.shiranui-isuzu.unity-mcp.

Usage:
    python mcp_http_bridge.py [--project-name OpenGS]
    python mcp_http_bridge.py --base-url http://127.0.0.1:27182

By default, the bridge discovers the requested Unity project by checking the
package's local HTTP port range (27182-27199). Use --base-url only to pin one
specific editor instance.
"""

import argparse
import json
import sys
import urllib.parse
import urllib.request
import urllib.error


SUPPORTED_PROTOCOL_VERSION = "2025-03-26"


TOOLS = [
    {
        "name": "unity_health",
        "description": "Get Unity MCP HTTP server status and available handlers.",
        "inputSchema": {"type": "object", "properties": {}, "required": []},
    },
    {
        "name": "unity_resource",
        "description": "Read Unity MCP resources such as packages or assemblies.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "name": {"type": "string", "description": "Resource name, e.g. packages or assemblies."},
                "limit": {"type": "integer"},
                "offset": {"type": "integer"},
                "fields": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Optional field filter.",
                },
            },
            "required": ["name"],
        },
    },
    {
        "name": "unity_browse_hierarchy",
        "description": "Browse the current scene hierarchy.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "limit": {"type": "integer"},
                "offset": {"type": "integer"},
                "fields": {"type": "array", "items": {"type": "string"}},
                "includeInactive": {"type": "boolean"},
                "rootPath": {"type": "string"},
            },
            "required": [],
        },
    },
    {
        "name": "unity_read_logs",
        "description": "Read Unity console logs with optional filtering.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "limit": {"type": "integer"},
                "offset": {"type": "integer"},
                "fields": {"type": "array", "items": {"type": "string"}},
                "type": {"type": "string"},
                "contains": {"type": "string"},
            },
            "required": [],
        },
    },
    {
        "name": "unity_execute_code",
        "description": "Execute C# code inside the Unity Editor.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "code": {"type": "string", "description": "C# code to execute."},
            },
            "required": ["code"],
        },
    },
    {
        "name": "unity_inspect",
        "description": "Read, list, or write object/component data through /inspect.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "action": {"type": "string", "description": "read, list, or write"},
                "instanceId": {"type": "integer"},
                "gameObjectPath": {"type": "string"},
                "component": {"type": "string"},
                "property": {"type": "string"},
                "value": {},
                "limit": {"type": "integer"},
                "offset": {"type": "integer"},
                "fields": {"type": "array", "items": {"type": "string"}},
            },
            "required": ["action"],
        },
    },
    {
        "name": "unity_play_mode",
        "description": "Query or control Unity Play Mode.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "action": {"type": "string", "description": "status, play, stop, pause, unpause, or step"},
            },
            "required": ["action"],
        },
    },
    {
        "name": "unity_command",
        "description": "Invoke a Unity MCP command handler such as console.getLogs or menu.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "command": {"type": "string"},
                "args": {"type": "object", "additionalProperties": True},
            },
            "required": ["command"],
        },
    },
    {
        "name": "unity_capture_screenshot",
        "description": "Capture a screenshot from game, scene, or editor windows.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "view": {"type": "string", "description": "game, scene, inspector, hierarchy, project, console, etc."},
                "width": {"type": "integer"},
                "height": {"type": "integer"},
            },
            "required": [],
        },
    },
]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--base-url",
        help="Use a fixed Unity HTTP endpoint instead of discovering one.",
    )
    parser.add_argument("--project-name", default="OpenGS")
    parser.add_argument("--port-start", type=int, default=27182)
    parser.add_argument("--port-end", type=int, default=27199)
    options = parser.parse_args()
    base_url = options.base_url.rstrip("/") if options.base_url else None
    if options.port_start < 1 or options.port_end > 65535 or options.port_start > options.port_end:
        parser.error("port range must be valid and port-start must not exceed port-end")
    target = (
        f"{base_url} (fixed endpoint)"
        if base_url
        else f"Unity project {options.project_name!r} on 127.0.0.1:{options.port_start}-{options.port_end} (auto-discovered)"
    )

    for line in sys.stdin:
        line = line.strip()
        if not line:
            continue

        try:
            req = json.loads(line)
        except json.JSONDecodeError:
            continue

        rid = req.get("id")
        method = req.get("method", "")
        params = req.get("params", {})

        if method == "initialize":
            respond(rid, {
                "protocolVersion": params.get("protocolVersion", SUPPORTED_PROTOCOL_VERSION),
                "capabilities": {"tools": {}},
                "serverInfo": {"name": "OpenGSR Unity HTTP MCP Bridge", "version": "1.0.0"},
                "instructions": f"Targets {target}.",
            })
        elif method == "notifications/initialized":
            pass
        elif method == "tools/list":
            respond(rid, {"tools": TOOLS})
        elif method == "tools/call":
            name = params.get("name", "")
            arguments = params.get("arguments", {})
            try:
                target_base_url = base_url or discover_base_url(
                    options.project_name,
                    options.port_start,
                    options.port_end,
                )
                result = handle_tool_call(target_base_url, name, arguments)
                respond(rid, {"content": [{"type": "text", "text": format_result(result)}], "isError": False})
            except HttpError as exc:
                respond(rid, {"content": [{"type": "text", "text": format_result(exc.payload)}], "isError": True})
            except Exception as exc:
                respond(rid, {"content": [{"type": "text", "text": str(exc)}], "isError": True})
        elif method == "shutdown":
            respond(rid, None)
        else:
            if rid is not None:
                respond_error(rid, -32601, f"Method not found: {method}")


class HttpError(Exception):
    def __init__(self, payload):
        self.payload = payload
        super().__init__(payload.get("error", {}).get("message", "Unity HTTP request failed"))


def discover_base_url(project_name, port_start, port_end):
    """Find exactly one running Unity MCP server for the requested project."""
    matches = []
    for port in range(port_start, port_end + 1):
        base_url = f"http://127.0.0.1:{port}"
        try:
            request = urllib.request.Request(f"{base_url}/health", method="GET")
            with urllib.request.urlopen(request, timeout=0.35) as response:
                health = json.loads(response.read().decode("utf-8"))
            result = health.get("result", {})
            if health.get("status") == "success" and result.get("project", "").casefold() == project_name.casefold():
                matches.append((port, result.get("clientId", f"{project_name}-{port}")))
        except (OSError, ValueError, urllib.error.URLError):
            continue

    if not matches:
        raise RuntimeError(
            f"Could not find Unity MCP for project {project_name!r} on "
            f"127.0.0.1:{port_start}-{port_end}. Start it from Edit > Preferences > Unity MCP."
        )
    if len(matches) > 1:
        targets = ", ".join(f"{client_id} (port {port})" for port, client_id in matches)
        raise RuntimeError(
            f"Found multiple Unity MCP servers for project {project_name!r}: {targets}. "
            "Set --base-url to select one explicitly."
        )
    return f"http://127.0.0.1:{matches[0][0]}"


def handle_tool_call(base_url, name, arguments):
    if name == "unity_health":
        return http_get(base_url, "/health")
    if name == "unity_resource":
        query = encode_query(arguments)
        return http_get(base_url, "/resource", query)
    if name == "unity_browse_hierarchy":
        return http_post(base_url, "/browse_hierarchy", arguments)
    if name == "unity_read_logs":
        return http_post(base_url, "/read_logs", arguments)
    if name == "unity_execute_code":
        return http_post(base_url, "/execute_code", arguments)
    if name == "unity_inspect":
        return http_post(base_url, "/inspect", arguments)
    if name == "unity_play_mode":
        return http_post(base_url, "/play_mode", arguments)
    if name == "unity_command":
        payload = {"command": arguments["command"]}
        if "args" in arguments:
            payload.update(arguments["args"] or {})
        return http_post(base_url, "/command", payload)
    if name == "unity_capture_screenshot":
        return http_post(base_url, "/capture_screenshot", arguments)
    raise ValueError(f"Unknown tool: {name}")


def encode_query(arguments):
    pairs = []
    for key, value in arguments.items():
        if value is None:
            continue
        if isinstance(value, list):
            for item in value:
                pairs.append((key, str(item)))
        else:
            pairs.append((key, str(value)))
    return urllib.parse.urlencode(pairs)


def http_get(base_url, path, query=None):
    url = f"{base_url}{path}"
    if query:
        url = f"{url}?{query}"
    req = urllib.request.Request(url, method="GET")
    return perform(req)


def http_post(base_url, path, payload):
    body = json.dumps(payload, ensure_ascii=False).encode("utf-8")
    req = urllib.request.Request(
        f"{base_url}{path}",
        data=body,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    return perform(req)


def perform(req):
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            data = json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as exc:
        payload = json.loads(exc.read().decode("utf-8", errors="replace"))
        raise HttpError(payload)
    except urllib.error.URLError as exc:
        raise RuntimeError(f"Could not reach Unity MCP HTTP server: {exc.reason}") from exc

    if data.get("status") == "error":
        raise HttpError(data)
    return data


def format_result(result):
    return json.dumps(result, indent=2, ensure_ascii=False)


def respond(rid, result):
    if rid is None:
        return
    sys.stdout.write(json.dumps({"jsonrpc": "2.0", "id": rid, "result": result}) + "\n")
    sys.stdout.flush()


def respond_error(rid, code, message):
    sys.stdout.write(json.dumps({"jsonrpc": "2.0", "id": rid, "error": {"code": code, "message": message}}) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
