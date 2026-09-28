using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    /// <summary>
    /// ステージ上に配置されるアイテムの基本クラス。
    /// 一定時間でアイテムが消滅するメカニズムを提供します。
    /// </summary>
    public abstract class AbstractFieldItem : AbstractGameObject
    {
        /// <summary>
        /// アイテムの生存時間（ミリ秒）。デフォルト: 30秒
        /// </summary>
        protected int lifetimeMsec = 30000;

        /// <summary>
        /// アイテムの生成時刻
        /// </summary>
        protected DateTime createdTime = DateTime.Now;

        /// <summary>
        /// アイテムが消滅したかどうか
        /// </summary>
        protected bool destroyed = false;

        public int LifetimeMsec 
        { 
            get => lifetimeMsec; 
            set => lifetimeMsec = value; 
        }

        public bool IsDestroyed { get => destroyed; }

        public AbstractFieldItem(float x, float y)
        {
            Posx = x;
            Posy = y;
            ObjectType = eGameObjectType.FieldItem;
            createdTime = DateTime.Now;
        }

        /// <summary>
        /// フレーム更新。生存時間をチェックして消滅判定を行います。
        /// </summary>
        public override void Update()
        {
            base.Update();

            // 生存時間チェック
            var elapsed = (DateTime.Now - createdTime).TotalMilliseconds;
            if (elapsed > lifetimeMsec)
            {
                destroyed = true;
            }
        }

        /// <summary>
        /// プレイヤーがアイテムを取得時の処理。
        /// 派生クラスでオーバーライドして実装します。
        /// </summary>
        public abstract void OnPickedUp(PlayerGameObject player);

        /// <summary>
        /// アイテムの状態をJSON形式で取得します。
        /// </summary>
        public override JObject ToJSon()
        {
            var json = base.ToJSon();
            json["lifetime"] = lifetimeMsec;
            json["elapsed"] = (DateTime.Now - createdTime).TotalMilliseconds;
            json["destroyed"] = destroyed;
            return json;
        }
    }
}
