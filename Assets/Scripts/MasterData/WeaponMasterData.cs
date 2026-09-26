using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    [CreateAssetMenu(menuName = "MasterData/Weapon/WeaponMasterData")]
    public class WeaponMasterData : ScriptableObject
    {
        public EWeaponType weaponType;
        // Runtime equipment uses this reference instead of requiring the
        // weapon prefab to live under a Resources directory.
        public GameObject weaponPrefab;
        public AudioClip shotSound;
        public float reloadTime = 2.0f;
        public int maxBullet = 30;
        public Sprite inGameSprite;
        public Sprite inSelectionSprite;
        public Sprite shilhouetteSprite;
    }
}
