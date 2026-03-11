using Newtonsoft.Json.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenGS
{
    /// <summary>
    /// オンライン（TCPマルチプレイ）版のリザルト画面。
    /// GeneralServerNetworkManager から MatchResult イベントの JSON を受け取り、
    /// その勝敗データに基づいて画面を表示・次のウェイトルームへ戻る。
    /// </summary>
    public class OnlineResultScene : AbstractResultScene, INetworkManagerScript
    {
        private GeneralServerNetworkManager networkManager;
        
        [Header("UI Manager")]
        public AbstractMatchResultUIManager resultUIManager;

        protected override void Start()
        {
            base.Start();

            // DIコンテナ等からネットワークマネージャを取得
            networkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();

            if (networkManager != null)
            {
                // データ受信時の処理を購読（UIスレッドで動くように設定）
                networkManager.DataReceivedStream
                    .ObserveOnMainThread()
                    .Subscribe(OnDataReceived)
                    .AddTo(this);
                    
                networkManager.Subscribe(this);

                // もし既にリザルトデータが届いていれば、即座に表示処理を行う
                if (networkManager.LastMatchResult != null)
                {
                    OnDataReceived(networkManager.LastMatchResult);
                }
            }
            else
            {
                Debug.LogWarning("GeneralServerNetworkManager が見つかりません。オンライン結果を受け取れません。");
                // デバッグ用に仮の描画を行うなどの代替処理をここに書いてもよい
            }
        }

        private void OnDestroy()
        {
            if (networkManager != null)
            {
                networkManager.UnSubscribe(this);
            }
        }

        /// <summary>
        /// TCPネットワークからデータが入ってくるたびに呼ばれる
        /// </summary>
        public void ParseMessageFromGeneralServer(JObject json)
        {
        }

        public void ParseMessageFromMatchServer(JObject json)
        {
            // 使わない
        }

        /// <summary>
        /// Rx ストリーム経由で受信したJSONデータ
        /// </summary>
        private void OnDataReceived(JObject json)
        {
            if (json == null) return;
            
            var messageType = json["MessageType"]?.ToString();
            
            if (messageType == "MatchResult")
            {
                // 勝ったチーム名を抽出
                string winningTeam = json["WinningTeam"]?.ToString() ?? "Draw";
                // 自分の所属チームを抽出
                string myTeam = json["MyTeam"]?.ToString() ?? "Spectator";

                // UI更新 (Win / Lose)
                ShowResult(winningTeam, myTeam);

                // JSONから各プレイヤーの戦績リストをパースしてUIに流す
                var playersArray = json["Players"] as JArray;
                if (playersArray != null && resultUIManager != null)
                {
                    var parsedData = new System.Collections.Generic.List<PlayerMatchResultData>();
                    foreach (var pToken in playersArray)
                    {
                        var p = pToken as JObject;
                        if (p == null) continue;

                        parsedData.Add(new PlayerMatchResultData()
                        {
                            PlayerId = p["PlayerId"]?.ToString() ?? "",
                            PlayerName = p["Name"]?.ToString() ?? "Unknown",
                            Team = p["Team"]?.ToString() ?? "None",
                            Kills = p["Kills"]?.ToObject<int>() ?? 0,
                            Deaths = p["Deaths"]?.ToObject<int>() ?? 0,
                            Score = p["Score"]?.ToObject<int>() ?? 0
                        });
                    }
                    resultUIManager.UpdateResultList(parsedData);
                }
            }
        }

        protected override void GoToNextScene()
        {
            // オンライン版は、結果確認後オンラインのウェイトルームに戻る
            // 正しいオンラインウェイトルームのシーン名は OnlineWaitRoom
            SceneManager.LoadScene("OnlineWaitRoom");

            // リザルトデータをクリア（次の試合のために）
            if (networkManager != null)
            {
                networkManager.ClearLastMatchResult();
            }
        }

        public void TestFunc() {}
        public void ParseNetworkMatchMessageFromServer(JObject json) {}
        public void OnConnected() {}
        public void OnDisconnected() {}
    }
}
