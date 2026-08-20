# Embedded WebView

OpenGSR uses `net.gree.unity-webview` for optional news and clan pages.

## Unity setup

1. Let Unity resolve the package from `Packages/manifest.json`.
2. Create or reuse a UI `GameObject` in the lobby or wait-room canvas.
3. Add `OpenGS.UI.EmbeddedWebViewPanel`.
4. Assign `Assets/Settings/EmbeddedWebViewSettings.asset` to `Settings` and set
   the production HTTPS endpoints there. Per-panel `News Url` and `Clan Url`
   override the shared settings when filled.
5. Wire UI Buttons to `OpenNews()`, `OpenClan()`, `Close()`, and optionally
   `Reload()`.

The component creates the native overlay lazily on the first open, so the
WebView is not loaded during gameplay scenes unless a button opens it.

## Content rules

- Only absolute `https://` URLs are accepted.
- News and clan pages should be read-only presentation surfaces.
- Authentication and clan mutations should remain in the native client/API;
  do not put long-lived account secrets in page URLs.
- The web page must be responsive to the configured native margins.

## Platform notes

- Android uses the native Android WebView.
- iOS should use WKWebView; the component enables it.
- Windows uses WebView2. The package currently supplies the x64/x86
  `WebView.dll` native plugins; the target machine still needs the WebView2
  Runtime installed.
- WebView is a 2D overlay. It is not intended to be placed in the 3D world.

## Current scope

This is intentionally a reusable panel rather than a scene-specific prefab.
The production news/clan URLs and the exact lobby buttons are project content
decisions, so they remain configurable in the Unity Inspector.
