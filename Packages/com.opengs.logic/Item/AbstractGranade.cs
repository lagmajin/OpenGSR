using System;
using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public enum EGranadeType : int
    {
        Normal = 0,
        Power,
        Cluster,
        Fire,
        Magnet,
        Unknown
    }

    /// <summary>
    /// 手榴弾が今どの段階にあるか。
    /// サーバが決めた状態をクライアントがそのまま受け取るための値で、
    /// クライアント側で自分の推測で爆発させないようにする。
    /// </summary>
    public enum GrenadeState
    {
        Idle,
        Armed,
        Exploded,
        Disposed
    }

    public class GranadeType
    {
        EGranadeType type_ = EGranadeType.Unknown;

        public EGranadeType Type { get => type_; set => type_ = value; }

        public GranadeType()
        {

        }

        GranadeType(EGranadeType type)
        {
            type_ = type;
        }

        public override string ToString()
        {
            string result=new string("");

            switch(type_)
            {
                case EGranadeType.Normal:

                    result = "Normal";

                    break;

                case EGranadeType.Fire:

                    result = "Fire";

                    break;

                case EGranadeType.Power:
                    result = "Power";
                    break;


                case EGranadeType.Cluster:
                    result = "Cluster";
                    break;

                case EGranadeType.Magnet:
                    result = "Magnet";
                    break;

                default:
                    result = "Unknown";

                    break;

            }

            return result;
        }

        public bool FromStr(in string str)
        {
            if (str == null)
            {
                type_ = EGranadeType.Unknown;
                return false;
            }

            switch(str)
            {
                case "Normal":
                    type_ = EGranadeType.Normal;
                    return true;


                case "Fire":
                    type_ = EGranadeType.Fire;
                    return true;

                case "Power":
                    type_ = EGranadeType.Power;
                    return true;

                case "Cluster":
                    type_ = EGranadeType.Cluster;
                    return true;

                case "Magnet":
                    type_ = EGranadeType.Magnet;
                    return true;

                default:
                    type_ = EGranadeType.Unknown;
                    return false;

            }
        }

        public override bool Equals(object obj)
        {
            return obj is GranadeType type &&
                   type_ == type.type_;
        }

        public override int GetHashCode()
        {
            return type_.GetHashCode();
        }

        public static bool operator==(GranadeType a, GranadeType b)
        {
            if (a is null || b is null) return false;
            return a.type_ == b.type_;
        }

        public static bool operator !=(GranadeType a, GranadeType b)
        {

            return !(a == b);
        }

    }

    public abstract class AbstractGrenade :AbstractGameObject
    {
        

        int stoppingPower = 0;
        int angle = 0;
        int speed = 0;

        int damage = 0;
        int explosionDamage = 0;

        private GranadeType granadeType = new GranadeType();
        private string ownerId = "";
        private float lifeTime = 3.0f;
        private float fuseTime = 3.0f;
        private float elapsedTime = 0f;
        private float explosionRadius = 2.5f;
        private GrenadeState state = GrenadeState.Idle;

        public event Action<AbstractGrenade> Armed;
        public event Action<AbstractGrenade> Exploded;
        public event Action<AbstractGrenade> ChildGrenadeCreated;

        public int StoppingPower { get => stoppingPower; set => stoppingPower = Math.Max(0, value); }
        public int Angle { get => angle; set => angle = value; }
        public int Speed { get => speed; set => speed = Math.Max(0, value); }
        public int ExplosionDamage { get => explosionDamage; set => explosionDamage = Math.Max(0, value); }
        public int Damage { get => damage; set => damage = Math.Max(0, value); }
        public string OwnerId { get => ownerId; set => ownerId = value ?? ""; }
        public GranadeType GranadeType { get => granadeType; set => granadeType = value ?? new GranadeType(); }
        public float LifeTime { get => lifeTime; set => lifeTime = Math.Max(0f, value); }
        public float FuseTime { get => fuseTime; set => fuseTime = Math.Max(0f, value); }
        public float ExplosionRadius { get => explosionRadius; set => explosionRadius = Math.Max(0f, value); }

        public GrenadeState State => state;
        public float ElapsedTime => elapsedTime;
        public float RemainingFuseTime => Math.Max(0f, FuseTime - elapsedTime);
        public bool IsArmed => state == GrenadeState.Armed;
        public bool IsExploded => state == GrenadeState.Exploded;
        public bool IsDisposed => state == GrenadeState.Disposed;

        protected AbstractGrenade()
        {
            ObjectType = eGameObjectType.Grenade;
        }

        protected AbstractGrenade(float x, float y)
        {
            ObjectType = eGameObjectType.Grenade;
            Posx = x;
            Posy = y;
        }

        /// <summary>
        /// 信管開始。Idle のときだけ状態を進めるので、二重に開始しても二重爆発にならない。
        /// </summary>
        public void Arm()
        {
            if (state != GrenadeState.Idle)
            {
                return;
            }

            state = GrenadeState.Armed;
            elapsedTime = 0f;
            OnArmed();
            Armed?.Invoke(this);
        }

        /// <summary>
        /// 起爆前の解除。解除できたときだけ true を返す。
        /// </summary>
        public bool Disarm()
        {
            if (state != GrenadeState.Armed)
            {
                return false;
            }

            state = GrenadeState.Idle;
            elapsedTime = 0f;
            return true;
        }

        /// <summary>
        /// 爆発を強制する。爆発済み・破棄済みの手榴弾では何もしないので、
        /// サーバからの通知が二重に届いても一度しか爆発しない。
        /// </summary>
        public void ForceExplode()
        {
            if (state == GrenadeState.Exploded || state == GrenadeState.Disposed)
            {
                return;
            }

            state = GrenadeState.Exploded;
            OnExploded();
            Exploded?.Invoke(this);
        }

        /// <summary>
        /// 信管を進め、到達したら true を返す。実際の爆炸は呼び出し側の責務で、
        /// クライアントが自分の推測で爆発させずに済む。
        /// </summary>
        public bool UpdateCountdown(float deltaSeconds)
        {
            if (!IsArmed || deltaSeconds <= 0f)
            {
                return false;
            }

            elapsedTime += deltaSeconds;

            if (elapsedTime >= FuseTime)
            {
                elapsedTime = FuseTime;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 寿命が終わった手榴弾を場から消す。
        /// </summary>
        public void DisposeGrenade()
        {
            state = GrenadeState.Disposed;
        }

        /// <summary>
        /// サーバの報告をそのまま反映する。キーがない項目はローカルの値を保つ。
        /// 位置は基底クラスの ToJSon が posx/posy として出すため、両表記を受け取る。
        /// </summary>
        public void ApplyState(JObject json)
        {
            if (json == null) return;

            if (TryReadFloat(json, out var px, "posx", "PosX")) Posx = px;
            if (TryReadFloat(json, out var py, "posy", "PosY")) Posy = py;

            if (json.TryGetValue("ownerId", out var oid))
            {
                OwnerId = oid.ToString();
            }

            if (json.TryGetValue("granadeType", out var gt))
            {
                GranadeType.FromStr(gt.ToString());
            }

            if (TryReadFloat(json, out var ft, "fuseTime")) FuseTime = ft;

            if (TryReadFloat(json, out var rf, "remainingFuseTime"))
            {
                elapsedTime = Math.Max(0f, FuseTime - rf);
            }

            if (TryReadFloat(json, out var er, "explosionRadius")) ExplosionRadius = er;

            if (TryReadInt(json, out var dmg, "damage")) Damage = dmg;

            if (TryReadInt(json, out var ed, "explosionDamage")) ExplosionDamage = ed;

            if (json.TryGetValue("state", out var st))
            {
                if (Enum.TryParse<GrenadeState>(st.ToString(), true, out var parsed))
                {
                    state = parsed;
                }
            }
        }

        /// <summary>
        /// 副作用なしで状態だけを設定する。サーバ報告の適用と手動での上进行で使う。
        /// </summary>
        public void SetState(GrenadeState toState, float elapsed = 0f)
        {
            state = toState;
            elapsedTime = Math.Max(0f, elapsed);
        }

        protected virtual void OnArmed()
        {
        }

        protected virtual void OnExploded()
        {
            Hit();
        }

        protected bool NotifyChildCreated(AbstractGrenade child)
        {
            if (child == null)
            {
                return false;
            }

            ChildGrenadeCreated?.Invoke(child);
            return true;
        }

        abstract public void Hit();

        public override JObject ToJSon()
        {
            var json = base.ToJSon();
            json["stoppingPower"] = StoppingPower;
            json["angle"] = Angle;
            json["speed"] = Speed;
            json["damage"] = Damage;
            json["explosionDamage"] = ExplosionDamage;
            json["ownerId"] = OwnerId;
            json["granadeType"] = GranadeType.ToString();
            json["lifeTime"] = LifeTime;
            json["fuseTime"] = FuseTime;
            json["remainingFuseTime"] = RemainingFuseTime;
            json["explosionRadius"] = ExplosionRadius;
            json["state"] = State.ToString();
            return json;
        }

        private static bool TryReadFloat(JObject json, out float value, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (json.TryGetValue(key, out var token) &&
                    float.TryParse(token.ToString(), out value))
                {
                    return true;
                }
            }

            value = 0f;
            return false;
        }

        private static bool TryReadInt(JObject json, out int value, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (json.TryGetValue(key, out var token) &&
                    int.TryParse(token.ToString(), out value))
                {
                    return true;
                }
            }

            value = 0;
            return false;
        }
    }
}
