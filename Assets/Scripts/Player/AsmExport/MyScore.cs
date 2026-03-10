

namespace OpenGS
{
    public class MyScore
    {
        int kill = 0;
        int death = 0;
        //int suicide = 0;
        int totalDamage = 0;

        int flagDefence = 0;
        int flagReturn = 0;
        int salvageFrag = 0;

        public int Kill { get => kill; set => kill = value; }
        public int Death { get => death; set => death = value; }
        public int TotalDamage { get => totalDamage; set => totalDamage = value; }
        public int FlagReturn { get => flagReturn; set => flagReturn = value; }
        public int SalvageFrag { get => salvageFrag; set => salvageFrag = value; }

        public void AddKill()
        {

        }
        public void AddDeath()
        {

        }

    }
}
