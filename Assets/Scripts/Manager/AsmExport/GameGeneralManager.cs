
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
        private static GameGeneralManager instance;


        public bool IsOnlineGameMode { get; set; } = false;

        public static GameGeneralManager GetInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameGeneralManager();

                }

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
