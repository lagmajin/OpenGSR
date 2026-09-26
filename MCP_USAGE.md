# OpenGSR MCP Usage

## Local debug MCP server

`debug_mcp_bridge.py` is a client-side, read-only MCP server. It is started
over stdio and reads the Unity Editor MCP server through `127.0.0.1:51234`.
It exposes `debug_snapshot`, `debug_logs`, and `debug_scene`.

## Recommended connection: Codex ↔ Unity Editor

Use the `unity-http` MCP server. It is the verified connection path for this
project and exposes the running Unity Editor through a loopback HTTP endpoint.

### One-time setup

Run the following once on the machine that runs Codex:

```powershell
codex mcp add unity-http -- "C:\Users\lagma\AppData\Local\Programs\Python\Python314\python.exe" -u "X:\Dev\OpenGS_Workspace\OpenGSR\mcp_http_bridge.py" --base-url http://127.0.0.1:27182
```

Restart Codex or open a new task after adding the server. Confirm the server is
listed with:

```powershell
codex mcp get unity-http
```

### Daily workflow

1. Open the `OpenGSR` project in Unity.
2. The `jp.shiranui-isuzu.unity-mcp` package starts its local server at
   `http://127.0.0.1:27182` automatically.
3. Start Codex or open a new task. Codex launches `mcp_http_bridge.py` as the
   `unity-http` MCP server.
4. Ask Codex to use Unity MCP, for example: "Unity MCPでシーン階層とConsoleエラーを確認して".

### Connection check

With Unity open, this command must return a successful response:

```powershell
Invoke-RestMethod http://127.0.0.1:27182/health
```

If it does not, verify that the project is open in Unity and that no other
process has claimed port `27182`.

### Tools available through `unity-http`

- Scene hierarchy and component inspection
- Unity Console log reads
- Play Mode control
- Game/Scene/Editor screenshots
- C# execution inside the Editor

`unity-http` is loopback-only (`127.0.0.1`) and cannot be reached from the
network.

## Legacy TCP bridge

The repository also contains a previous TCP JSON-RPC bridge at
`127.0.0.1:51234` (`mcp_bridge.py` and `Assets/Editor/MCP/MCPServer.cs`). Keep
it only for compatibility; use `unity-http` for new Codex work.
