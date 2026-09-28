#nullable enable
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;


namespace OpenGSCore
{
    //public interface IWaitRoom { }

    //#new
    public partial class WaitRoom
    {
        public MatchRoom? MatchRoomLink { get; set; } = null;
        public string RoomName { get; set; } = "";
        public string RoomId { get; set; }
        public bool NowPlaying { get; private set; } = false;
        public int Capacity { get; set; } = 0;
        public EGameMode GameMode { get; private set; } = EGameMode.Unknown;
        public EMap Map { get; set; } = EMap.Unknown;
        public string Password { get; set; } = string.Empty;
        public int PlayerCount
        {
            get
            {
                lock (lockObject)
                {
                    return Players?.Count ?? 0;
                }
            }
        }

        
        public List<PlayerInfo> OldPlayers { get; set; }
        
        public Dictionary<string,PlayerInfo> Players { get; set; }

        private readonly  Object lockObject = new Object();


        public AbstractMatchRule? MatchRule { get; set; } = null;

        public AbstractMatchSetting? setting = null;
        public WaitRoom(in string roomName, int capacity = 8)
        {
            RoomName = roomName;
            Capacity = capacity;
            RoomId = Guid.NewGuid().ToString();
            Players = new Dictionary<string, PlayerInfo>();
            OldPlayers = new List<PlayerInfo>();
        }
        public WaitRoom(in string name,in string id,int capacity=8)
        {
            RoomName = name;

            RoomId = id ?? Guid.NewGuid().ToString();

            Capacity=capacity;
            Players = new Dictionary<string, PlayerInfo>();
            OldPlayers = new List<PlayerInfo>();
        }

        public void ChangeGameMode(EGameMode mode)
        {
            lock (lockObject)
            {
                // Do not change mode while a game is in progress
                if (NowPlaying) return;

                switch (mode)
                {
                    case EGameMode.DeathMatch:
                        setting = new DeathMatchSetting();
                        break;
                    case EGameMode.TeamDeathMatch:
                        setting = new TDMMatchSetting();
                        break;
                    case EGameMode.CaptureTheFlag:
                        setting = new CaptureTheFlagMatchSetting();
                        break;
                    case EGameMode.Survival:
                        setting = new SuvMatchSetting(Capacity, false);
                        break;
                    case EGameMode.TeamSurvival:
                        setting = new TeamSurvivalMatchSetting();
                        break;
                    default:
                        // fallback to a generic abstract setting
                        setting = new AbstractMatchSetting(mode, Capacity);
                        break;
                }

                // Ensure capacity is in sync with setting if provided
                if (setting != null && setting.MaxPlayerCount > 0)
                {
                    Capacity = setting.MaxPlayerCount;
                }

                GameMode = setting != null ? setting.Mode : mode;

                // A room that is changing mode cannot keep the map it had: the
                // old mode's map has whatever the old mode needs in it, and a
                // capture the flag room pointed at a death match stage has no flag
                // stands on it. The map is a property of the mode, so it is
                // chosen from the mode here rather than left to whoever asks.
                var playable = OpenGSCore.GameMode.DefaultMapFor(GameMode);
                if (playable.HasValue)
                {
                    Map = playable.Value;
                }
            }
        }

        public void AddPlayer(PlayerInfo info)
        {
            lock (lockObject)
            {
                if (info == null) return;

                // initialize collections if necessary
                if (Players == null) Players = new Dictionary<string, PlayerInfo>();
                if (OldPlayers == null) OldPlayers = new List<PlayerInfo>();

                // enforce capacity if set (> 0)
                if (Capacity > 0 && Players.Count >= Capacity && !Players.ContainsKey(info.Id))
                {
                    // room full - ignore
                    return;
                }

                // add or replace
                Players[info.Id] = info;

                // keep a simple history
                OldPlayers.Add(info);
            }
        }

        public bool ContainsPlayer(string id)
        {
            lock (lockObject)
            {
                if (string.IsNullOrWhiteSpace(id) || Players == null)
                {
                    return false;
                }

                return Players.ContainsKey(id);
            }
        }

        public bool TryRemovePlayer(string id, out PlayerInfo? removed)
        {
            lock (lockObject)
            {
                removed = null;
                if (string.IsNullOrWhiteSpace(id) || Players == null)
                {
                    return false;
                }

                if (!Players.TryGetValue(id, out removed))
                {
                    return false;
                }

                Players.Remove(id);
                return true;
            }
        }

        public void AddPlayer(in string id, in string displayName)
        {
            lock (lockObject)
            {
                var info = new PlayerInfo(id, displayName);
                AddPlayer(info);


            }

        }

        public void AddPlayers()
        {
            AddPlayers(OldPlayers);
        }

        // convenience overload to add multiple players
        public void AddPlayers(IEnumerable<PlayerInfo> infos)
        {
            if (infos == null) return;
            lock (lockObject)
            {
                foreach (var p in infos)
                {
                    AddPlayer(p);
                }
            }
        }

        public void AddBotPlayer()
        {
            AddBotPlayer(null);
        }

        public PlayerInfo? AddBotPlayer(string? displayName = null)
        {
            lock (lockObject)
            {
                if (Capacity > 0 && Players.Count >= Capacity)
                {
                    return null;
                }

                var id = $"bot_{Guid.NewGuid():N}";
                var name = displayName ?? $"Bot_{Players.Count + 1}";
                var bot = new PlayerInfo(id, name)
                {
                    IsBot = true
                };

                Players.Add(id, bot);
                OldPlayers.Add(bot);
                return bot;
            }
        }


        public void RemovePlayer(in string id)
        {
            lock (lockObject)
            {
                Players.Remove(id);



            }

        }

        public void RemoveAllPlayers()
        {
            lock(lockObject)
            {
                Players.Clear();

            }

        }

        public void ClearHistory()
        {
            lock (lockObject)
            {
                OldPlayers.Clear();
            }
        }

        public void RemoveAllBotPlayer()
        {
            lock (lockObject)
            {
                var removeKeys = new List<string>();
                foreach (var kv in Players)
                {
                    if (kv.Value != null && kv.Value.IsBot)
                    {
                        removeKeys.Add(kv.Key);
                    }
                }

                foreach (var k in removeKeys)
                {
                    Players.Remove(k);
                }
            }
        }


        public List<PlayerInfo> AllPlayers()
        {
            lock (lockObject)
            {

                var players = new List<PlayerInfo>();


                foreach (var info in Players)
                {
                    players.Add(info.Value);


                }


                return players;
            }
        }

        public void GameStart()
        {
            lock (lockObject)
            {
                if (NowPlaying) return; // already playing
                if (Players == null || Players.Count == 0) return; // no players

                NowPlaying = true;
            }
        }

        /// <summary>
        /// MatchRoom とリンクする（サーバ側から呼ばれる）
        /// </summary>
        public void LinkMatchRoom(MatchRoom matchRoom)
        {
            lock (lockObject)
            {
                MatchRoomLink = matchRoom;
                matchRoom.WaitRoomLink = this;
                NowPlaying = true;
            }
        }

        /// <summary>
        /// <summary>
        /// Releases the wait room when a match never reached the map.
        /// <para>
        /// When the S3 loading handshake times out, the MatchRoom that was
        /// created is discarded, but NowPlaying on the wait room stays true.
        /// CanStartMatch() then keeps returning false, so the room is stuck and
        /// can never start again. This releases it so a retry is possible.
        /// The link is cleared as well, because the MatchRoom on the other side
        /// is being thrown away; leaving it set would keep the room locked.
        /// </para>
        /// </summary>
        public void CancelPendingMatch()
        {
            lock (lockObject)
            {
                var pending = MatchRoomLink;
                MatchRoomLink = null;
                NowPlaying = false;

                if (pending != null)
                {
                    pending.WaitRoomLink = null;
                }
            }
        }

        /// <summary>
        /// ゲーム開始可能かチェック
        /// </summary>
        public bool CanStartMatch()
        {
            lock (lockObject)
            {
                if (NowPlaying) return false;
                if (Players == null || Players.Count == 0) return false;
                return true;
            }
        }

        /// <summary>
        /// 設定を取得（未設定ならデフォルト）
        /// </summary>
        public AbstractMatchSetting GetOrCreateSetting()
        {
            lock (lockObject)
            {
                if (setting == null)
                {
                    setting = new DeathMatchSetting();
                }
                GameMode = setting.Mode;
                return setting;
            }
        }

        /// <summary>
        /// 最初のプレイヤーIDを取得（オーナー用）
        /// </summary>
        public string GetFirstPlayerId()
        {
            lock (lockObject)
            {
                foreach (var kv in Players)
                {
                    return kv.Key;
                }
                return "";
            }
        }

        public void GameIsOver()
        {
            lock (lockObject)
            {
                NowPlaying = false;
                MatchRoomLink = null;

                // Reset player states for the next match
                foreach (var p in Players.Values)
                {
                    p.IsReady = false;
                    p.Kills = 0;
                    p.Deaths = 0;
                }
            }
        }

        public JObject ToJson()
        {
            var result = new JObject();

            var array = new JArray();
            lock (lockObject)
            {
                result["RoomName"] = RoomName;
                result["RoomId"] = RoomId;
                result["NowPlaying"] = NowPlaying;
                result["Capacity"] = Capacity;
                result["GameMode"] = GameMode.ToString();
                result["Map"] = Map.ToString();
                result["HasPassword"] = !string.IsNullOrWhiteSpace(Password);

                if (setting != null)
                {
                    result["Setting"] = setting.ToJson();
                }

                foreach (var p in Players.Values)
                {
                    array.Add(p.ToJson());
                }

                result["Players"] = array;
            }

            return result;
        }
    }
}
