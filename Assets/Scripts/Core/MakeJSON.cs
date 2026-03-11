

using Newtonsoft.Json.Linq;

using OpenGSCore;

namespace OpenGS
{
    public class MakeJSON
    {

        public static JObject CreateNewRoomRequest(string roomName="",int maxPlayer=8,EGameMode mode=EGameMode.TeamDeathMatch)
        {
            var json = new JObject();

            json["Type"] = "CreateNewRoom";
            json["GameMode"] = mode.ToString();
            json["MaxPlayer"]=maxPlayer;

            return json;
        }

        public static JObject EnterRoomRequest()
        {
            var json = new JObject();

            json["Type"] = "EnterRoom";
            json["RoomNum"] = "";
            json["PlayerID"]="";

            return json;
        }

        public static JObject ExitRoomRequest(int roomNum)
        {
            var json = new JObject();

            json["Type"] = "ExitRoom";
            json["RoomNum"] = roomNum.ToString();
            json["RoomID"] = "";

            return json;
        }

        public static JObject AskAllRoomsRequest()
        {
            var json = new JObject();
            json["Type"] = "AllRoomsRequest";

            return json;
        }

        public static JObject AskPlayerStatusRequest(string playerID)
        {
            var json = new JObject();

            json["Type"] = "PlayerStatusRequest";
            json["PlayerID"] = playerID;

            return json;

        }

        public static JObject AskMatchServerReeust()
        {
            var json = new JObject();
            json["Type"] = "MatchServerRequest";


            return json;

        }

        public static JObject aaa()
        {
            var json = new JObject();

            return json;
        }

        public static JObject bbb()
        {
            var json = new JObject();

            return json;
        }



    }
}
