using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    //[CreateAssetMenu(menuName = "Master/BGN")]
    public class BGNSoundMasterData : ScriptableObject
    {
        private static BGNSoundMasterData instance;

        public AudioClip amusementPark;
        public AudioClip aurora;
        public AudioClip factory;
        public AudioClip dryDays;
        public AudioClip pipe;
        public AudioClip forest;
        public AudioClip green;
        public AudioClip ruin;
        public AudioClip bas;




        public static BGNSoundMasterData Instance()
        {
            if (instance == null)
            {
                var v = nameof(instance);

                instance = Resources.Load<BGNSoundMasterData>(v);

                return instance;
            }
            else
            {
                return instance;
            }

        }

        public AudioClip BGNSound(EMap map)
        {
            return null;
        }
    }
}
