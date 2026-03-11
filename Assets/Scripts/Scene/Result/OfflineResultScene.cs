using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenGS
{
    /// <summary>
    /// オフライン（シングルプレイ）用リザルト画面。
    /// ネットワークには一切接続せず、ローカルの GameManager や SessionData から直接情報を読み出す。
    /// </summary>
    public class OfflineResultScene : AbstractResultScene
    {
        protected override void Start()
        {
            base.Start();

            // ※ TODO:
            // 実際は ScoreManager や GameManager.Instance など、ローカルメモリに残された
            // 前の試合記録を参照して変数を取り出します。
            
            // 例: (仮実装)
            string winningTeam = "Red"; 
            string myTeam = "Red";      

            // オフライン用プレイヤーやボットの戦績ランキングもここで組み立てる
            
            // 即座にUIを更新してファンファーレ等を開始する
            ShowResult(winningTeam, myTeam);
        }

        protected override void GoToNextScene()
        {
            // オフライン版は、通常のルームマップ選択やメインメニューに帰る
            // ※ TODO: 適当なシーン名 ("WaitRoom" Or "MapSelect") に書き直してください
            SceneManager.LoadScene("WaitRoom");
        }
    }
}
