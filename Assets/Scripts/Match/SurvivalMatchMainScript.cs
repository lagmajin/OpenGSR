using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using OpenGSCore;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class SurvivalMatchMainScript : AbstractMatchMainScript
    {
        public static SurvivalMatchMainScript Instance { get; private set; }

        [SerializeField] [Required] private ReSpawnPoints respawnPoints;

        private MatchRUDPServerNetworkManager networkManager;
        private int aliveCount;

        private new void Start()
        {
            base.Start();
            Application.targetFrameRate = 30;
            Instance = this;

            try
            {
                networkManager = DependencyInjectionConfig.Resolve<MatchRUDPServerNetworkManager>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SUV] Failed to resolve MatchRUDPServerNetworkManager: {ex.Message}");
                networkManager = null;
            }

            Debug.Log("[SUV] Survival GameStart");
            Invoke(nameof(GameSetup), 0.1f);
        }

        private void GameSetup()
        {
            PlayGameStartVoice();
            CountAlivePlayers();
            CreateMyPlayerLocally();
            CreateOtherPlayers();
        }

        private void CountAlivePlayers()
        {
            var room = matchRoomManager?.WaitRoom;
            aliveCount = room?.AllPlayers()?.Count(p => p != null) ?? 1;
            aliveCount = Math.Max(1, aliveCount);
            Debug.Log($"[SUV] Alive players={aliveCount}");
        }

        private void CreateMyPlayerLocally()
        {
            var spawnPos = GetRandomSpawnPoint(respawnPoints);
            var myPlayer = CreateMyPlayer(spawnPos, ETeam.NoTeam);
            AttachPlayerId(myPlayer, ResolveLocalPlayerId());
        }

        private void CreateOtherPlayers()
        {
            var room = matchRoomManager?.WaitRoom;
            if (room == null)
            {
                return;
            }

            var localPlayerId = ResolveLocalPlayerId();
            foreach (var info in room.AllPlayers())
            {
                if (info == null || string.IsNullOrWhiteSpace(info.Id))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(localPlayerId) &&
                    string.Equals(info.Id, localPlayerId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var prefab = prefabMasterData?.SearchPlayerPrefab(info.playerCharacter.ToString())
                    ?? prefabMasterData?.SearchPlayerPrefab(EPlayerCharacter.Misty.ToString());
                if (prefab == null)
                {
                    continue;
                }

                var spawnPos = GetRandomSpawnPoint(respawnPoints);
                var playerObj = Instantiate(prefab, spawnPos, Quaternion.identity);
                playerObj.name = $"OtherPlayer_{info.Name}";

                var player = playerObj.GetComponent<AbstractPlayer>();
                if (player != null)
                {
                    player.SetPlayerType(EPlayerType.OtherPlayer);
                    player.SetTeam(ETeam.NoTeam);
                    AttachPlayerId(playerObj, info.Id);
                    player.OnSpawn();
                }
            }
        }

        public override void OnMyPlayerDead()
        {
            Debug.Log("[SUV] My player died. Switching to spectator mode.");

            if (player != null)
            {
                player.SetActive(false);
            }

            EnterSpectatorMode(player?.transform);
            HandlePlayerEliminated();
        }

        public override void PostEvent(AbstractGameEvent e)
        {
            if (e is PlayerDeadEvent)
            {
                HandlePlayerEliminated();
            }
        }

        protected override void OnNetworkDataRecved(JObject obj)
        {
            var messageType = MessageType.Normalize(obj["MessageType"]?.ToString());
            if (messageType == RUDPMessageTypes.PlayerDeath)
            {
                HandlePlayerEliminated();
                return;
            }

            base.OnNetworkDataRecved(obj);
        }

        private void HandlePlayerEliminated()
        {
            if (endFlag)
            {
                return;
            }

            aliveCount = Math.Max(0, aliveCount - 1);
            Debug.Log($"[SUV] Player eliminated. Alive={aliveCount}");

            if (aliveCount > 1)
            {
                return;
            }

            endFlag = true;
            Invoke(nameof(GoToResultScene), gotoResultSceneWaitTime);
        }

        private void GoToResultScene()
        {
            GoToResult();
        }

        private static string ResolveLocalPlayerId()
        {
            var profile = AccountManager.Instance?.CurrentProfile;
            return string.IsNullOrWhiteSpace(profile?.GlobalUserId) ? string.Empty : profile.GlobalUserId;
        }

        private static void AttachPlayerId(GameObject playerObj, string playerId)
        {
            if (playerObj == null) return;

            var linker = playerObj.GetComponent<PlayerDataLinker>();
            if (linker == null)
                linker = playerObj.AddComponent<PlayerDataLinker>();

            linker.SetPlayerId(playerId ?? string.Empty);
        }
    }
}
