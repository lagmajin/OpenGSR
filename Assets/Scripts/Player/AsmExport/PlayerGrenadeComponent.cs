using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;



namespace OpenGS
{

    [DisallowMultipleComponent]
    public class PlayerGrenadeComponent : MonoBehaviour
    {
        [SerializeField] public AllGrenadeListMasterData grenadeListMasterData;

        [Header("Grenade Throw Settings")]
        [SerializeField] private float maxChargeTime = 2.0f; // パワー1.0になるまでの時間（秒）
        [SerializeField] private float minPower = 0.1f;
        [SerializeField] private float maxPower = 1.0f;
        [SerializeField] private float baseThrowForce = 20f; // 基準となる投擲の強さ

        [Header("References")]
        [SerializeField] private Transform throwPoint; // 投げる位置の起点

        private bool isCharging = false;
        private float currentChargeTime = 0f;
        
        // UI 用に現在のパワー (0.0 ~ 1.0) を公開する場合に使う
        public float CurrentChargeRatio => isCharging ? Mathf.Clamp01(currentChargeTime / maxChargeTime) : 0f;

        void Start()
        {
            if (throwPoint == null)
            {
                throwPoint = transform; // 設定されてなければ自身の位置から
            }
        }

        void Update()
        {
            // スペースキーを押した瞬間
            if (Input.GetKeyDown(KeyCode.Space))
            {
                isCharging = true;
                currentChargeTime = 0f;
            }

            // スペースキー長押し中（パワーを溜める）
            if (isCharging && Input.GetKey(KeyCode.Space))
            {
                currentChargeTime += Time.deltaTime;
                currentChargeTime = Mathf.Min(currentChargeTime, maxChargeTime);
            }

            // スペースキーを離した瞬間（投げる）
            if (isCharging && Input.GetKeyUp(KeyCode.Space))
            {
                isCharging = false;
                
                // 比率 (0.0 ~ 1.0) を元にパワー (minPower ~ maxPower) を決定
                float ratio = currentChargeTime / maxChargeTime;
                float powerMultiplier = Mathf.Lerp(minPower, maxPower, ratio);
                
                ThrowGrenade(powerMultiplier);
            }
        }

        [Button("オートセット")]
        private void AutoSet()
        {
            var searchSentence = "t:" + nameof(AllGrenadeListMasterData);
            // エディタ設定用
        }

        [Button("グレネード投擲(テスト用)")]
        private void TestThrow()
        {
            ThrowGrenade(1.0f);
        }

        public void ThrowGrenade(float powerMultiplier)
        {
            var pAgent = GetComponent<AbstractPlayer>();
            if (pAgent != null)
            {
                if (pAgent.Status.GrenadeCount <= 0)
                {
                    Debug.Log("グレネードの残弾がありません。");
                    return;
                }
                
                // グレネードを消費
                pAgent.Status.ConsumeGrenade();
            }

            if (grenadeListMasterData == null || grenadeListMasterData.dataList == null || grenadeListMasterData.dataList.Count == 0)
            {
                Debug.LogWarning("グレネードのマスターデータが設定されていません");
                return;
            }

            // とりあえずリストの最初のグレネードを投げる（将来は装備中のインデックスを使用）
            var grenadeData = grenadeListMasterData.dataList[0];
            if (grenadeData == null || grenadeData.GrenadePrefab == null) return;

            // プレハブを生成
            var grenadeObj = Instantiate(grenadeData.GrenadePrefab, throwPoint.position, Quaternion.identity);

            // Rigidbody2D で物理的な力を加えて飛ばす
            var rb = grenadeObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // キャラクターの向いている方向を取得（仮に transform.right と少し上向きを合成）
                // 実際はプレイヤーのエイム向きに合わせます
                Vector2 throwDir = (transform.right + Vector3.up * 0.5f).normalized;
                
                // 力 = 基本値 × 溜め倍率
                float force = baseThrowForce * powerMultiplier;
                rb.AddForce(throwDir * force, ForceMode2D.Impulse);
                
                // 少し回転（トルク）をかけてっぽくする
                rb.AddTorque(-5f * powerMultiplier, ForceMode2D.Impulse);
            }

            Debug.Log($"グレネードを投げました! パワー倍率: {powerMultiplier:F2}");
            
            // 必要に応じて GameEventBroker に投擲イベントを Publish してネットワークに同期させる
            // var evt = new GrenadeThrowEvent(GetComponent<AbstractPlayer>()?.UniqueID(), throwPoint.position, throwDir, grenadeData.name);
            // GameEventBroker.Publish(evt);
        }
    }
}