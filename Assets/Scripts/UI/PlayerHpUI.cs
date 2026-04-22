using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// プレイヤーの HP を Gauge (Slider) にリアルタイム反映するバインディングスクリプト。
    ///
    /// PlayerRegistry の OnPlayerHealthChanged / OnPlayerDied / OnPlayerRespawned イベントを購読し、
    /// 自分のプレイヤー（MyPlayer）の HP 変動を検知して Gauge.UpdateGauge() を呼ぶ。
    ///
    /// 【使い方】
    ///   1. Canvas 配下に Slider を持つ GameObject を作成し、Gauge を追加
    ///   2. その GameObject（または親）にこのスクリプトをアタッチ
    ///   3. hpGauge フィールドに Gauge コンポーネントをアサイン
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerHpUI : MonoBehaviour
    {
        [Header("HP ゲージ")]
        [SerializeField] private Gauge hpGauge;

        private AbstractPlayer myPlayer;

        private void OnEnable()
        {
            if (PlayerRegistry.Instance == null) return;

            PlayerRegistry.Instance.OnPlayerHealthChanged += HandleHealthChanged;
            PlayerRegistry.Instance.OnPlayerDied += HandlePlayerDied;
            PlayerRegistry.Instance.OnPlayerRespawned += HandlePlayerRespawned;
            PlayerRegistry.Instance.OnPlayerRegistered += HandlePlayerRegistered;

            // 既に自プレイヤーが登録済みなら初期化
            TryFindMyPlayer();
        }

        private void OnDisable()
        {
            if (PlayerRegistry.Instance == null) return;

            PlayerRegistry.Instance.OnPlayerHealthChanged -= HandleHealthChanged;
            PlayerRegistry.Instance.OnPlayerDied -= HandlePlayerDied;
            PlayerRegistry.Instance.OnPlayerRespawned -= HandlePlayerRespawned;
            PlayerRegistry.Instance.OnPlayerRegistered -= HandlePlayerRegistered;
        }

        private void HandlePlayerRegistered(AbstractPlayer player)
        {
            // 自プレイヤーが後から登録された場合にキャッチする
            if (player != null && player.PlayerType() == EPlayerType.MyPlayer)
            {
                myPlayer = player;
                InitGauge();
            }
        }

        private void HandleHealthChanged(AbstractPlayer player, float newHp)
        {
            if (!IsMyPlayer(player)) return;
            if (hpGauge == null) return;

            hpGauge.UpdateGauge(newHp, player.GetMaxHP());
        }

        private void HandlePlayerDied(AbstractPlayer player)
        {
            if (!IsMyPlayer(player)) return;
            if (hpGauge == null) return;

            // 死亡時は即座に 0 にする
            hpGauge.Init(0f, player.GetMaxHP());
        }

        private void HandlePlayerRespawned(AbstractPlayer player)
        {
            if (!IsMyPlayer(player)) return;

            // リスポーン時に満タンで初期化
            InitGauge();
        }

        private void TryFindMyPlayer()
        {
            if (PlayerRegistry.Instance == null) return;

            var players = PlayerRegistry.Instance.GetAllPlayers();
            foreach (var p in players)
            {
                if (p != null && p.PlayerType() == EPlayerType.MyPlayer)
                {
                    myPlayer = p;
                    InitGauge();
                    return;
                }
            }
        }

        private void InitGauge()
        {
            if (hpGauge == null || myPlayer == null) return;
            hpGauge.Init(myPlayer.GetHP(), myPlayer.GetMaxHP());
        }

        private bool IsMyPlayer(AbstractPlayer player)
        {
            return player != null && myPlayer != null && player.UniqueID() == myPlayer.UniqueID();
        }
    }
}
