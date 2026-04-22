using System;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// PlayerRegistry の死亡イベントを監視し、KillLogManager にキルログを追加するリスナー。
    /// バトルシーンに1つ配置しておくだけで、キルが発生するたびに画面にキルログが流れる。
    ///
    /// 【使い方】
    ///   バトルシーンの Canvas 等に配置するだけ。KillLogManager が同シーンに必要。
    /// </summary>
    [DisallowMultipleComponent]
    public class KillLogEventListener : MonoBehaviour
    {
        private void OnEnable()
        {
            if (PlayerRegistry.Instance == null) return;

            PlayerRegistry.Instance.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            if (PlayerRegistry.Instance == null) return;

            PlayerRegistry.Instance.OnPlayerDied -= HandlePlayerDied;
        }

        /// <summary>
        /// プレイヤーが死亡したときに呼ばれる。
        /// キルしたプレイヤーの情報を取得してキルログに表示する。
        /// </summary>
        private void HandlePlayerDied(AbstractPlayer victim)
        {
            if (victim == null) return;
            if (KillLogManager.Instance == null) return;

            // 被害者の名前
            string victimName = GetPlayerName(victim);

            // キルしたプレイヤーの名前（LastDamagedBy などから取得）
            // TODO: AbstractPlayer に「最後にダメージを与えたプレイヤー」情報が追加されたら、
            //       ここで正しいキラー名を取得する。
            string killerName = "???";

            // 自分が関係しているかどうかの判定
            bool isVictimMe = victim.PlayerType() == EPlayerType.MyPlayer;
            bool isKillerMe = false;

            // キラーの情報が取れる場合の処理（将来の拡張用）
            // if (killer != null) {
            //     killerName = GetPlayerName(killer);
            //     isKillerMe = killer.PlayerType() == EPlayerType.MyPlayer;
            // }

            KillLogManager.Instance.AddLog(
                killerName: killerName,
                victimName: victimName,
                weaponSprite: null, // TODO: 武器スプライトの参照先が決まったらここに渡す
                isKillerMe: isKillerMe,
                isVictimMe: isVictimMe
            );
        }

        private string GetPlayerName(AbstractPlayer player)
        {
            if (player == null) return "Unknown";

            // プレイヤー名を取得する試み
            try
            {
                string name = player.gameObject.name;
                return string.IsNullOrEmpty(name) ? "Unknown" : name;
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
