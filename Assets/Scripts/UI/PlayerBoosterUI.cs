using System.Collections;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// プレイヤーのブースター残量を Gauge (Slider) にリアルタイム反映するバインディングスクリプト。
    ///
    /// PlayerRegistry の OnPlayerBoosterChanged イベントを購読し、
    /// 自分のプレイヤー（MyPlayer）のブースター変動を検知して Gauge.UpdateGauge() を呼ぶ。
    ///
    /// 【使い方】
    ///   1. Canvas 配下に Slider を持つ GameObject を作成し、Gauge を追加
    ///   2. その GameObject（または親）にこのスクリプトをアタッチ
    ///   3. boosterGauge フィールドに Gauge コンポーネントをアサイン
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerBoosterUI : MonoBehaviour
    {
        [Header("ブースターゲージ")]
        [SerializeField] private Gauge boosterGauge;

        private AbstractPlayer myPlayer;
        private Coroutine bindRoutine;
        private bool subscribed;

        private void OnEnable()
        {
            bindRoutine = StartCoroutine(BindWhenRegistryReady());
        }

        private IEnumerator BindWhenRegistryReady()
        {
            while (isActiveAndEnabled && PlayerRegistry.Instance == null)
            {
                yield return null;
            }

            if (!isActiveAndEnabled || PlayerRegistry.Instance == null)
            {
                yield break;
            }

            PlayerRegistry.Instance.OnPlayerBoosterChanged += HandleBoosterChanged;
            PlayerRegistry.Instance.OnPlayerRespawned += HandlePlayerRespawned;
            PlayerRegistry.Instance.OnPlayerRegistered += HandlePlayerRegistered;

            TryFindMyPlayer();
            subscribed = true;
            bindRoutine = null;
        }

        private void OnDisable()
        {
            if (bindRoutine != null)
            {
                StopCoroutine(bindRoutine);
                bindRoutine = null;
            }

            if (!subscribed)
            {
                myPlayer = null;
                return;
            }

            if (PlayerRegistry.Instance == null)
            {
                subscribed = false;
                myPlayer = null;
                return;
            }

            PlayerRegistry.Instance.OnPlayerBoosterChanged -= HandleBoosterChanged;
            PlayerRegistry.Instance.OnPlayerRespawned -= HandlePlayerRespawned;
            PlayerRegistry.Instance.OnPlayerRegistered -= HandlePlayerRegistered;
            subscribed = false;
            myPlayer = null;
        }

        private void HandlePlayerRegistered(AbstractPlayer player)
        {
            if (player != null && player.PlayerType() == EPlayerType.MyPlayer)
            {
                myPlayer = player;
                InitGauge();
            }
        }

        private void HandleBoosterChanged(AbstractPlayer player, float newBooster)
        {
            if (!IsMyPlayer(player)) return;
            if (boosterGauge == null) return;

            boosterGauge.UpdateGauge(newBooster, player.GetMaxBooster());
        }

        private void HandlePlayerRespawned(AbstractPlayer player)
        {
            if (!IsMyPlayer(player)) return;
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
            if (boosterGauge == null || myPlayer == null) return;
            boosterGauge.Init(myPlayer.GetBooster(), myPlayer.GetMaxBooster());
        }

        private bool IsMyPlayer(AbstractPlayer player)
        {
            return player != null && myPlayer != null && player.UniqueID() == myPlayer.UniqueID();
        }
    }
}
