# OpenGSR MCP Usage

## Local debug MCP server

`debug_mcp_bridge.py` is a client-side, read-only MCP server. It is started
over stdio and reads the Unity Editor MCP server through `127.0.0.1:51234`.
It exposes `debug_snapshot`, `debug_logs`, and `debug_scene`.

## Recommended connection: Codex ↔ Unity Editor

Use the `unity-http` MCP server. The bridge discovers the OpenGS Unity Editor
on its loopback port range (`27182`–`27199`) instead of assuming one port.

### One-time setup

Run the following once on the machine that runs Codex:

```powershell
codex mcp add unity-http -- python -u "J:\dev\OpenGS_Workspace\OpenGSR\mcp_http_bridge.py" --project-name OpenGS
```

Restart Codex or open a new task after adding the server. Confirm the server is
listed with:

```powershell
codex mcp get unity-http
```

### Daily workflow

1. Open the `OpenGSR` project in Unity.
2. In **Edit > Preferences > Unity MCP**, confirm the server status is
   listening. If it says not initialized, click **Initialize MCP Server**,
   then **Start Server**. Unity may choose any available port from
   `27182`–`27199`.
   The current package initializer also requires `UNITY_MCP_AUTOSTART=1` at
   Unity launch before `autoStartOnLaunch` takes effect; manual initialization
   and start are the reliable option when that environment variable was not
   set before opening the Editor.
3. Start Codex or open a new task. Codex launches `mcp_http_bridge.py`, which
   discovers the `OpenGS` project by its `/health` response.
4. Ask Codex to use Unity MCP, for example: "Unity MCPでシーン階層とConsoleエラーを確認して".

### Connection check

With the server listening, the configured Codex `unity-http` bridge must
return a successful `unity_health` response for project `OpenGS`. For a direct
manual check, probe the documented port range until `/health` returns
`project: OpenGS`:

```powershell
for ($port = 27182; $port -le 27199; $port++) {
    try {
        $health = Invoke-RestMethod "http://127.0.0.1:$port/health" -TimeoutSec 1
        if ($health.result.project -eq "OpenGS") { $health; break }
    } catch { }
}
```

If it does not, check the server status in Unity Preferences. Another Unity
instance may have selected a different port; the bridge scans the full range.

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
