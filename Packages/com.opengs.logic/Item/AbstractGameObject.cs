using Newtonsoft.Json.Linq;
using System;

namespace OpenGSCore
{
    public enum eGameObjectType
    {
        Bullet,
        Grenade,
        Character,
        FieldItem,
    }

    /// <summary>
    /// ゲーム内オブジェクトの基本クラス。
    /// サーバーとクライアント間で状態同期するためのフレームワークを提供します。
    /// </summary>
    public abstract class AbstractGameObject
    {
        private string name = "";
        private float posx = 0;
        private float posy = 0;
        private string id = Guid.NewGuid().ToString("N");
        private bool updated = false;
        private int frame = 0;
        private DateTime time = DateTime.Now;

        // 同期用の前フレーム値
        protected float lastPosx = 0;
        protected float lastPosy = 0;
        protected DateTime lastSyncTime = DateTime.Now;

        public string Name { get => name; set => name = value; }
        public float Posx
        {
            get => posx;
            set
            {
                if (posx != value) updated = true;
                posx = value;
            }
        }
        public float Posy
        {
            get => posy;
            set
            {
                if (posy != value) updated = true;
                posy = value;
            }
        }
        public string Id { get => id; set => id = value; }
        public bool Updated { get => updated; }
        public int Frame { get => frame; set => frame = value; }
        public eGameObjectType ObjectType { get; protected set; }

        /// <summary>
        /// 位置を更新し、同期フラグを立てます。
        /// </summary>
        public virtual void SetPos(float x, float y)
        {
            if (Posx != x || Posy != y)
            {
                Posx = x;
                Posy = y;
                updated = true;
            }
        }

        /// <summary>
        /// フレーム更新時に呼び出されます。
        /// </summary>
        public virtual void Update()
        {
            frame++;
        }

        /// <summary>
        /// 状態が変化したかどうかを判定します。
        /// </summary>
        public virtual bool HasChanged()
        {
            return (Posx != lastPosx || Posy != lastPosy) || updated;
        }

        /// <summary>
        /// 現在の状態を同期状態として保存します。
        /// </summary>
        public virtual void SaveSyncState()
        {
            lastPosx = Posx;
            lastPosy = Posy;
            lastSyncTime = DateTime.Now;
            updated = false;
        }

        /// <summary>
        /// オブジェクトの状態をJSON形式で取得します。
        /// </summary>
        public virtual JObject ToJSon()
        {
            var json = new JObject();
            json["id"] = Id;
            json["name"] = Name;
            json["posx"] = Posx;
            json["posy"] = Posy;
            json["frame"] = Frame;
            json["type"] = ObjectType.ToString();
            return json;
        }
    }

    /// <summary>
    /// プレイヤーのゲーム内オブジェクト表現。
    /// ステータス情報を持ち、サーバー側でプレイヤーの状態を管理します。
    /// </summary>
    public class PlayerGameObject : AbstractGameObject
    {
        private PlayerStatus status;
        private string playerId = "";

        public PlayerStatus Status
        {
            get => status;
            set => status = value;
        }

        public string PlayerId
        {
            get => playerId;
            set => playerId = value;
        }

        public PlayerGameObject()
        {
            ObjectType = eGameObjectType.Character;
            status = new PlayerStatus();
        }

        public PlayerGameObject(string playerId, string name, float x, float y)
        {
            PlayerId = playerId;
            Name = name;
            Posx = x;
            Posy = y;
            ObjectType = eGameObjectType.Character;
            status = new PlayerStatus();
        }

        public override void Update()
        {
            base.Update();
            // プレイヤー固有の更新処理（ブースター減少など）をここに追加
        }

        public override JObject ToJSon()
        {
            var json = base.ToJSon();
            json["playerId"] = PlayerId;
            json["hp"] = Status?.Hp ?? 0;
            json["maxHp"] = Status?.MaxHp ?? 0;
            json["booster"] = Status?.Booster ?? 0;
            json["maxBooster"] = Status?.MaxBooster ?? 0;
            return json;
        }
    }

    /// <summary>
    /// ゲーム内キャラクター（NPC など）。
    /// </summary>
    public class Character : AbstractGameObject
    {
        public Character()
        {
            ObjectType = eGameObjectType.Character;
        }

        public Character(string name, float x, float y)
        {
            Name = name;
            Posx = x;
            Posy = y;
            ObjectType = eGameObjectType.Character;
        }
    }

    /// <summary>
    /// ステージ上のアイテム。
    /// </summary>
    public class FieldItem : AbstractGameObject
    {
        public FieldItem()
        {
            ObjectType = eGameObjectType.FieldItem;
        }

        public FieldItem(float x, float y)
        {
            Posx = x;
            Posy = y;
            ObjectType = eGameObjectType.FieldItem;
        }
    }

    /// <summary>
    /// 通常の手榴弾。
    /// </summary>
    public class NormalGranade : AbstractGameObject
    {
        private float velocityX = 0;
        private float velocityY = 0;
        private float lifeTime = 3.0f; // 秒単位

        public float VelocityX { get => velocityX; set => velocityX = value; }
        public float VelocityY { get => velocityY; set => velocityY = value; }
        public float LifeTime { get => lifeTime; set => lifeTime = value; }

        public NormalGranade()
        {
            ObjectType = eGameObjectType.Grenade;
        }

        public NormalGranade(float x, float y, float vx = 0, float vy = 0)
        {
            Posx = x;
            Posy = y;
            VelocityX = vx;
            VelocityY = vy;
            ObjectType = eGameObjectType.Grenade;
        }

        public override JObject ToJSon()
        {
            var json = base.ToJSon();
            json["velocityX"] = VelocityX;
            json["velocityY"] = VelocityY;
            json["lifeTime"] = LifeTime;
            return json;
        }
    }

}
