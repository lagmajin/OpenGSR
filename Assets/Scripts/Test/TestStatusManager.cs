




namespace OpenGS
{
    public class TestStatusManager
    {
        private readonly object _lockObj = new object();

        public static TestStatusManager Instance { get; } = new();

        private int hitPoint = 500;

        private int magazine = 40;

        public int HitPoint
        {
            get { return hitPoint; }
            set
            {
                hitPoint = value;
            }

        }

        private int Magazine
        {
            get { return magazine; }
            set { magazine = value; }
        }

        public int MagazineCount
        {
            get { return magazine; }
            set { magazine = value; }
        }

        public bool ConsumeMagazine(int amount = 1)
        {
            if (amount <= 0 || magazine <= 0)
            {
                return false;
            }

            magazine = global::System.Math.Max(0, magazine - amount);
            return true;
        }

        public void RefillMagazine(int amount = 40)
        {
            magazine = global::System.Math.Max(0, amount);
        }

        public bool TakeDamage(int amount = 1)
        {
            if (amount <= 0 || hitPoint <= 0)
            {
                return false;
            }

            hitPoint = global::System.Math.Max(0, hitPoint - amount);
            return true;
        }

        public void Heal(int amount = 1)
        {
            if (amount <= 0)
            {
                return;
            }

            hitPoint += amount;
        }



        private TestStatusManager()
        {
            //HitPoint = 500;

        }





    }
}
