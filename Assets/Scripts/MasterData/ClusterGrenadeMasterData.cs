using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// クラスターグレネードの子弾設定。
    /// child grenade の個数、初速、拡散角を master data 化して調整しやすくする。
    /// </summary>
    [CreateAssetMenu(menuName = "MasterData/Weapon/ClusterGrenadeMasterData")]
    public class ClusterGrenadeMasterData : ScriptableObject
    {
        [SerializeField] private int childGrenadeCount = 3;
        [SerializeField] private float childLaunchSpeed = 8.0f;
        [SerializeField] private float childSpreadAngle = 45.0f;

        private static ClusterGrenadeMasterData instance;

        public static ClusterGrenadeMasterData Instance()
        {
            if (instance == null)
            {
                instance = Resources.Load<ClusterGrenadeMasterData>("MasterData/Grenade/ClusterGrenadeMasterData");
            }

            return instance;
        }

        public int ChildGrenadeCount() => Mathf.Max(0, childGrenadeCount);
        public float ChildLaunchSpeed() => Mathf.Max(0f, childLaunchSpeed);
        public float ChildSpreadAngle() => Mathf.Max(0f, childSpreadAngle);
    }
}
