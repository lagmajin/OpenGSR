using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// GameEventBroker と PlayerRegistry の死亡イベントを監視し、
    /// KillLogManager にキルログを追加するリスナー。
    /// </summary>
    [DisallowMultipleComponent]
    public class KillLogEventListener : MonoBehaviour
    {
        private IDisposable killSub;
        private IDisposable deadSub;
        private readonly Dictionary<string, string> nameCache = new();
        private readonly HashSet<string> handledDeadVictims = new();

        private void OnEnable()
        {
            killSub?.Dispose();
            deadSub?.Dispose();

            killSub = GameEventBroker.Subscribe<PlayerKillEvent>(HandlePlayerKillEvent);
            deadSub = GameEventBroker.Subscribe<PlayerDeadEvent>(HandlePlayerDeadEvent);
        }

        private void OnDisable()
        {
            killSub?.Dispose();
            deadSub?.Dispose();
            killSub = null;
            deadSub = null;
            nameCache.Clear();
            handledDeadVictims.Clear();
        }

        private void HandlePlayerKillEvent(PlayerKillEvent evt)
        {
            if (evt == null || KillLogManager.Instance == null) return;

            var entry = KillLogFormatter.Create(
                killerName: ResolvePlayerName(evt.KillerID()),
                victimName: ResolvePlayerName(evt.VictimID()),
                weaponName: evt.WeaponType(),
                isKillerMe: IsMyPlayer(evt.KillerID()),
                isVictimMe: IsMyPlayer(evt.VictimID()),
                isHeadshot: evt.IsHeadshot()
            );

            handledDeadVictims.Add(evt.VictimID());
            KillLogManager.Instance.AddLog(entry);
        }

        private void HandlePlayerDeadEvent(PlayerDeadEvent evt)
        {
            if (evt == null || KillLogManager.Instance == null) return;

            if (!string.IsNullOrWhiteSpace(evt.PlayerID()) && handledDeadVictims.Remove(evt.PlayerID()))
            {
                return;
            }

            var killerId = evt.KillerID();
            var killerName = string.IsNullOrWhiteSpace(killerId) ? "Unknown" : ResolvePlayerName(killerId);
            var entry = KillLogFormatter.Create(
                killerName: killerName,
                victimName: ResolvePlayerName(evt.PlayerID()),
                weaponName: string.Empty,
                isKillerMe: IsMyPlayer(killerId),
                isVictimMe: IsMyPlayer(evt.PlayerID()),
                isHeadshot: false
            );

            KillLogManager.Instance.AddLog(entry);
        }

        private string ResolvePlayerName(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return "Unknown";
            }

            if (nameCache.TryGetValue(playerId, out var cached) && !string.IsNullOrWhiteSpace(cached))
            {
                return cached;
            }

            if (PlayerRegistry.Instance != null && Guid.TryParse(playerId, out var guid) && PlayerRegistry.Instance.TryGetPlayer(guid, out var player) && player != null)
            {
                var name = string.IsNullOrWhiteSpace(player.gameObject.name) ? "Unknown" : player.gameObject.name;
                nameCache[playerId] = name;
                return name;
            }

            return playerId;
        }

        private bool IsMyPlayer(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId) || PlayerRegistry.Instance == null)
            {
                return false;
            }

            if (!Guid.TryParse(playerId, out var guid))
            {
                return false;
            }

            return PlayerRegistry.Instance.TryGetPlayer(guid, out var player) && player != null && player.PlayerType() == EPlayerType.MyPlayer;
        }
    }
}
