using System.Collections.Generic;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// キルログ全体の管理を行うコンポーネント。
    /// 画面右上などに配置した VerticalLayoutGroup の親オブジェクトにアタッチする想定。
    ///
    /// 【使用方法】
    ///   KillLogManager.Instance.AddLog("キルした人", "キルされた人", [武器スプライト]);
    ///
    /// 【Prefab 構成】
    ///   KillLogManager (このスクリプト)
    ///    ├─ VerticalLayoutGroup (下から上に追加されるような設定)
    ///    ├─ Content Size Fitter (縦幅に合わせて伸縮)
    /// </summary>
    [DisallowMultipleComponent]
    public class KillLogManager : SingletonMonoBehaviour<KillLogManager>
    {
        [Header("リスト設定")]
        [SerializeField] private KillLogItem logPrefab; // キルログ1行分のPrefab
        [SerializeField] private int maxLogCount = 5;   // 同時に表示する最大ログ数

        private readonly Queue<KillLogItem> activeLogs = new();

        /// <summary>
        /// キル時のログを新しく追加する。
        /// </summary>
        /// <param name="killerName">キルしたプレイヤー名</param>
        /// <param name="victimName">倒されたプレイヤー名</param>
        /// <param name="weaponSprite">使用した武器のアイコン(オプショナル)</param>
        /// <param name="isKillerMe">キラーが自分かどうか(色を黄色や青に変えたい場合等)</param>
        /// <param name="isVictimMe">倒されたのが自分かどうか</param>
        public void AddLog(string killerName, string victimName, Sprite weaponSprite = null, bool isKillerMe = false, bool isVictimMe = false)
        {
            if (logPrefab == null)
            {
                Debug.LogWarning("KillLogManager: logPrefab がアサインされていません。");
                return;
            }

            // 新しいログを生成 (VerticalLayoutGroup等を使っているなら子として追加)
            var newLog = Instantiate(logPrefab, transform);
            
            // 例: 自分関連なら色を変える。通常はキラー青/味方色、敵赤など
            Color killerColor = isKillerMe ? Color.cyan : Color.white;
            Color victimColor = isVictimMe ? Color.red : Color.gray;

            newLog.Setup(killerName, victimName, weaponSprite, killerColor, victimColor);

            activeLogs.Enqueue(newLog);

            // 制限数を超えたら一番古いログにフェードアウト命令を出す
            if (activeLogs.Count > maxLogCount)
            {
                var oldestLog = activeLogs.Dequeue();
                if (oldestLog != null)
                {
                    oldestLog.ForceFadeOut();
                }
            }
        }

        /// <summary>
        /// (内部クリーンアップ)
        /// Destroyされたオブジェクトをキューから取り除きたい時などに使う。
        /// </summary>
        private void Update()
        {
            // すでにDestroyされたものがキューの先頭にある場合は破棄しておく
            while (activeLogs.Count > 0 && activeLogs.Peek() == null)
            {
                activeLogs.Dequeue();
            }
        }
    }
}
