using UnityEngine;
using System;
using System.Collections.Generic;

namespace OpenGS
{
    /// <summary>
    /// プレイヤーがダメージを受けた際に、被弾位置にダメージ数値UI (DamageTextUI or DamageTextSprite) を生成するスポナー。
    /// PlayerRegistry の OnPlayerHealthChanged イベントを監視する。
    ///
    /// 【使い方】
    ///   1. バトルシーンの Canvas に配置
    ///   2. damagePrefab に DamageTextUI または DamageTextSprite の Prefab をアサイン
    ///   3. targetCamera に対象カメラをアサイン（未指定なら Camera.main）
    /// </summary>
    [DisallowMultipleComponent]
    public class DamageTextSpawner : MonoBehaviour
    {
        [Header("Prefab (DamageTextUI または DamageTextSprite)")]
        [SerializeField] private GameObject damagePrefab;

        [Header("表示先")]
        [SerializeField] private RectTransform spawnParent; // Canvas の子にする親
        [SerializeField] private Camera targetCamera;

        [Header("設定")]
        [SerializeField] private Vector2 randomOffset = new Vector2(20f, 10f); // ランダムずれ幅(px)

        private readonly Dictionary<string, AbstractPlayer> playerCache = new();
        private IDisposable damageSub;

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (spawnParent == null)
                spawnParent = transform as RectTransform;
        }

        private void OnEnable()
        {
            damageSub?.Dispose();
            damageSub = GameEventBroker.Subscribe<PlayerDamageEvent>(HandlePlayerDamageEvent);
        }

        private void OnDisable()
        {
            damageSub?.Dispose();
            damageSub = null;
            playerCache.Clear();
        }

        /// <summary>
        /// ダメージイベントを受けたら、被弾位置にダメージを表示する。
        /// </summary>
        private void HandlePlayerDamageEvent(PlayerDamageEvent evt)
        {
            if (evt == null || damagePrefab == null || targetCamera == null) return;

            var player = ResolvePlayer(evt.TargetID());
            if (player == null) return;

            float currentHp = Mathf.Max(0, evt.RemainingHp());
            float previousHp = currentHp + Mathf.Max(0, evt.Damage());
            var feedback = DamageFeedbackCalculator.FromHealthSnapshot(
                evt.TargetID(),
                evt.AttackerID(),
                previousHp,
                currentHp,
                player.GetMaxHP(),
                false
            );

            if (!feedback.ShouldShow) return;

            SpawnDamageText(player, feedback.Damage, feedback.IsCritical);
        }

        private AbstractPlayer ResolvePlayer(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return null;
            }

            if (playerCache.TryGetValue(playerId, out var cached) && cached != null)
            {
                return cached;
            }

            if (PlayerRegistry.Instance == null)
            {
                return null;
            }

            if (!Guid.TryParse(playerId, out var guid))
            {
                return null;
            }

            if (PlayerRegistry.Instance.TryGetPlayer(guid, out var player) && player != null)
            {
                playerCache[playerId] = player;
                return player;
            }

            return null;
        }

        private void SpawnDamageText(AbstractPlayer player, int damage, bool isCritical)
        {
            // プレイヤーのワールド座標をスクリーン座標に変換
            Vector3 worldPos = player.transform.position + new Vector3(0, 0.3f, 0); // 少し頭上
            Vector3 screenPos = targetCamera.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0f) return; // カメラの背後なら表示しない

            // Prefab を生成
            var obj = Instantiate(damagePrefab, spawnParent);
            var rt = obj.GetComponent<RectTransform>();

            if (rt != null)
            {
                // ランダムなオフセットを加えて複数ヒットが重ならないようにする
                float rx = Random.Range(-randomOffset.x, randomOffset.x);
                float ry = Random.Range(-randomOffset.y, randomOffset.y);
                rt.position = new Vector3(screenPos.x + rx, screenPos.y + ry, 0);
            }

            // DamageTextUI (テキスト版) の場合
            var textUI = obj.GetComponent<DamageTextUI>();
            if (textUI != null)
            {
                textUI.SetDamage(damage, isCritical);
                return;
            }

            // DamageTextSprite (スプライト版) の場合
            var spriteUI = obj.GetComponent<DamageTextSprite>();
            if (spriteUI != null)
            {
                spriteUI.SetDamage(damage, isCritical);
            }
        }
    }
}
