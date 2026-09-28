using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    /// <summary>
    /// アイテムAグループ (PowerUp, DefenceUp) の出現サイクルとロジック。
    /// - 初回出現: ゲーム開始から27秒後
    /// - 次回出現: 取得または消滅（30秒）から27秒後
    /// - 消滅時間: 30秒間
    /// - 交互スポーン: 前回出現しなかった方が必ず出現する
    /// </summary>
    public class FieldItemService
    {
        public enum ESpawnState
        {
            None,       // 状態変化なし
            Waiting,    // 出現待ち (27秒)
            Active      // 出現中 (30秒)
        }

        private const float SPAWN_INTERVAL = 27.0f;
        private const float LIFE_TIME = 30.0f;

        private EFieldItemType lastSpawnedItemA = EFieldItemType.GrenadePack; // 初期値ダミー
        private float timer = SPAWN_INTERVAL;
        private ESpawnState currentState = ESpawnState.Waiting;

        public ESpawnState CurrentState => currentState;
        public float Timer => timer;
        public bool IsActive => currentState == ESpawnState.Active;
        public bool IsWaiting => currentState == ESpawnState.Waiting;

        public FieldItemService()
        {
            // 初期化
            var itemListA = new List<EFieldItemType>
            {
                EFieldItemType.PowerUpItem,
                EFieldItemType.DefenceUpItem
            };
            
            Random _random = new Random();
            int randomIndex = _random.Next(itemListA.Count);
            lastSpawnedItemA = itemListA[randomIndex];
            
            timer = SPAWN_INTERVAL;
            currentState = ESpawnState.Waiting;
        }

        public void Reset()
        {
            timer = SPAWN_INTERVAL;
            currentState = ESpawnState.Waiting;
        }

        /// <summary>
        /// タイマーを更新し、状態が変わった場合にそのイベント（出現・消滅）を返す
        /// </summary>
        /// <param name="deltaTime">経過時間(秒)</param>
        /// <param name="spawnedItem">出現したアイテムの種類</param>
        /// <returns>状態変化があった場合はその状態、なければ None</returns>
        public ESpawnState Update(float deltaTime, out EFieldItemType? spawnedItem)
        {
            spawnedItem = null;
            timer -= deltaTime;

            if (timer <= 0)
            {
                if (currentState == ESpawnState.Waiting)
                {
                    // 出現させる
                    currentState = ESpawnState.Active;
                    timer = LIFE_TIME;
                    
                    // 次のアイテムを選択 (交互スポーン)
                    lastSpawnedItemA = (lastSpawnedItemA == EFieldItemType.PowerUpItem) 
                        ? EFieldItemType.DefenceUpItem 
                        : EFieldItemType.PowerUpItem;
                    
                    spawnedItem = lastSpawnedItemA;
                    return ESpawnState.Active;
                }
                else
                {
                    // 消滅させる
                    currentState = ESpawnState.Waiting;
                    timer = SPAWN_INTERVAL;
                    return ESpawnState.Waiting;
                }
            }

            return ESpawnState.None;
        }

        /// <summary>
        /// プレイヤーが取得した際に呼び出す
        /// </summary>
        public void OnItemPickedUp()
        {
            currentState = ESpawnState.Waiting;
            timer = SPAWN_INTERVAL;
        }

        public void OnItemExpired()
        {
            currentState = ESpawnState.Waiting;
            timer = SPAWN_INTERVAL;
        }
    }
}
