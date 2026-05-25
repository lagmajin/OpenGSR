using System.Text;
using OpenGSCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    /// <summary>
    /// ウェイトルームで1人分のプレイヤー情報を表示するコントローラ。
    /// 既存のシーンは Text と TMP が混在しているので、両方を自動で拾えるようにしている。
    /// </summary>
    [DisallowMultipleComponent]
    public class WaitRoomPlayerInfoController : MonoBehaviour
    {
        [System.Serializable]
        private sealed class TextBinding
        {
            [SerializeField] private Text legacyText;
            [SerializeField] private TMP_Text tmpText;

            public void BindFrom(Transform root, bool fallbackToAny, params string[] candidateNames)
            {
                if (root == null)
                {
                    return;
                }

                if (legacyText == null)
                {
                    legacyText = FindLegacyText(root, fallbackToAny, candidateNames);
                }

                if (tmpText == null)
                {
                    tmpText = FindTmpText(root, fallbackToAny, candidateNames);
                }
            }

            public void Set(string value)
            {
                if (legacyText != null)
                {
                    legacyText.text = value;
                }

                if (tmpText != null)
                {
                    tmpText.text = value;
                }
            }

            private static Text FindLegacyText(Transform root, bool fallbackToAny, params string[] candidateNames)
            {
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child == null || !MatchesCandidateName(child.name, candidateNames))
                    {
                        continue;
                    }

                    var text = child.GetComponent<Text>();
                    if (text != null)
                    {
                        return text;
                    }
                }

                return fallbackToAny ? root.GetComponentInChildren<Text>(true) : null;
            }

            private static TMP_Text FindTmpText(Transform root, bool fallbackToAny, params string[] candidateNames)
            {
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child == null || !MatchesCandidateName(child.name, candidateNames))
                    {
                        continue;
                    }

                    var text = child.GetComponent<TMP_Text>();
                    if (text != null)
                    {
                        return text;
                    }
                }

                return fallbackToAny ? root.GetComponentInChildren<TMP_Text>(true) : null;
            }
        }

        [Header("Text")]
        [SerializeField] private TextBinding summaryText;
        [SerializeField] private TextBinding playerNameText;
        [SerializeField] private TextBinding playerIdText;
        [SerializeField] private TextBinding levelText;
        [SerializeField] private TextBinding teamText;
        [SerializeField] private TextBinding readyText;
        [SerializeField] private TextBinding pingText;
        [SerializeField] private TextBinding characterText;
        [SerializeField] private TextBinding statText;

        [Header("Flags")]
        [SerializeField] private GameObject hostBadge;
        [SerializeField] private GameObject localPlayerBadge;
        [SerializeField] private GameObject botBadge;
        [SerializeField] private GameObject readyBadge;

        [Header("Background")]
        [SerializeField] private GameObject soloBackgroundObject;
        [SerializeField] private GameObject redTeamBackgroundObject;
        [SerializeField] private GameObject blueTeamBackgroundObject;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite soloBackgroundSprite;
        [SerializeField] private Sprite redTeamBackgroundSprite;
        [SerializeField] private Sprite blueTeamBackgroundSprite;
        [SerializeField] private Color backgroundTint = Color.white;

        private PlayerInfo boundPlayer;
        private int slotIndex = -1;
        private string roomOwnerId = string.Empty;
        private string localPlayerId = string.Empty;

        private void Awake()
        {
            EnsureBindings();
            Clear();
        }

        /// <summary>
        /// 1人分のプレイヤー情報をUIへ反映する。
        /// </summary>
        public void Bind(PlayerInfo player, int index, string ownerId = null, string currentLocalPlayerId = null)
        {
            boundPlayer = player;
            slotIndex = index;
            roomOwnerId = ownerId ?? string.Empty;
            localPlayerId = currentLocalPlayerId ?? string.Empty;

            EnsureBindings();
            Refresh();
        }

        /// <summary>
        /// 表示を空に戻す。
        /// </summary>
        public void Clear()
        {
            boundPlayer = null;
            slotIndex = -1;
            roomOwnerId = string.Empty;
            localPlayerId = string.Empty;

            EnsureBindings();

            summaryText?.Set(string.Empty);
            playerNameText?.Set(string.Empty);
            playerIdText?.Set(string.Empty);
            levelText?.Set(string.Empty);
            teamText?.Set(string.Empty);
            readyText?.Set(string.Empty);
            pingText?.Set(string.Empty);
            characterText?.Set(string.Empty);
            statText?.Set(string.Empty);

            SetBadgeActive(hostBadge, false);
            SetBadgeActive(localPlayerBadge, false);
            SetBadgeActive(botBadge, false);
            SetBadgeActive(readyBadge, false);

            if (backgroundImage != null)
            {
                ApplyBackground(OpenGSCore.ETeam.NoTeam, false, false, false);
            }
        }

        private void EnsureBindings()
        {
            summaryText ??= new TextBinding();
            playerNameText ??= new TextBinding();
            playerIdText ??= new TextBinding();
            levelText ??= new TextBinding();
            teamText ??= new TextBinding();
            readyText ??= new TextBinding();
            pingText ??= new TextBinding();
            characterText ??= new TextBinding();
            statText ??= new TextBinding();

            // まず名前ベースで探し、見つからなければ配下の最初のテキストを使う。
            summaryText.BindFrom(transform, true, "Summary", "PlayerInfo", "CharacterInfo", "Info");
            playerNameText.BindFrom(transform, false, "Name", "PlayerName", "DisplayName");
            playerIdText.BindFrom(transform, false, "PlayerId", "Id", "GUID");
            levelText.BindFrom(transform, false, "Level", "Lv");
            teamText.BindFrom(transform, false, "Team");
            readyText.BindFrom(transform, false, "Ready", "Status");
            pingText.BindFrom(transform, false, "Ping", "Latency");
            characterText.BindFrom(transform, false, "Character", "Chara");
            statText.BindFrom(transform, false, "Stats", "Param", "Detail");

            hostBadge ??= FindChild(transform, "HostBadge", "Host", "Owner");
            localPlayerBadge ??= FindChild(transform, "LocalBadge", "Local", "You", "Me");
            botBadge ??= FindChild(transform, "BotBadge", "Bot", "AI");
            readyBadge ??= FindChild(transform, "ReadyBadge", "Ready");

            soloBackgroundObject ??= FindChild(transform, "SoloBackground", "Solo", "Background");
            redTeamBackgroundObject ??= FindChild(transform, "RedPlayerSlotBG", "RedBackground", "RedTeamBackground", "RedTeam");
            blueTeamBackgroundObject ??= FindChild(transform, "BluePlayerSlotBG", "BlueBackground", "BlueTeamBackground", "BlueTeam");

            if (backgroundImage == null)
            {
                var backgroundObject = FindChild(transform, "Background", "PlayerSlotBG", "Solo");
                if (backgroundObject != null)
                {
                    backgroundImage = backgroundObject.GetComponent<Image>();
                }
            }
        }

        private void Refresh()
        {
            if (boundPlayer == null)
            {
                Clear();
                return;
            }

            var isHost = IsOwner(boundPlayer.Id);
            var isLocalPlayer = IsLocalPlayer(boundPlayer.Id);
            var isBot = boundPlayer.IsBot;
            var isReady = boundPlayer.IsReady;

            var summary = BuildSummary(boundPlayer, slotIndex, isHost, isLocalPlayer, isBot, isReady);
            summaryText?.Set(summary);
            playerNameText?.Set(boundPlayer.Name);
            playerIdText?.Set(string.IsNullOrWhiteSpace(boundPlayer.Id) ? "-" : boundPlayer.Id);
            levelText?.Set($"Lv.{boundPlayer.Level}");
            teamText?.Set($"Team: {FormatTeam(boundPlayer.Team)}");
            readyText?.Set(isReady ? "Ready" : "Not Ready");
            pingText?.Set(boundPlayer.Ping > 0 ? $"{boundPlayer.Ping} ms" : "-");
            characterText?.Set(FormatCharacter(boundPlayer));
            statText?.Set(FormatStats(boundPlayer));

            SetBadgeActive(hostBadge, isHost);
            SetBadgeActive(localPlayerBadge, isLocalPlayer);
            SetBadgeActive(botBadge, isBot);
            SetBadgeActive(readyBadge, isReady);

            ApplyBackground(boundPlayer.Team, isLocalPlayer, isHost, isBot);
        }

        private bool IsOwner(string playerId)
        {
            return !string.IsNullOrWhiteSpace(playerId)
                   && !string.IsNullOrWhiteSpace(roomOwnerId)
                   && string.Equals(playerId, roomOwnerId, System.StringComparison.OrdinalIgnoreCase);
        }

        private bool IsLocalPlayer(string playerId)
        {
            return !string.IsNullOrWhiteSpace(playerId)
                   && !string.IsNullOrWhiteSpace(localPlayerId)
                   && string.Equals(playerId, localPlayerId, System.StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildSummary(PlayerInfo player, int index, bool isHost, bool isLocalPlayer, bool isBot, bool isReady)
        {
            var builder = new StringBuilder();

            if (index >= 0)
            {
                builder.Append('#').Append(index + 1).Append(' ');
            }

            builder.Append(string.IsNullOrWhiteSpace(player.Name) ? "Player" : player.Name);
            builder.Append(" | ").Append("Lv.").Append(player.Level);
            builder.Append(" | ").Append(FormatTeam(player.Team));

            if (isReady)
            {
                builder.Append(" | Ready");
            }

            if (isHost)
            {
                builder.Append(" | Host");
            }

            if (isLocalPlayer)
            {
                builder.Append(" | You");
            }

            if (isBot)
            {
                builder.Append(" | Bot");
            }

            return builder.ToString();
        }

        private static string FormatTeam(OpenGSCore.ETeam team)
        {
            var value = team.ToString();
            return string.IsNullOrWhiteSpace(value) ? "No Team" : value;
        }

        private static string FormatCharacter(PlayerInfo player)
        {
            var character = player.playerCharacter.ToString();
            return string.IsNullOrWhiteSpace(character) ? "-" : character;
        }

        private static string FormatStats(PlayerInfo player)
        {
            return $"HP {player.Health}/{player.MaxHealth}  ATK {player.AttackPower}  DEF {player.DefensePower}";
        }

        private void ApplyBackground(OpenGSCore.ETeam team, bool isLocalPlayer, bool isHost, bool isBot)
        {
            var hasDedicatedBackgrounds = soloBackgroundObject != null
                                           || redTeamBackgroundObject != null
                                           || blueTeamBackgroundObject != null;

            if (hasDedicatedBackgrounds)
            {
                SetBackgroundActive(soloBackgroundObject, team == OpenGSCore.ETeam.NoTeam);
                SetBackgroundActive(redTeamBackgroundObject, team == OpenGSCore.ETeam.Red);
                SetBackgroundActive(blueTeamBackgroundObject, team == OpenGSCore.ETeam.Blue);
                return;
            }

            if (backgroundImage == null)
            {
                return;
            }

            // 既定はソロ背景、チーム戦では赤/青背景に切り替える。
            var sprite = ResolveBackgroundSprite(team);
            if (sprite != null)
            {
                backgroundImage.sprite = sprite;
            }

            backgroundImage.color = backgroundTint;

            // ソロ時はローカル/ホスト/ bot の補助演出を少しだけ残す。
            if (team == OpenGSCore.ETeam.NoTeam)
            {
                if (isLocalPlayer)
                {
                    backgroundImage.color = new Color(0.78f, 0.92f, 1f, 1f);
                }
                else if (isHost)
                {
                    backgroundImage.color = new Color(1f, 0.92f, 0.68f, 1f);
                }
                else if (isBot)
                {
                    backgroundImage.color = new Color(0.9f, 0.9f, 0.9f, 1f);
                }
            }
        }

        private Sprite ResolveBackgroundSprite(OpenGSCore.ETeam team)
        {
            return team switch
            {
                OpenGSCore.ETeam.Red => redTeamBackgroundSprite != null ? redTeamBackgroundSprite : soloBackgroundSprite,
                OpenGSCore.ETeam.Blue => blueTeamBackgroundSprite != null ? blueTeamBackgroundSprite : soloBackgroundSprite,
                _ => soloBackgroundSprite,
            };
        }

        private static GameObject FindChild(Transform root, params string[] candidateNames)
        {
            if (root == null)
            {
                return null;
            }

            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && MatchesCandidateName(child.name, candidateNames))
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static bool MatchesCandidateName(string currentName, params string[] candidateNames)
        {
            if (string.IsNullOrWhiteSpace(currentName) || candidateNames == null)
            {
                return false;
            }

            foreach (var candidate in candidateNames)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                if (string.Equals(currentName, candidate, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (currentName.IndexOf(candidate, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SetBadgeActive(GameObject badge, bool active)
        {
            if (badge != null)
            {
                badge.SetActive(active);
            }
        }

        private static void SetBackgroundActive(GameObject backgroundObject, bool active)
        {
            if (backgroundObject != null)
            {
                backgroundObject.SetActive(active);
            }
        }
    }
}
