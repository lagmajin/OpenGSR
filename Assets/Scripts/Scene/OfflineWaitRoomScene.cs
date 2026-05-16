using System;
using System.Threading;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OpenGSCore;
using Sirenix.OdinInspector;

#pragma warning disable 0219

namespace OpenGS
{
    public partial class OfflineWaitRoomScene
    {
        void OnApplicationQuit()
        {
        }
    }

    public partial class OfflineWaitRoomScene
    {
    }

    public partial class OfflineWaitRoomScene : AbstractNonBattleScene, IOfflineWaitRoom
    {
        private static readonly EMap[] DeathMatchMaps =
        {
            EMap.DryDays,
            EMap.GreenHillSide1,
            EMap.GreenHillSide2,
            EMap.CityOfDarkness1,
            EMap.CityOfDarkness2,
            EMap.BluffStructure1,
            EMap.BluffStructure2,
            EMap.DesertedJungleSide1,
            EMap.DesertedJungleSide2,
            EMap.BattlePort1,
            EMap.FullHouse,
            EMap.FactoryInGaol,
            EMap.RobotFactory,
            EMap.RedStorm1,
            EMap.RedStorm2,
            EMap.ThePark,
            EMap.RuinOfWarSide1,
            EMap.RuinOfWarSide2,
            EMap.Nocturne,
            EMap.Waterfall,
            EMap.SkyHigh,
            EMap.GhostHouse,
            EMap.OnStudio,
            EMap.Christmas,
            EMap.SeaSideBase,
            EMap.AuroraClassic,
            EMap.ArchLoadOfGunster,
            EMap.IceValley,
        };

        private static readonly EMap[] TeamDeathMatchMaps =
        {
            EMap.DryDays,
            EMap.GreenHillSide1,
            EMap.GreenHillSide2,
            EMap.CityOfDarkness1,
            EMap.CityOfDarkness2,
            EMap.BluffStructure1,
            EMap.BluffStructure2,
            EMap.DesertedJungleSide1,
            EMap.DesertedJungleSide2,
            EMap.BattlePort1,
            EMap.FullHouse,
            EMap.FactoryInGaol,
            EMap.RobotFactory,
            EMap.RedStorm1,
            EMap.RedStorm2,
            EMap.ThePark,
            EMap.RuinOfWarSide1,
            EMap.RuinOfWarSide2,
            EMap.Nocturne,
            EMap.Waterfall,
            EMap.SkyHigh,
            EMap.GhostHouse,
            EMap.OnStudio,
            EMap.Christmas,
            EMap.SeaSideBase,
            EMap.AuroraClassic,
            EMap.ArchLoadOfGunster,
            EMap.IceValley,
        };

        private static readonly EMap[] CaptureTheFlagMaps =
        {
            EMap.BattlePortCTF,
            EMap.TheParkCTF,
            EMap.SkyHighCTF,
        };

        private static readonly EMap[] SurvivalMaps =
        {
            EMap.DryDays,
            EMap.Nocturne,
            EMap.GreenHillSide1,
            EMap.GhostHouse,
        };

        private SynchronizationContext mainThread;
        private MatchRoomManager matchRoomManager;
        private WaitRoom waitRoom;
        private OfflineGameModeSelect offlineSelect;

        public Button chara;
        public Button map;

        public Button PlusButton;
        public Button MinusButton;

        [Required] public GameObject charaSelectDialog;
        public GameObject dmMapSelectDialog;
        public GameObject tdmMapSelectDialog;
        public GameObject ctFMapSelectDialog;
        public GameObject suvMapSelectDialog;

        public GameObject weaponLimitDialog;
        public GameObject instantItemSelectDialog;

        public GameObject teamBalanceText;
        public GameObject playerCountText;

        public AudioClip bgm;
        public AudioClip gameStart;
        public AudioClip Click;

        void Awake()
        {
            DebugFlagManager.SetFirstSceneName("OfflineWaitRoom");

            mainThread = SynchronizationContext.Current;
            matchRoomManager = DependencyInjectionConfig.Resolve<MatchRoomManager>();

            GameGeneralManager.GetInstance.CreatePlayerWaitRoomInfo("test");
            InitializeOfflineState();
        }

        void Start()
        {
            if (SoundManager.Instance.IsBgmPlaying())
            {
                SoundManager.Instance.StopBgm();
            }

            SoundManager.Instance.PlayBgm(bgm);
            SceneManager.sceneLoaded += GameSceneLoaded;
        }

        void Update()
        {
            if (Input.GetKey(KeyCode.F10))
            {
                GameStart();
            }

            if (Input.GetKey(KeyCode.Escape))
            {
                GotoTitleScene();
            }
        }

        public void setDisableUI()
        {
            HideAllDialog();
        }

        public void ShowCharacterSelectDialog()
        {
            HideAllDialog();
            if (charaSelectDialog)
            {
                charaSelectDialog.SetActive(true);
            }
        }

        public void ShowDMMapSelectDialog()
        {
            HideAllDialog();
            if (dmMapSelectDialog)
            {
                dmMapSelectDialog.SetActive(true);
            }
        }

        public void ShowTDMMapSelectDialog()
        {
            HideAllDialog();
            if (tdmMapSelectDialog)
            {
                tdmMapSelectDialog.SetActive(true);
            }
        }

        public void ShowSUVMapSelectDialog()
        {
            HideAllDialog();
            if (suvMapSelectDialog)
            {
                suvMapSelectDialog.SetActive(true);
            }
        }

        public void ShowCTFMapSelectDialog()
        {
            HideAllDialog();
            if (ctFMapSelectDialog)
            {
                ctFMapSelectDialog.SetActive(true);
            }
        }

        public void ShowWeaponLimitDialog()
        {
            HideAllDialog();
            if (weaponLimitDialog)
            {
                weaponLimitDialog.SetActive(true);
            }
        }

        public void HideWeaponDialog()
        {
            if (weaponLimitDialog)
            {
                weaponLimitDialog.SetActive(false);
            }
        }

        public void HideAllDialog()
        {
            if (charaSelectDialog) charaSelectDialog.SetActive(false);
            if (dmMapSelectDialog) dmMapSelectDialog.SetActive(false);
            if (tdmMapSelectDialog) tdmMapSelectDialog.SetActive(false);
            if (ctFMapSelectDialog) ctFMapSelectDialog.SetActive(false);
            if (suvMapSelectDialog) suvMapSelectDialog.SetActive(false);
            if (weaponLimitDialog) weaponLimitDialog.SetActive(false);
            if (instantItemSelectDialog) instantItemSelectDialog.SetActive(false);
        }

        public void CharacterChanged(string str)
        {
            Debug.Log(str);
        }

        private void EditSelect(EGameMode mode, EMap map)
        {
            EnsureSelection();

            offlineSelect.GameMode = mode;
            offlineSelect.Map = map;
            offlineSelect.Capacity = Mathf.Max(1, offlineSelect.Capacity);
            GameModeSelectManager.Instance.OfflineGameSelect = offlineSelect;

            EnsureWaitRoom();
            ApplySelectionToWaitRoom();
            RefreshStatusText();
        }

        public void DMMapChanged(string str)
        {
            if (TryResolveMap(str, out var map))
            {
                EditSelect(EGameMode.DeathMatch, map);
            }
        }

        public void TDMMapChanged(string str)
        {
            if (TryResolveMap(str, out var map))
            {
                EditSelect(EGameMode.TeamDeathMatch, map);
            }
        }

        public void SUVMapChanged(string str)
        {
            if (TryResolveMap(str, out var map))
            {
                EditSelect(EGameMode.Survival, map);
            }
        }

        public void CTFMapChanged(string str)
        {
            if (TryResolveMap(str, out var map))
            {
                EditSelect(EGameMode.CaptureTheFlag, map);
            }
        }

        public void SetRandomMap()
        {
            EnsureSelection();
            var candidates = GetMapsForMode(offlineSelect.GameMode);
            if (candidates.Length == 0)
            {
                candidates = DeathMatchMaps;
            }

            var index = UnityEngine.Random.Range(0, candidates.Length);
            ChangeMap(candidates[index]);
        }

        private void GameSceneLoaded(Scene next, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= GameSceneLoaded;
        }

        public void GameStart()
        {
            EnsureSelection();
            EnsureWaitRoom();
            ApplySelectionToWaitRoom();

            if (waitRoom != null)
            {
                waitRoom.GameStart();
            }

            Debug.Log($"GameStart mode={offlineSelect.GameMode} map={offlineSelect.Map} capacity={offlineSelect.Capacity}");

            GameFlagsManager.GetInstance().BeforeSceneName = "OfflineWaitRoom";
            SceneManager.LoadSceneAsync(GeneralSceneMasterData.Instance().OfflineLoadingScene());
        }

        public void ChangeGameMode()
        {
            EnsureSelection();
            ChangeGameMode(offlineSelect.GameMode);
        }

        void ChangeGameModeToCTF(CTFMatchRule rule)
        {
            ChangeGameMode(EGameMode.CaptureTheFlag);
        }

        void ChangeGameModeToDeathMatch(DeathMatchRule rule)
        {
            ChangeGameMode(EGameMode.DeathMatch);
        }

        void ChangeGameModeToTeamDeathMatch(TeamDeathMatchRule rule)
        {
            ChangeGameMode(EGameMode.TeamDeathMatch);
        }

        void ChangeGameModeToSurvival()
        {
            ChangeGameMode(EGameMode.Survival);
        }

        void ChangeGameModeToTeamSurvival()
        {
            ChangeGameMode(EGameMode.TeamSurvival);
        }

        public void ChangeGameMode(EGameMode mode)
        {
            EnsureSelection();
            offlineSelect.GameMode = mode;
            if (offlineSelect.Capacity <= 0)
            {
                offlineSelect.Capacity = 8;
            }

            if (!IsMapCompatible(mode, offlineSelect.Map))
            {
                offlineSelect.Map = GetDefaultMap(mode);
            }

            GameModeSelectManager.Instance.OfflineGameSelect = offlineSelect;
            EnsureWaitRoom();
            ApplySelectionToWaitRoom();
            RefreshStatusText();
        }

        public void ChangeMap(EMap map)
        {
            EnsureSelection();
            offlineSelect.Map = map;
            GameModeSelectManager.Instance.OfflineGameSelect = offlineSelect;
            EnsureWaitRoom();
            ApplySelectionToWaitRoom();
            RefreshStatusText();
        }

        public void ChangeTeamBalance(bool balance)
        {
            EnsureSelection();
            offlineSelect.TeamBalance = balance;
            GameModeSelectManager.Instance.OfflineGameSelect = offlineSelect;
            RefreshStatusText();
        }

        public void Plus()
        {
            EnsureSelection();
            offlineSelect.Capacity = Mathf.Max(GetHumanPlayerCount(), offlineSelect.Capacity + 1);
            if (offlineSelect.Capacity > 16)
            {
                offlineSelect.Capacity = 16;
            }

            GameModeSelectManager.Instance.OfflineGameSelect = offlineSelect;
            EnsureWaitRoom();
            ApplySelectionToWaitRoom();
            RefreshStatusText();
        }

        public void Minus()
        {
            EnsureSelection();
            offlineSelect.Capacity = Mathf.Max(GetHumanPlayerCount(), offlineSelect.Capacity - 1);
            if (offlineSelect.Capacity <= 0)
            {
                offlineSelect.Capacity = 1;
            }

            GameModeSelectManager.Instance.OfflineGameSelect = offlineSelect;
            EnsureWaitRoom();
            ApplySelectionToWaitRoom();
            RefreshStatusText();
        }

        public void AddBot()
        {
            EnsureSelection();
            EnsureWaitRoom();

            if (waitRoom == null)
            {
                return;
            }

            if (waitRoom.AllPlayers().Count >= offlineSelect.Capacity)
            {
                return;
            }

            waitRoom.AddBotPlayer(null);
            RefreshStatusText();
        }

        public void FillBot()
        {
            EnsureSelection();
            EnsureWaitRoom();

            if (waitRoom == null)
            {
                return;
            }

            while (waitRoom.AllPlayers().Count < offlineSelect.Capacity)
            {
                var added = waitRoom.AddBotPlayer(null);
                if (added == null)
                {
                    break;
                }
            }

            RefreshStatusText();
        }

        public void AllBot()
        {
            EnsureSelection();
            EnsureWaitRoom();

            if (waitRoom == null)
            {
                return;
            }

            waitRoom.RemoveAllPlayers();
            while (waitRoom.AllPlayers().Count < offlineSelect.Capacity)
            {
                var added = waitRoom.AddBotPlayer(null);
                if (added == null)
                {
                    break;
                }
            }

            RefreshStatusText();
        }

        public void RemoveAllBot()
        {
            EnsureSelection();
            EnsureWaitRoom();

            if (waitRoom == null)
            {
                return;
            }

            waitRoom.RemoveAllBotPlayer();
            EnsureLocalPlayer();
            RefreshStatusText();
        }

        public void ShowInstantItemDialog()
        {
            HideAllDialog();
            if (instantItemSelectDialog)
            {
                instantItemSelectDialog.SetActive(true);
            }
        }

        public void GotoTitleScene()
        {
            GameFlagsManager.GetInstance().BeforeSceneName = "OfflineWaitRoom";
            SceneManager.LoadSceneAsync(GeneralSceneMasterData.Instance().TitleScene());
        }

        public void GotoShopScene()
        {
            GameFlagsManager.GetInstance().BeforeSceneName = "ShopScene";
            SceneManager.LoadSceneAsync(GeneralSceneMasterData.Instance().ShopScene());
        }

        public override SynchronizationContext MainThread()
        {
            return mainThread ?? SynchronizationContext.Current;
        }

        protected override void OnStartUnityEditor()
        {
        }

        protected override void OnQuitUnityEditor()
        {
        }

        protected override void OnStartFromEditorDirectly()
        {
        }

        private void InitializeOfflineState()
        {
            offlineSelect = GameModeSelectManager.Instance.OfflineGameSelect ?? new OfflineGameModeSelect();

            if (offlineSelect.GameMode == EGameMode.Unknown)
            {
                offlineSelect.GameMode = EGameMode.DeathMatch;
            }

            if (offlineSelect.Map == EMap.Unknown)
            {
                offlineSelect.Map = GetDefaultMap(offlineSelect.GameMode);
            }

            if (offlineSelect.Capacity <= 0)
            {
                offlineSelect.Capacity = 8;
            }

            GameModeSelectManager.Instance.OfflineGameSelect = offlineSelect;
            EnsureWaitRoom();
            ApplySelectionToWaitRoom();
            RefreshStatusText();
        }

        private void EnsureSelection()
        {
            if (offlineSelect == null)
            {
                offlineSelect = GameModeSelectManager.Instance.OfflineGameSelect ?? new OfflineGameModeSelect();
            }

            if (offlineSelect.GameMode == EGameMode.Unknown)
            {
                offlineSelect.GameMode = EGameMode.DeathMatch;
            }

            if (offlineSelect.Map == EMap.Unknown)
            {
                offlineSelect.Map = GetDefaultMap(offlineSelect.GameMode);
            }

            if (offlineSelect.Capacity <= 0)
            {
                offlineSelect.Capacity = 8;
            }
        }

        private void EnsureWaitRoom()
        {
            if (matchRoomManager == null)
            {
                matchRoomManager = DependencyInjectionConfig.Resolve<MatchRoomManager>();
            }

            if (!matchRoomManager.IsValidOfflineWaitRoom())
            {
                matchRoomManager.CreateNewOfflineWaitRoom("OfflineRoom");
            }

            waitRoom = matchRoomManager.WaitRoom;
            if (waitRoom == null)
            {
                return;
            }

            EnsureLocalPlayer();
        }

        private void EnsureLocalPlayer()
        {
            if (waitRoom == null)
            {
                return;
            }

            var players = waitRoom.AllPlayers();
            foreach (var player in players)
            {
                if (player != null && !player.IsBot)
                {
                    return;
                }
            }

            waitRoom.AddPlayer("local_player", "Player");
        }

        private void ApplySelectionToWaitRoom()
        {
            if (waitRoom == null)
            {
                return;
            }

            waitRoom.Capacity = offlineSelect.Capacity;
            waitRoom.ChangeGameMode(offlineSelect.GameMode);
            TrimBotsToCapacity();

            if (matchRoomManager != null)
            {
                matchRoomManager.MapInfo = new MapInfo
                {
                    GameMode = offlineSelect.GameMode,
                    Map = offlineSelect.Map
                };
            }
        }

        private void TrimBotsToCapacity()
        {
            if (waitRoom == null)
            {
                return;
            }

            var players = waitRoom.AllPlayers();
            if (players.Count <= offlineSelect.Capacity)
            {
                return;
            }

            var removeCount = players.Count - offlineSelect.Capacity;
            foreach (var player in players)
            {
                if (removeCount <= 0)
                {
                    break;
                }

                if (player != null && player.IsBot)
                {
                    waitRoom.RemovePlayer(player.Id);
                    removeCount--;
                }
            }
        }

        private void RefreshStatusText()
        {
            if (playerCountText)
            {
                var tmp = playerCountText.GetComponentInChildren<TMP_Text>(true);
                if (tmp != null)
                {
                    tmp.text = $"{GetPlayerCount()}/{offlineSelect.Capacity}";
                }
                else
                {
                    var legacy = playerCountText.GetComponentInChildren<Text>(true);
                    if (legacy != null)
                    {
                        legacy.text = $"{GetPlayerCount()}/{offlineSelect.Capacity}";
                    }
                }
            }

            if (teamBalanceText)
            {
                var tmp = teamBalanceText.GetComponentInChildren<TMP_Text>(true);
                if (tmp != null)
                {
                    tmp.text = offlineSelect.TeamBalance ? "Team Balance: ON" : "Team Balance: OFF";
                }
                else
                {
                    var legacy = teamBalanceText.GetComponentInChildren<Text>(true);
                    if (legacy != null)
                    {
                        legacy.text = offlineSelect.TeamBalance ? "Team Balance: ON" : "Team Balance: OFF";
                    }
                }
            }
        }

        private int GetPlayerCount()
        {
            if (waitRoom == null)
            {
                return 0;
            }

            return waitRoom.AllPlayers().Count;
        }

        private int GetHumanPlayerCount()
        {
            if (waitRoom == null)
            {
                return 0;
            }

            var count = 0;
            var players = waitRoom.AllPlayers();
            foreach (var player in players)
            {
                if (player != null && !player.IsBot)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool TryResolveMap(string value, out EMap map)
        {
            if (Enum.TryParse(value, true, out map))
            {
                return true;
            }

            switch (value)
            {
                case "DrayDays":
                case "DryDays":
                    map = EMap.DryDays;
                    return true;
                case "GreenHill1":
                    map = EMap.GreenHillSide1;
                    return true;
                case "GreenHill2":
                    map = EMap.GreenHillSide2;
                    return true;
                case "Jungle1":
                    map = EMap.DesertedJungleSide1;
                    return true;
                case "Jungle2":
                    map = EMap.DesertedJungleSide2;
                    return true;
                case "Ruin":
                    map = EMap.RuinOfWarSide1;
                    return true;
                case "House":
                    map = EMap.GhostHouse;
                    return true;
                case "SecretFactory":
                    map = EMap.RobotFactory;
                    return true;
                default:
                    map = EMap.Unknown;
                    return false;
            }
        }

        private static bool IsMapCompatible(EGameMode mode, EMap map)
        {
            var maps = GetMapsForMode(mode);
            foreach (var candidate in maps)
            {
                if (candidate == map)
                {
                    return true;
                }
            }

            return false;
        }

        private static EMap[] GetMapsForMode(EGameMode mode)
        {
            return mode switch
            {
                EGameMode.TeamDeathMatch => TeamDeathMatchMaps,
                EGameMode.CaptureTheFlag => CaptureTheFlagMaps,
                EGameMode.Survival => SurvivalMaps,
                EGameMode.TeamSurvival => SurvivalMaps,
                _ => DeathMatchMaps,
            };
        }

        private static EMap GetDefaultMap(EGameMode mode)
        {
            return mode switch
            {
                EGameMode.TeamDeathMatch => EMap.GreenHillSide1,
                EGameMode.CaptureTheFlag => EMap.BattlePortCTF,
                EGameMode.Survival => EMap.DryDays,
                EGameMode.TeamSurvival => EMap.DryDays,
                _ => EMap.DryDays,
            };
        }
    }
}
