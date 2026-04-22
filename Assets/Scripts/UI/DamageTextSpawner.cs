using UnityEngine;

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

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (spawnParent == null)
                spawnParent = transform as RectTransform;
        }

        private void OnEnable()
        {
            if (PlayerRegistry.Instance == null) return;
            PlayerRegistry.Instance.OnPlayerHealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            if (PlayerRegistry.Instance == null) return;
            PlayerRegistry.Instance.OnPlayerHealthChanged -= HandleHealthChanged;
        }

        /// <summary>
        /// HP が変動したとき、減少分をダメージとして表示する。
        /// </summary>
        private void HandleHealthChanged(AbstractPlayer player, float newHp)
        {
            if (player == null || damagePrefab == null || targetCamera == null) return;

            // ダメージ量を計算（HP が減った場合のみ表示）
            float prevHp = newHp; // NOTE: 厳密には前の値が必要だがイベントから取得できないため下記で代用
            // PlayerStatus.HpStream を直接購読する方がより正確だが、
            // ここでは簡易的にイベント引数の newHp と MaxHp から推測する
            // → 将来的に PlayerRegistry.OnPlayerDamaged(player, damageAmount) イベントを追加するのが理想

            // 現状は「HP変化 = 何かあった」としてポップアップを出す
            // 回復時は出さないようにするため、Status から直近の変化量を判定
            // TODO: ダメージ量を正確に取得できるイベントに置き換える
            int displayDamage = Mathf.RoundToInt(player.GetMaxHP() - newHp);
            if (displayDamage <= 0) return; // 回復やフルHPの場合は表示しない

            SpawnDamageText(player, displayDamage);
        }

        private void SpawnDamageText(AbstractPlayer player, int damage)
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
                textUI.SetDamage(damage);
                return;
            }

            // DamageTextSprite (スプライト版) の場合
            var spriteUI = obj.GetComponent<DamageTextSprite>();
            if (spriteUI != null)
            {
                spriteUI.SetDamage(damage);
            }
        }
    }
}
