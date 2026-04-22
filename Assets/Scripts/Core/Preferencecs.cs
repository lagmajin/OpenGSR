using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using OpenGSCore;

namespace OpenGS
{


    public class GraphicPreferences
    {

        public class PostEffectPreferences
        {

        }


    }

    public class UiPreferences
    {

    }

    public class Preferences
    {
        EGameMode mode;

        //private AudioPreferences audioPreferences;

        private GraphicPreferences graphicPreferences;
        public EGameMode DefaultShowMode()
        {

            return EGameMode.TeamDeathMatch;
        }

    }




}
