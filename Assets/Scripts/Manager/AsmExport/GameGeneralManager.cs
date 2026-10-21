using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Newtonsoft.Json;
using System.IO;

using OpenGSCore;
//using UnityEditor.SceneManagement;


#pragma warning disable 0414

namespace OpenGS
{
    public enum GameState
    {
        Start,
        Prepare,
        Playing,
        End
    }





    public interface IGameGeneralManager
    {

    }




    public class GameGeneralManager
    {
        private const string SaveFileName = "game_general_state.json";

        private static GameGeneralManager instance = new GameGeneralManager();

        internal static void SetSharedInstance(GameGeneralManager shared)
        {
            if (shared != null)
            {
                instance = shared;
            }
        }

        internal static void ResetSharedInstance()
        {
            instance = new GameGeneralManager();
        }


        public bool IsOnlineGameMode { get; set; } = false;

        public static GameGeneralManager GetInstance
        {
            get
            {
                return instance;
            }

        }

        /// <summary>
        /// The player this client is playing as.
        /// <para>
        /// It used to be created inside a block that was commented out, so every
        /// scene that asked who the local player was got nothing back and had to
        /// guess. It is kept here so a wait room and a match agree on one player
        /// rather than each making its own.
        /// </para>
        /// </summary>
        public PlayerWaitRoomInfo MyPlayerInfo { get; private set; }

        /// <summary>
        /// Creates the local player's wait room entry under the given name.
        /// <para>
        /// The identity is rebuilt rather than reused, so a new wait room is not
        /// entered by a player id the previous one already handed to the server.
        /// </para>
        /// </summary>
        public void CreatePlayerWaitRoomInfo(string username)
        {
            var name = username?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "Player";
            }

            MyPlayerInfo = new PlayerWaitRoomInfo(name)
            {
                IsAlive = true
            };

            // The account is the authority on who is logging in, so an online
            // client keeps the id the server gave it rather than a fresh one.
            var accountName = AccountManager.Instance?.CurrentProfile?.AccountName;
            if (!string.IsNullOrWhiteSpace(accountName))
            {
                MyPlayerInfo.GlobalID = accountName;
                MyPlayerInfo.Name = accountName;
            }
        }

        /// <summary>
        /// Writes the client's own state so the next run can pick it up.
        /// <para>
        /// It used to do nothing, so the state a session ended with was gone the
        /// moment the process did, and HasBeforeLoginData answered false forever
        /// because there was never anything to find.
        /// </para>
        /// </summary>
        public void Save()
        {
            JsonStorage.Save(SaveFileName, new GeneralState
            {
                OnlineGameMode = IsOnlineGameMode,
                Player = MyPlayerInfo
            });
        }

        /// <summary>
        /// Reads back what Save wrote. A missing or unreadable file leaves the
        /// current state alone rather than clearing a session that is running.
        /// </summary>
        public void Load()
        {
            if (!JsonStorage.TryLoad(SaveFileName, out GeneralState state) || state == null)
            {
                return;
            }

            IsOnlineGameMode = state.OnlineGameMode;
            if (state.Player != null)
            {
                MyPlayerInfo = state.Player;
            }
        }

        /// <summary>
        /// Whether there is saved state from an earlier session.
        /// </summary>
        public bool HasBeforeLoginData()
        {
            return JsonStorage.Exists(SaveFileName);
        }

        /// <summary>
        /// Restores the saved state. The file is left in place, so a failed login
        /// does not throw away the name the player would want typed back in.
        /// </summary>
        public void BeforeLoginData()
        {
            Load();
        }

        [System.Serializable]
        private class GeneralState
        {
            public bool OnlineGameMode;
            public PlayerWaitRoomInfo Player;
        }
    }


}
