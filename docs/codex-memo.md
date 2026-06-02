# Codex Memo

## Current Focus

- Unity project: `OpenGS`
- UnityMCP HTTP server: `http://127.0.0.1:27184`
- Main goal: keep the server usable for local editor automation

## Confirmed Working

- `GET /health`
- `POST /execute_code`
- `POST /read_logs`
- `POST /browse_hierarchy`
- `POST /capture_screenshot`
- `POST /play_mode`
- `POST /command`
- `POST /inspect`
- `GET /resource?name=assemblies`
- `GET /resource?name=packages`

## Important Fixes Applied

- Switched UnityMCP from git dependency to local package in `Packages/manifest.json`
- Removed invalid DLLs from the UnityMCP package copy
- Guarded `McpSettingsProvider.OnActivate` so GUI initialization does not throw
- Patched the project MCP server to avoid fatal port-conflict startup exceptions
- Added sprite usage auditing in `Assets/Editor/SpriteSceneUsageReport.cs`
- The sprite audit now reports both valid sprite references and broken SpriteRenderer/UI Image links
- Added sprite reference repair helper in `Assets/Editor/SpriteReferenceRepairWindow.cs`
- Added sprite sheet auto-slice helper in `Assets/Editor/SpriteSheetAutoSliceWindow.cs`
- Added an Editor-only debug dashboard in `Assets/Editor/DebugDashboardWindow.cs`
- The debug dashboard captures Unity console logs, PlayerRegistry state, and recent GameEventBroker events
- The debug dashboard now includes a Network tab for general server, match RUDP, ClientNetworkManager, and WaitRoomNetworkManager status
- Sprite sheet auto-slice menu: `OpenGSR/Tools/Sprite Sheet Auto Slice`
- The auto-slice helper can preview or apply Multiple import settings from selected textures or folders
- It supports transparency-island slicing, fixed grid divide mode, and auto grid detection from transparent separators
- It can skip textures that are already imported as Multiple
- It writes a markdown report to `Assets/Reports/SpriteSheetAutoSliceReport.md`

## Useful Paths

- `Packages/manifest.json`
- `Packages/jp.shiranui-isuzu.unity-mcp/`
- `Assets/Editor/MCP/MCPServer.cs`
- `Assets/Editor/SpriteSceneUsageReport.cs`
- `Assets/Editor/SpriteReferenceRepairWindow.cs`
- `Logs/Editor.log`
- `docs/codex/main-branch-development.md`
- `docs/codex/workstream-organization.md`
- `docs/codex/class-function-dictionary.md`
- `docs/codex/class-index.md`
- `docs/codex/generate-class-index.ps1`
- `docs/codex/playeragent-kinematic-movement.md`
- `docs/codex/grenade-types.md`
- `docs/codex/slot-definitions.md`
- `docs/codex/weapon-stats.md`

## Notes

- Use `name=assemblies` and `name=packages` for `/resource`
- `inspect` works with `instanceId` or `gameObjectPath`
- `command` uses `command: prefix.action`
- Main-dev marker lives at `%USERPROFILE%\.codex-mainpc`
- If the marker file exists, this PC is treated as the main development machine and may push directly to `main`
- If the marker file does not exist, assume a sub machine and use a short-lived development branch first
- Sprite audit menu: `OpenGSR/Tools/Sprite Scene Usage Report`
- Sprite repair menu: `OpenGSR/Tools/Sprite Reference Repair`
- Sprite auto-slice menu: `OpenGSR/Tools/Sprite Sheet Auto Slice`
- Direct main-branch development notes: `docs/codex/main-branch-development.md`
- Workstream organization notes: `docs/codex/workstream-organization.md`
- Class / function dictionary: `docs/codex/class-function-dictionary.md`
- Class index: `docs/codex/class-index.md`
- Class index generator: `docs/codex/generate-class-index.ps1`
- PlayerAgent scripted movement notes: `docs/codex/playeragent-kinematic-movement.md`
- Grenade type notes: `docs/codex/grenade-types.md`
- Slot definition notes: `docs/codex/slot-definitions.md`
- PlayerAgent now derives jump velocity from `jumpHeight` and uses coyote time / jump buffer for scripted movement
