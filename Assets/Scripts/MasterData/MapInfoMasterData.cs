


using OpenGSCore;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenGS
{
    public enum eDifficulty
    {
        Unknown,
        Easy,
        Normal,
        Hard,
        VeryHard
    }

    [CreateAssetMenu(menuName = "Map/MapInfoMasterData")]
    public class MapInfoMasterData : ScriptableObject
    {
        [SerializeField] [Required] public EMap map;

        [SerializeField] [Required] public string mapDisplayName;

        // Keep existing MapInfo assets authored with the former "stage" field usable.
        [SerializeField] [FormerlySerializedAs("stage")] private SceneObject mapScene;


        [SerializeField] public float boosterPower = 1.0f;
        [SerializeField] public float gravityScale = 1.0f;
        [SerializeField] public eDifficulty difficulty = eDifficulty.Normal;

        [SerializeField] public bool canPlayDM = true;
        [SerializeField] public bool canPlayTDM = true;
        [SerializeField] public bool canPlaySuv = true;

        [SerializeField] [Required] private Sprite smallThumbnail;
        [SerializeField] [Required] private Sprite bigThumbnail;

        public EMap MapType()
        {
            return map;
        }

        public string MapDisplayName()
        {
            return mapDisplayName;
        }

        public void CanPlayDeathMatch()
        {

        }

        public SceneObject MapScene()
        {
            return mapScene;
        }

        public bool HasMapScene()
        {
            return mapScene != null && !string.IsNullOrWhiteSpace(mapScene.SceneName());
        }

        public Sprite SmallThumbnail()
        {
            return smallThumbnail;
        }

        public Sprite BigThumbNail()
        {
            return bigThumbnail;
        }

        public eDifficulty Difficulty()
        {
            return difficulty;
        }

        public bool CanPlayForMode(EGameMode mode)
        {
            return mode switch
            {
                EGameMode.DeathMatch => canPlayDM,
                EGameMode.TeamDeathMatch => canPlayTDM,
                EGameMode.Survival => canPlaySuv,
                EGameMode.TeamSurvival => canPlaySuv,
                // This asset predates CTF-specific availability metadata.
                _ => true
            };
        }

        #if UNITY_EDITOR
        public void MapChanged()
        {

        }


        #endif


    }



}
