#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Antlr4.Runtime.Tree.Pattern;
using OpenGSCore;
using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public interface IMatchSubscriber
    {

        void OnMatchEnd()
        {
            Console.WriteLine("[Default] MatchEnd: Game has ended.");
        }



        void OnReceiveMatchResult(MatchResult eventData)
        {
            //Console.WriteLine($"[Default] MatchResult: Winner={eventData.Winner}, Score={eventData.Score}");
        }

    }

    public interface IMatchPublisher
    {

    }
    
    

    public class MatchRoomEventBus : AbstractEventBus
    {
        private MatchRoom? room;

        public event Action? OnLoadingStarted;
        public event Action? OnGameStarted;
        public event Action<EFieldItemType, int>? OnItemSpawned;
        public event Action? OnItemDespawned;
        public event Action? OnGameEnded;
        public event Action<JObject>? OnGameEndedWithResult;
        
        public MatchRoomEventBus()
        {
            Console.WriteLine("Match RoomEventBus");

        }

        public void setMatchRoom(MatchRoom room)
        {
            this.room = room;
        }
        /*
        public void setMatchRoomManager(MatchRoomManager manager)
        {
            this.roomManager = manager;
        }

        */
        public void setNetworkManager()
        {
            
        }
        
        
        
        public void PublishLoadingStart()
        {
            Console.WriteLine("LoadingStart");
            OnLoadingStarted?.Invoke();
        }

        public void PublishGameStart()
        {
            Console.WriteLine("GameStart");
            OnGameStarted?.Invoke();


            //Publish("GameStart", null);
        }

        // ゲーム終了イベントを発行
        public void PublishGameEnd()
        {
            OnGameEnded?.Invoke();
        }

        public void PublishGameEndWithResult(JObject result)
        {
            OnGameEndedWithResult?.Invoke(result);
        }

        public void PublishMatchResult(MatchResult result)
        {
            
            
        }

        public void PublishGameUpdateFromClient()
        {
            
        }

        public void PublishItemSpawn(EFieldItemType type, int spawnPointId)
        {
            OnItemSpawned?.Invoke(type, spawnPointId);
        }

        public void PublishItemDespawn()
        {
            OnItemDespawned?.Invoke();
        }
        
        
       
    }
}

