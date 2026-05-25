

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
        public int FlagDefence { get => flagDefence; set => flagDefence = value; }

        public void AddKill()
        {
            kill++;
        }
        public void AddDeath()
        {
            death++;
        }

        public void AddTotalDamage(int value)
        {
            if (value > 0)
            {
                totalDamage += value;
            }
        }

        public void AddFlagReturn(int value = 1)
        {
            if (value > 0)
            {
                flagReturn += value;
            }
        }

        public void AddSalvageFrag(int value = 1)
        {
            if (value > 0)
            {
                salvageFrag += value;
            }
        }

        public void AddFlagDefence(int value = 1)
        {
            if (value > 0)
            {
                flagDefence += value;
            }
        }

    }
}
