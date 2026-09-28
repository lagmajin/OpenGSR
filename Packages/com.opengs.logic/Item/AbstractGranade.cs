


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

    public class GranadeType
    {
        EGranadeType type_ = EGranadeType.Unknown;

        GranadeType(EGranadeType type=EGranadeType.Unknown)
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

                default:
                    result = "Unknown";

                    break;

            }

            return result;
        }

        public bool FromStr(in string str)
        {
            switch(str)
            {
                case "Normal":
                    type_ = EGranadeType.Normal;
                    break;


                case "Fire":
                    type_ = EGranadeType.Fire;
                    break;

                case "Power":
                    type_ = EGranadeType.Power;
                    break;

                case "Cluster":
                    type_ = EGranadeType.Cluster;
                    break;

                default:
                    type_ = EGranadeType.Unknown;
                    break;

            }



            return false;
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

        public int StoppingPower { get => stoppingPower; set => stoppingPower = value; }
        public int Angle { get => angle; set => angle = value; }
        public int Speed { get => speed; set => speed = value; }
        public int ExplosionDamage { get => explosionDamage; set => explosionDamage = value; }
        public int Damage { get => damage; set => damage = value; }

        protected AbstractGrenade()
        {

        }

        abstract public void Hit();
    }
}
