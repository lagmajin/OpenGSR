# Scene Scripts Organization

## Folder policy
- Root: core scene entry scripts (`TitleScene`, `OnlineLobbyScene`, etc.)
- `Account/`: login and sign-up scenes
- `Result/`: result scene variants
- `SceneController/`: scene input/controller scripts
- `WaitRoom/`: wait room event/server scripts
- `ExportAssets/`: editor/export helper scene script

## Rescue rule
- Copy from old OpenGS with original relative paths.
- Do not overwrite existing files automatically.
- Keep `.meta` files together with script files.
