using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;




namespace OpenGSCore
{
    public interface IAbstractMatchSetting
    {

    }
    public class AbstractMatchSetting : IAbstractMatchSetting
    {

        //public bool TeamBalance { get; set; } = true;
        private int maxPlayerCount = 0;
        private EGameMode mode = EGameMode.Unknown;
        private int matchTimeMSec = 0;

        private float damageRate = 1.0f;
        private float boosterRate = 1.0f;
        private bool timeLimit = false;

        private bool allowOvertime = false;

        private bool locked = false;

        public int MaxPlayerCount { get => maxPlayerCount; set => Set(ref maxPlayerCount, value); }
        public EGameMode Mode { get => mode; set => Set(ref mode, value); }
        public int MatchTimeMSec { get => matchTimeMSec; set => Set(ref matchTimeMSec, value); }

        public float DamageRate { get => damageRate; set => Set(ref damageRate, value); }
        public float BoosterRate { get => boosterRate; set => Set(ref boosterRate, value); }
        public bool TimeLimit { get => timeLimit; set => Set(ref timeLimit, value); }

        public bool AllowOvertime { get => allowOvertime; set => Set(ref allowOvertime, value); }

        /// <summary>
        /// Whether these settings have been frozen.
        /// </summary>
        public bool IsLocked => locked;

        /// <summary>
        /// Freezes the settings so the rest of the match cannot change them.
        /// <para>
        /// The rule is built from these once, when the room is made, and reads
        /// them again when it decides whether the match is over. A setting changed
        /// in between means the rule is judging the match by limits the room was
        /// not started under, so the room and the scoreboard can disagree about
        /// the same match. Locking is one way and does not undo itself, because a
        /// caller that has to be able to unlock would eventually unlock mid match.
        /// </para>
        /// <para>
        /// Locking twice is not an error; the settings are already frozen.
        /// </para>
        /// </summary>
        public void LockSettings()
        {
            locked = true;
        }

        /// <summary>
        /// Writes a setting unless the settings are frozen.
        /// <para>
        /// Derived settings have to write theirs through here as well. A lock the
        /// derived types can walk around is worse than none: the room would look
        /// frozen while its kill limit and match length still moved.
        /// </para>
        /// </summary>
        protected void Set<T>(ref T field, T value)
        {
            if (locked)
            {
                return;
            }

            field = value;
        }

        public AbstractMatchSetting(EGameMode gamemode, int maxplayerCount, bool teamBalance = true, bool hasTimeLimit_ = false)
        {
            Mode = gamemode;
            MaxPlayerCount = maxplayerCount;
            TimeLimit = hasTimeLimit_;
        }

        public AbstractMatchSetting(in EGameMode gamemode, int maxplayerCount, bool teamBalance = true, bool hasTimeLimit_ = false, int timeLimit = 1000)
        {
            Mode = gamemode;
            MaxPlayerCount = maxplayerCount;
            TimeLimit = hasTimeLimit_;
            MatchTimeMSec = timeLimit;
        }

        public AbstractMatchSetting()
        {

        }

        public AbstractMatchSetting(EGameMode gamemode, int maxplayerCount)
        {
            Mode = gamemode;
            MaxPlayerCount = maxplayerCount;
        }

        public virtual JObject ToJson()
        {
            var result = new JObject();

            //result["TeamBalance"] = TeamBalance.ToString();
            result["GameMode"] = Mode.ToString();
            result["MaxPlayerCount"] = MaxPlayerCount.ToString();
            result["AllowOvertime"] = AllowOvertime;


            //Dictionary<string, JToken> dictionary = result;





            return result;
        }




    }


}
