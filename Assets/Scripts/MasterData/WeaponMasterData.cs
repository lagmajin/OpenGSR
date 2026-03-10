using UnityEngine;

namespace OpenGS
{
    [CreateAssetMenu(menuName = "MasterData/Weapon/WeaponMasterData")]
    public class WeaponMasterData : ScriptableObject
    {
        public AudioClip shotSound;
        public float reloadTime = 2.0f;
        public int maxBullet = 30;
    }
}
