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
        public event Action<MatchResult>? OnMatchResult;
        public event Action<JObject>? OnGameUpdateFromClient;

        public MatchRoomEventBus()
        {
            Console.WriteLine("Match RoomEventBus");

        }

        public void setMatchRoom(MatchRoom room)
        {
            this.room = room;
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

        /// <summary>
        /// マッチ結果を発行する。
        /// <para>
        /// ここは以前何もしなかったので、結果イベント購読者は永久に呼ばれなかった。
        /// MatchResult は Status しか持たないので、誰の勝ちかはここには書かない。
        /// 勝者そのもの（Winner、WinningPlayerId 等）は PublishGameEndWithResult の
        /// JObject が持っており、勝敗の全体像はそちらが持つ。
        /// </para>
        /// </summary>
        public void PublishMatchResult(MatchResult result)
        {
            if (result == null)
            {
                return;
            }

            OnMatchResult?.Invoke(result);
        }

        /// <summary>
        /// この部屋の現状スナップショットを購読側に渡す。
        /// <para>
        /// ここがネットワークへ送る経路ではないので、購読者がスナップショットを受け取る
        /// 場所になる。room が設定されていないバスは何もせず、無い状態を null で
        /// 配り回さない。
        /// </para>
        /// </summary>
        public void PublishGameUpdateFromClient()
        {
            if (room == null)
            {
                return;
            }

            OnGameUpdateFromClient?.Invoke(room.ToJSon());
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

