"""MCP stdio bridge for Unity Editor MCP Server.
Relays MCP stdio protocol to the Unity Editor TCP JSON-RPC server.

Usage:
    python mcp_bridge.py [--port 51234]

Configure in Claude Code (claude_mcp.json):
    "unity-editor": {
        "command": "python",
        "args": ["mcp_bridge.py"]
    }

For opencode, add to CLAUDE.md or tool config:
    tools:
      unity-editor:
        command: python
        args: [mcp_bridge.py]
"""
import sys
import json
import socket


SUPPORTED_PROTOCOL_VERSION = "2025-03-26"


def main():
    port = 51234
    args = sys.argv[1:]
    for i, a in enumerate(args):
        if a == '--port' and i + 1 < len(args):
            port = int(args[i + 1])
        elif a == '--help':
            print(__doc__)
            return

    try:
        sock = socket.create_connection(('127.0.0.1', port), timeout=5)
    except (ConnectionRefusedError, OSError):
        _error(f'Unity MCP server not running on port {port}. '
               'Make sure Unity Editor is open with the MCP server active '
               '(check OpenGSR > MCP > Start Server in the menu).')
        return

    rfile = sock.makefile('r', encoding='utf-8')
    wfile = sock.makefile('w', encoding='utf-8')
    next_id = 1
    initialized = False

    for line in sys.stdin:
        line = line.strip()
        if not line:
            continue

        try:
            req = json.loads(line)
        except json.JSONDecodeError:
            continue

        rid = req.get('id')
        method = req.get('method', '')
        params = req.get('params', {})

        if method == 'initialize':
            initialized = True
            respond(rid, {
                'protocolVersion': params.get('protocolVersion', SUPPORTED_PROTOCOL_VERSION),
                'capabilities': {
                    'tools': {}
                },
                'serverInfo': {
                    'name': 'OpenGSR Unity MCP Bridge',
                    'version': '1.0.0'
                },
                'instructions': 'Unity Editor must be open with OpenGSR > MCP > Start Server.'
            })
        elif method == 'notifications/initialized':
            pass
        elif method == 'tools/list':
            uid = next_id; next_id += 1
            wfile.write(json.dumps(
                {'jsonrpc': '2.0', 'id': uid, 'method': 'list_tools', 'params': {}}) + '\n')
            wfile.flush()
            resp_line = rfile.readline()
            if not resp_line:
                _error('Unity disconnected')
                return
            resp = json.loads(resp_line)
            tools = resp.get('result', [])
            respond(rid, {
                'tools': [
                    {
                        'name': t['name'],
                        'description': t['description'],
                        'inputSchema': t.get('inputSchema', {
                            'type': 'object',
                            'properties': {},
                            'required': []
                        })
                    }
                    for t in tools
                ]
            })
        elif method == 'tools/call':
            name = params.get('name', '')
            args = params.get('arguments', {})
            uid = next_id; next_id += 1
            wfile.write(json.dumps(
                {'jsonrpc': '2.0', 'id': uid, 'method': name, 'params': args}) + '\n')
            wfile.flush()
            resp_line = rfile.readline()
            if not resp_line:
                _error('Unity disconnected')
                return
            resp = json.loads(resp_line)
            inner = resp.get('result')
            is_error = 'error' in resp
            if is_error:
                inner = resp.get('error', {})
            if not isinstance(inner, str):
                inner = json.dumps(inner, indent=2, ensure_ascii=False)
            respond(rid, {
                'content': [{
                    'type': 'text',
                    'text': inner
                }],
                'isError': is_error
            })
        elif method == 'shutdown':
            respond(rid, None)
        else:
            if rid is not None:
                respond_error(rid, -32601, f'Method not found: {method}')

    sock.close()


def respond(rid, result):
    if rid is None:
        return
    sys.stdout.write(json.dumps(
        {'jsonrpc': '2.0', 'id': rid, 'result': result}) + '\n')
    sys.stdout.flush()


def respond_error(rid, code, message):
    sys.stdout.write(json.dumps(
        {'jsonrpc': '2.0', 'id': rid, 'error': {'code': code, 'message': message}}) + '\n')
    sys.stdout.flush()


def _error(msg):
    sys.stderr.write(f'[unity-mcp] {msg}\n')
    sys.stderr.flush()


if __name__ == '__main__':
    main()
