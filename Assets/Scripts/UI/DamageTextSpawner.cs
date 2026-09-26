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
        [SerializeField] private int maxActiveDamageTexts = 32;

        private readonly Dictionary<string, AbstractPlayer> playerCache = new();
        private readonly List<GameObject> activeDamageTexts = new();
        private IDisposable damageSub;

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (spawnParent == null)
                spawnParent = transform as RectTransform;

            if (maxActiveDamageTexts < 1)
            {
                maxActiveDamageTexts = 1;
            }

            randomOffset.x = NormalizeNonNegative(randomOffset.x);
            randomOffset.y = NormalizeNonNegative(randomOffset.y);
        }

        private void OnEnable()
        {
            damageSub?.Dispose();
            damageSub = GameEventBroker.Subscribe<PlayerDamageEvent>(HandlePlayerDamageEvent);

            if (PlayerRegistry.Instance != null)
            {
                PlayerRegistry.Instance.OnPlayerUnregistered += HandlePlayerUnregistered;
            }
        }

        private void OnDisable()
        {
            damageSub?.Dispose();
            damageSub = null;
            if (PlayerRegistry.Instance != null)
            {
                PlayerRegistry.Instance.OnPlayerUnregistered -= HandlePlayerUnregistered;
            }
            playerCache.Clear();

            foreach (var damageText in activeDamageTexts)
            {
                if (damageText != null)
                {
                    Destroy(damageText);
                }
            }
            activeDamageTexts.Clear();
        }

        private void HandlePlayerUnregistered(AbstractPlayer player)
        {
            if (player == null)
            {
                return;
            }

            var playerId = player.UniqueID().ToString();
            playerCache.Remove(playerId);
        }

        /// <summary>
        /// ダメージイベントを受けたら、被弾位置にダメージを表示する。
        /// </summary>
        private void HandlePlayerDamageEvent(PlayerDamageEvent evt)
        {
            if (evt == null)
            {
                Debug.LogWarning("[DamageTextSpawner] PlayerDamageEvent is null.");
                return;
            }

            if (damagePrefab == null)
            {
                Debug.LogWarning("[DamageTextSpawner] damagePrefab is not assigned.");
                return;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    Debug.LogWarning("[DamageTextSpawner] targetCamera is not assigned.");
                    return;
                }
            }

            if (spawnParent == null)
            {
                spawnParent = transform as RectTransform;
                if (spawnParent == null)
                {
                    Debug.LogWarning("[DamageTextSpawner] spawnParent is not assigned.");
                    return;
                }
            }

            var player = ResolvePlayer(evt.TargetID());
            if (player == null) return;

            var remainingHp = evt.RemainingHp();
            var incomingDamage = evt.Damage();
            if (!float.IsFinite(remainingHp) || !float.IsFinite(incomingDamage))
            {
                Debug.LogWarning("[DamageTextSpawner] Ignoring non-finite damage event values.");
                return;
            }

            float currentHp = Mathf.Max(0, remainingHp);
            float previousHp = currentHp + Mathf.Max(0, incomingDamage);
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
                Debug.LogWarning("[DamageTextSpawner] playerId is empty.");
                return null;
            }

            if (playerCache.TryGetValue(playerId, out var cached) && cached != null)
            {
                return cached;
            }

            if (PlayerRegistry.Instance == null)
            {
                Debug.LogWarning("[DamageTextSpawner] PlayerRegistry.Instance is null.");
                return null;
            }

            if (!Guid.TryParse(playerId, out var guid))
            {
                Debug.LogWarning($"[DamageTextSpawner] Invalid playerId format: {playerId}");
                return null;
            }

            if (PlayerRegistry.Instance.TryGetPlayer(guid, out var player) && player != null)
            {
                playerCache[playerId] = player;
                return player;
            }

            Debug.LogWarning($"[DamageTextSpawner] Player not found for playerId: {playerId}");
            return null;
        }

        private void SpawnDamageText(AbstractPlayer player, int damage, bool isCritical)
        {
            if (player == null)
            {
                Debug.LogWarning("[DamageTextSpawner] player is null.");
                return;
            }

            if (damagePrefab == null || targetCamera == null || spawnParent == null)
            {
                Debug.LogWarning("[DamageTextSpawner] Spawn dependencies are not ready.");
                return;
            }

            // プレイヤーのワールド座標をスクリーン座標に変換
            Vector3 worldPos = player.transform.position + new Vector3(0, 0.3f, 0); // 少し頭上
            Vector3 screenPos = targetCamera.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0f) return; // カメラの背後なら表示しない

            // Prefab を生成
            var obj = Instantiate(damagePrefab, spawnParent);
            activeDamageTexts.RemoveAll(entry => entry == null);
            while (activeDamageTexts.Count >= maxActiveDamageTexts)
            {
                var oldest = activeDamageTexts[0];
                activeDamageTexts.RemoveAt(0);
                if (oldest != null)
                {
                    Destroy(oldest);
                }
            }
            activeDamageTexts.Add(obj);
            var rt = obj.GetComponent<RectTransform>();

            if (rt != null)
            {
                // ランダムなオフセットを加えて複数ヒットが重ならないようにする
                float rx = UnityEngine.Random.Range(-randomOffset.x, randomOffset.x);
                float ry = UnityEngine.Random.Range(-randomOffset.y, randomOffset.y);
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

        private static float NormalizeNonNegative(float value)
        {
            return float.IsFinite(value) ? Mathf.Max(0f, value) : 0f;
        }
    }
}
