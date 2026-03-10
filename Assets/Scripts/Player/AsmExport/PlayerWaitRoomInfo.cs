using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


#pragma warning disable 0414

namespace OpenGS
{

    /*
    public enum EPlayerCharacter
    {
        Ami,
        Yumi,
        Jack,
        Jackle,
        Misty,
        Liu,
        Mary,
        Wolf,
        Wyburn,
        Seoul,
        LittleJ,
        Shue
    }

    */

    [JsonObject]
    public class PlayerWaitRoomInfo
    {

        [JsonProperty("TotalKill")]
        int totalKill = 0;
        [JsonProperty("TotalDeath")]
        int totalDeath = 0;
        [JsonProperty("TotalMatch")]
        int totalMatch = 0;


        [JsonProperty("Name")]
        public string Name { get; set; }

        public bool IsAlive { get; set; }
        public string LocalID { get; set; }
        public string GlobalID { get; set; }

        public PlayerWaitRoomInfo()
        {
            LocalID = System.Guid.NewGuid().ToString("N");
        }
        public PlayerWaitRoomInfo(string name)
        {

        }

        public PlayerWaitRoomInfo(string name, string id)
        {
            Name = name;
            //Id = id;

        }

        public void ResetUUID()
        {

        }

        public void AddKill(int i = 1)
        {
            totalKill += i;

        }

        public void AddDeath(int i = 1)
        {
            totalDeath += i;
        }



    }
}
