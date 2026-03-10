using UnityEngine;

namespace OpenGS
{
    [CreateAssetMenu(menuName = "MasterData/Player/PlayerMasterData")]
    public class PlayerMasterData : ScriptableObject
    {
        public AudioClip[] damageVoices = new AudioClip[0];
    }
}
