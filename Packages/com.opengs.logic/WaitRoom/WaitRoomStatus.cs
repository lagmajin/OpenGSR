using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public enum eReady
    {
        Ready,
        UnReady
    }

    public class WaitRoomStatus
    {
        public string RoomId { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public EGameMode GameMode { get; set; } = EGameMode.Unknown;
        public bool IsPlaying { get; set; }
        public int PlayerCount { get; set; }
        public int Capacity { get; set; }
        public eReady ReadyState { get; set; } = eReady.UnReady;

        public bool IsReady => ReadyState == eReady.Ready;
    }
}
