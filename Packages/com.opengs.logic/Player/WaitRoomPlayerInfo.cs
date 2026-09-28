using System;
using System.Collections.Generic;
using System.Text;
using OpenGSCore;



namespace OpenGSCore
{
    public  class WaitRoomPlayerInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsReady { get; set; }
        public bool IsBot { get; set; }
        public ETeam Team { get; set; } = ETeam.NoTeam;

        public WaitRoomPlayerInfo()
        {
        }

        public WaitRoomPlayerInfo(string id, string name)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
        }

    }
}
