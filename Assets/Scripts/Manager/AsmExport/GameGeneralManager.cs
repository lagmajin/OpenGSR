
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


        public void CreatePlayerWaitRoomInfo(string username)
        {

            /*
            if (info == null)
            {

                var info = new PlayerWaitRoomInfo();


                MyPlayerInfo = info;
            }

            */
        }



        public void Save()
        {

        }

        public void Load()
        {

        }

        public bool HasBeforeLoginData()
        {

            return false;
        }

        public void BeforeLoginData()
        {

        }




    }


}
