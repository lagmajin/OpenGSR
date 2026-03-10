

using System.Collections.Generic;
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    internal interface IMatchManager
    {

    }

    internal class MatchManagerImpl
    {

    }

    internal class CaptureTheFlagImpl : MatchManagerImpl
    {

    }






    internal class MatchManager
    {
        private MatchManagerImpl imp;

        private IAbstractMatchMainScript mainScriptSubscriber = null;

        //private List<MonoBehaviour> subscriber2 = new();

        private List<IPlayer> subscribePlayers = new();
        //private List<IAbstractMatchMainScript> mainScriptSubscriber=new();
        public static MatchManager Instance { get; } = new();

        private readonly object subscribePlayerLockObject = new();

        public List<PlayerStatus> status;

        public PlayerDatabase MatchPlayerDatabase { get; set; }=new();

        private MatchManager()
        {
            //MatchPlayerInfo
        }

        public void SubscribeEvent(IAbstractMatchMainScript script)
        {
            mainScriptSubscriber = script;

            Debug.Log("<color=blue>SubscribeEvent</color>");

        }

        public void UnSubscribeEvent(IAbstractMatchMainScript script)
        {
            Debug.Log("<color=red>UnSubscribeEvent</color>");

            mainScriptSubscriber = null;

            

        }

        public void SubscribeEvent(IPlayer player)
        {
            lock (subscribePlayerLockObject)
            {
                if (subscribePlayers.Contains(player))
                {
                    subscribePlayers.Add(player);
                }
            }

        }

        public void UnSubscribeEvent(IPlayer player)
        {
            lock (subscribePlayerLockObject)
            {
                subscribePlayers.Remove(player);
            }



        }

        public void SendEvent(AbstractGameEvent ev)
        {

        }

        public void ClearAllSubscribe()
        {
            mainScriptSubscriber = null;
        }

        public void Clear()
        {
            //MatchPlayerDatabase.Clear();

            
        }

    }


}
