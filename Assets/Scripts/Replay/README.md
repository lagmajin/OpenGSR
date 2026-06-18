# Replay

This folder contains the replay foundation for OpenGSR.

What is included:
- input capture through the existing `IInputService`
- binary save/load for replay files
- playback switching without touching player code
- a small scene controller for manual start/stop/save/load

How to use:
1. Bind `ReplayInputService` in Zenject through `GameInstaller`.
2. Add `ReplaySessionController` to a scene object for debug control.
3. Call `Start Recording`, play a match, then `Stop Recording And Save`.
4. Later, call `Load Replay Now` to play the recorded input back.
