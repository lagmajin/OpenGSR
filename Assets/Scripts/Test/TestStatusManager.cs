




namespace OpenGS
{
    public class TestStatusManager
    {
        private readonly object _lockObj = new object();

        public static TestStatusManager Instance { get; } = new();

        private int hitPoint = 500;

        private int boostPoint = 100;

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



        private TestStatusManager()
        {
            //HitPoint = 500;

        }





    }
}
