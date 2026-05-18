# OpenGSR MCP Usage

This project exposes the Unity Editor MCP server at `127.0.0.1:51234`.

## 1. Start The Unity Server

In Unity Editor, use:

- `OpenGSR > MCP > Start Server`

The server is implemented in:

- [`Assets/Editor/MCP/MCPServer.cs`](X:/dev/OpenGSR/Assets/Editor/MCP/MCPServer.cs)

## 2. Use It Directly

The Unity server speaks newline-delimited JSON-RPC over TCP.

Example request:

```json
{"jsonrpc":"2.0","id":1,"method":"list_tools","params":{}}
```

Useful requests:

- `list_tools`
- `get_scene_hierarchy`
- `find_game_objects`
- `get_game_object_info`
- `create_game_object`
- `save_scene`
- `open_scene`
- `set_play_mode`
- `get_console_logs`

The authoritative tool list is returned by `list_tools`.

## 3. Use It From Stdio-Based MCP Clients

The repo includes a bridge script:

- [`mcp_bridge.py`](X:/dev/OpenGSR/mcp_bridge.py)

This bridge launches as a stdio MCP server and forwards requests to the Unity TCP server.

Example config for Claude Code or other stdio MCP clients:

```json
{
  "unity-editor": {
    "command": "python",
    "args": ["-u", "X:\\dev\\opengsr\\mcp_bridge.py"],
    "env": {}
  }
}
```

On Windows, prefer an absolute path to `mcp_bridge.py`. Some MCP launchers do not start the process with the project root as the working directory, and `python -u mcp_bridge.py` will exit before replying to `initialize` if the relative path cannot be resolved.

The bridge expects Unity to already be running with the MCP server enabled.

The repo already includes a ready-to-use config file:

- [`claude_mcp.json`](X:/dev/OpenGSR/claude_mcp.json)
- [`codex_mcp.json`](X:/dev/OpenGSR/codex_mcp.json)

## 4. Use It From Codex

If your Codex environment accepts a local JSON tool config, point it at:

- [`codex_mcp.json`](X:/dev/OpenGSR/codex_mcp.json)

That file launches:

- `python -u X:\dev\opengsr\mcp_bridge.py`

Which forwards to the Unity TCP server on:

- `127.0.0.1:51234`

## 5. Tool List

These are the tools exposed by the Unity server:

- `get_scene_hierarchy`
- `get_game_object_info`
- `find_game_objects`
- `create_game_object`
- `delete_game_object`
- `set_transform_position`
- `set_transform_rotation`
- `set_transform_scale`
- `add_component`
- `remove_component`
- `set_parent`
- `set_active`
- `duplicate_game_object`
- `set_property`
- `rename_game_object`
- `set_tag`
- `set_layer`
- `set_static_flags`
- `select_and_frame`
- `find_assets`
- `get_asset_info`
- `instantiate_prefab`
- `save_prefab`
- `create_material`
- `create_folder`
- `create_script`
- `open_scene`
- `get_project_structure`
- `save_scene`
- `get_all_scenes`
- `set_active_scene`
- `find_objects_by_component`
- `batch_set_property`
- `unpack_prefab`
- `revert_prefab_overrides`
- `duplicate_asset`
- `delete_asset`
- `move_asset`
- `create_ui_element`
- `create_light`
- `create_camera`
- `set_play_mode`
- `get_all_tags`
- `get_all_layers`
- `set_material_property`
- `set_renderer_material`
- `get_asset_dependencies`
- `create_physics_material`
- `create_particle_system`
- `create_audio_source`
- `create_animator_controller`
- `refresh_and_compile`
- `build_project`
- `get_console_logs`

## 6. Protocol Notes

- MCP clients should send `initialize` first.
- After initialization, they should send `notifications/initialized`.
- The bridge supports `tools/list` and `tools/call`.
- Unsupported MCP methods return a JSON-RPC `Method not found` error.

## 7. References

- [`PROTOCOL.md`](X:/dev/OpenGSR/PROTOCOL.md)
- [`claude_mcp.json`](X:/dev/OpenGSR/claude_mcp.json)
- [`codex_mcp.json`](X:/dev/OpenGSR/codex_mcp.json)
