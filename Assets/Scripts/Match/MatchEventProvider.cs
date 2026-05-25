using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class MatchEventProvider : MonoBehaviour
    {
        [SerializeField] [Required] private AbstractMatchMainScript mainScript;
        private MatchRUDPServerNetworkManager networkManager;

        private void Start()
        {
            if (mainScript == null)
            {
                mainScript = GetComponent<AbstractMatchMainScript>();
            }

            ResolveNetworkManager();
        }

        public void UseGrenade()
        {
            var player = mainScript != null ? mainScript.player?.GetComponent<AbstractPlayer>() : null;
            var resolvedPlayerId = ResolvePlayerId(player);
            if (string.IsNullOrWhiteSpace(resolvedPlayerId))
            {
                return;
            }

            var position = player != null ? (Vector2)player.transform.position : Vector2.zero;
            var direction = player != null && player.transform.localScale.x < 0f
                ? Vector2.left
                : Vector2.right;
            var message = RUDPMessageBuilder.CreateGrenadeThrow(resolvedPlayerId, position, direction, "Normal");
            SendToServer(message);
        }

        public void UseInstantItem(EInstantItemType type)
        {
            UseInstantItem(mainScript != null ? mainScript.player?.GetComponent<AbstractPlayer>() : null, type);
        }

        public void UseInstantItem(AbstractPlayer player, EInstantItemType type)
        {
            var resolvedPlayerId = ResolvePlayerId(player);
            var effect = ResolveEffectName(type);
            var itemId = type.ToString();

            if (string.IsNullOrWhiteSpace(resolvedPlayerId))
            {
                return;
            }

            var message = RUDPMessageBuilder.CreateItemUseRequest(resolvedPlayerId, itemId, type.ToString(), effect);
            SendToServer(message);
        }

        private void ResolveNetworkManager()
        {
            if (networkManager != null)
            {
                return;
            }

            try
            {
                networkManager = DependencyInjectionConfig.Resolve<MatchRUDPServerNetworkManager>();
            }
            catch
            {
                networkManager = null;
            }
        }

        private void SendToServer(JObject json)
        {
            ResolveNetworkManager();
            if (networkManager == null || !networkManager.IsConnected())
            {
                Debug.LogWarning($"[MatchEventProvider] Match network not available for {json?["MessageType"]}");
                return;
            }

            networkManager.SendToServer(json);
        }

        private static string ResolvePlayerId(AbstractPlayer player)
        {
            if (player != null)
            {
                return player.UniqueID().ToString();
            }

            var profileId = AccountManager.Instance?.CurrentProfile?.GlobalUserId;
            return string.IsNullOrWhiteSpace(profileId) ? "local_player" : profileId;
        }

        private static string ResolveEffectName(EInstantItemType type)
        {
            return type switch
            {
                EInstantItemType.HealthKit => "heal",
                EInstantItemType.FireBullet => "fire_bullet",
                EInstantItemType.PoisonBullet => "poison_bullet",
                EInstantItemType.PowerGrenadePack => "power_grenade_pack",
                EInstantItemType.ClusterGrenadePack => "cluster_grenade_pack",
                EInstantItemType.MagnetGrenadePack => "magnet_grenade_pack",
                EInstantItemType.MineGrenadePack => "mine_grenade_pack",
                _ => "unknown"
            };
        }
    }
}
