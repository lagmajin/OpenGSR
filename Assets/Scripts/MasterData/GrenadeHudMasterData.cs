using UnityEngine;

namespace OpenGS
{
    [CreateAssetMenu(menuName = "MasterData/GrenadeHudMasterData")]
    public class GrenadeHudMasterData : ScriptableObject
    {
        public Sprite normal;
        public Sprite power;
        public Sprite magnetic;
        public Sprite mine;
        public Sprite cluster;
        public Sprite fire;
    }
}
