using System;
using UnityEngine;

namespace OpenGS
{
    [Serializable]
    public struct ReplayFrame
    {
        public int tick;
        public Vector2 aimWorldPosition;
        public float horizontal;
        public float vertical;
        public bool firePressed;
        public bool fireJustPressed;
        public bool reloadJustPressed;
        public bool swapWeaponJustPressed;
        public bool dropWeaponJustPressed;
        public bool jumpJustPressed;
        public bool sitJustPressed;
        public bool lieDownJustPressed;
        public int instantItemSlotJustPressed;
        public bool jumpPressed;
        public bool boosterPressed;
    }

    [Serializable]
    public sealed class ReplayRecording
    {
        public int formatVersion = 1;
        public string gameVersion = string.Empty;
        public string mapId = string.Empty;
        public int seed;
        public float fixedDeltaTime = 1f / 60f;
        public ReplayFrame[] frames = Array.Empty<ReplayFrame>();
    }
}
