# Shared logic package

`Packages/com.opengs.logic` is a tracked Unity package in OpenGSR. `OpenGSCore`
is the source of its C# files; `tools/sync_core.ps1` copies those files while
keeping package-only files and Unity `.meta` GUIDs. Do not make this directory
a Git submodule or push package-only commits to `OpenGSCore/main`.

To update the package:

1. Check out the intended `OpenGSCore` commit next to OpenGSR.
2. Run `./tools/sync_core.ps1` from OpenGSR.
3. Review and commit the package files, including new `.meta` files, in OpenGSR.
4. Update the `ref` for the `OpenGSCore` checkout in
   `.github/workflows/unity-core-sync.yml` to that same commit.
5. Run `./tools/sync_core.ps1 -Check` and the relevant builds.

CI checks the committed package against the pinned source commit before
building. On CI, the source checkout is inside OpenGSR, so the workflow passes
`-Source ./OpenGSCore` explicitly.

The former package repository had local commits that were not on
`OpenGSCore/main`. Its complete history was saved as a Git bundle before the
submodule was removed. The package contents are now recorded directly in
OpenGSR; the old commit graph remains available from that bundle for recovery.
