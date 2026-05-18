r"""MCP stdio bridge for Unity Editor MCP Server.
Relays MCP stdio protocol to the Unity Editor TCP JSON-RPC server.

Usage:
    python mcp_bridge.py [--port 51234]

Configure in Claude Code (claude_mcp.json):
    "unity-editor": {
        "command": "python",
        "args": ["X:\\dev\\opengsr\\mcp_bridge.py"]
    }

For opencode, add to CLAUDE.md or tool config:
    tools:
      unity-editor:
        command: python
        args: [X:\dev\opengsr\mcp_bridge.py]
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

    unity = UnityConnection(port)
    next_id = 1

    while True:
        req = read_message()
        if req is None:
            break

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
            uid = next_id
            next_id += 1
            try:
                resp = unity.request('list_tools', {}, uid)
            except UnityUnavailableError as ex:
                respond_error(rid, -32001, str(ex))
                continue

            tools = resp.get('result', [])
            respond(rid, {'tools': normalize_tools(tools)})
        elif method == 'tools/call':
            name = params.get('name', '')
            args = params.get('arguments', {})
            uid = next_id
            next_id += 1
            try:
                resp = unity.request(name, args, uid)
            except UnityUnavailableError as ex:
                respond(rid, {
                    'content': [{
                        'type': 'text',
                        'text': str(ex)
                    }],
                    'isError': True
                })
                continue

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
            break
        else:
            if rid is not None:
                respond_error(rid, -32601, f'Method not found: {method}')

    unity.close()


class UnityUnavailableError(Exception):
    pass


class UnityConnection:
    def __init__(self, port):
        self._port = port
        self._sock = None
        self._rfile = None
        self._wfile = None

    def request(self, method, params, rid):
        self._ensure_connected()

        try:
            self._wfile.write(json.dumps(
                {'jsonrpc': '2.0', 'id': rid, 'method': method, 'params': params}) + '\n')
            self._wfile.flush()
            resp_line = self._rfile.readline()
        except OSError:
            self.close()
            raise UnityUnavailableError(_unity_unavailable_message(self._port))

        if not resp_line:
            self.close()
            raise UnityUnavailableError(_unity_unavailable_message(self._port))

        try:
            return json.loads(resp_line)
        except json.JSONDecodeError as ex:
            raise UnityUnavailableError(f'Unity MCP server returned invalid JSON: {ex}') from ex

    def close(self):
        for handle in (self._rfile, self._wfile):
            try:
                if handle is not None:
                    handle.close()
            except OSError:
                pass
        try:
            if self._sock is not None:
                self._sock.close()
        except OSError:
            pass
        self._sock = None
        self._rfile = None
        self._wfile = None

    def _ensure_connected(self):
        if self._sock is not None:
            return

        try:
            self._sock = socket.create_connection(('127.0.0.1', self._port), timeout=5)
            self._rfile = self._sock.makefile('r', encoding='utf-8')
            self._wfile = self._sock.makefile('w', encoding='utf-8')
        except (ConnectionRefusedError, OSError):
            self.close()
            raise UnityUnavailableError(_unity_unavailable_message(self._port))


def _unity_unavailable_message(port):
    return (f'Unity MCP server not running on port {port}. Make sure Unity Editor is open '
            'with the MCP server active (check OpenGSR > MCP > Start Server in the menu).')


def normalize_tools(tools):
    return [
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


def respond(rid, result):
    if rid is None:
        return
    write_message({'jsonrpc': '2.0', 'id': rid, 'result': result})


def respond_error(rid, code, message):
    write_message(
        {'jsonrpc': '2.0', 'id': rid, 'error': {'code': code, 'message': message}})


def _error(msg):
    sys.stderr.write(f'[unity-mcp] {msg}\n')
    sys.stderr.flush()


def read_message():
    """Read a stdio MCP message.

    Supports standard Content-Length framing and falls back to newline-delimited
    JSON so the bridge remains easy to test manually.
    """
    stdin = sys.stdin.buffer

    while True:
        first_line = stdin.readline()
        if not first_line:
            return None
        if first_line in (b'\n', b'\r\n'):
            continue

        # Manual / legacy compatibility: one JSON object per line.
        if first_line[:1] == b'{':
            try:
                return json.loads(first_line.decode('utf-8').strip())
            except json.JSONDecodeError:
                _error('Invalid JSON received on stdin')
                return None

        header_line = first_line.decode('ascii', errors='replace').strip()
        content_length = None

        while True:
            if header_line:
                name, sep, value = header_line.partition(':')
                if sep and name.lower() == 'content-length':
                    try:
                        content_length = int(value.strip())
                    except ValueError:
                        _error(f'Invalid Content-Length header: {value!r}')
                        return None

            next_line = stdin.readline()
            if not next_line:
                return None
            if next_line in (b'\n', b'\r\n'):
                break
            header_line = next_line.decode('ascii', errors='replace').strip()

        if content_length is None:
            _error('Missing Content-Length header')
            return None

        body = stdin.read(content_length)
        if len(body) != content_length:
            _error('Unexpected EOF while reading MCP message body')
            return None

        try:
            return json.loads(body.decode('utf-8'))
        except json.JSONDecodeError:
            _error('Invalid JSON body received on stdin')
            return None


def write_message(payload):
    body = json.dumps(payload).encode('utf-8')
    stdout = sys.stdout.buffer
    stdout.write(f'Content-Length: {len(body)}\r\n\r\n'.encode('ascii'))
    stdout.write(body)
    stdout.flush()


if __name__ == '__main__':
    main()
