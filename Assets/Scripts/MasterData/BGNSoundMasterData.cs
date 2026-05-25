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
            AudioClip clip = map switch
            {
                EMap.DryDays => dryDays,
                EMap.GreenHillSide1 => green,
                EMap.GreenHillSide2 => green,
                EMap.CityOfDarkness1 => ruin,
                EMap.CityOfDarkness2 => ruin,
                EMap.BluffStructure1 => bas,
                EMap.BluffStructure2 => bas,
                EMap.DesertedJungleSide1 => forest,
                EMap.DesertedJungleSide2 => forest,
                EMap.BattlePort1 => pipe,
                EMap.BattlePortCTF => pipe,
                EMap.FullHouse => bas,
                EMap.FactoryInGaol => factory,
                EMap.RobotFactory => factory,
                EMap.RedStorm1 => pipe,
                EMap.RedStorm2 => pipe,
                EMap.ThePark => amusementPark,
                EMap.TheParkCTF => amusementPark,
                EMap.RuinOfWarSide1 => ruin,
                EMap.RuinOfWarSide2 => ruin,
                EMap.Nocturne => aurora,
                EMap.Waterfall => aurora,
                EMap.SkyHigh => aurora,
                EMap.SkyHighCTF => aurora,
                EMap.GhostHouse => forest,
                EMap.OnStudio => amusementPark,
                EMap.Christmas => green,
                EMap.SeaSideBase => pipe,
                EMap.AuroraClassic => aurora,
                EMap.ArchLoadOfGunster => bas,
                EMap.IceValley => aurora,
                _ => dryDays
            };

            if (clip == null)
            {
                Debug.LogWarning($"[BGNSoundMasterData] BGN clip not assigned for {map}. Falling back to DryDays.");
                return dryDays;
            }

            return clip;
        }
    }
}
