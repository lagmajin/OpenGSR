using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    public class WeaponLimitDialog : MonoBehaviour
    {
        [SerializeField] private Vector2 position;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Button okButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Toggle ar;
        [SerializeField] private Toggle smg;
        [SerializeField] private Toggle sr;
        [SerializeField] private Toggle sg;
        [SerializeField] private Toggle gr;
        [SerializeField] private Toggle mg;
        [SerializeField] private AudioClip buttonPushSound;

        private MatchRoomManager matchRoomManager;
        private bool listenersRegistered;
        private static readonly eWeaponType[] AssaultRifleWeapons =
        {
            eWeaponType.AK47,
            eWeaponType.M16,
            eWeaponType.FAMAS,
            eWeaponType.F2000,
            eWeaponType.SteyAug,
        };

        private static readonly eWeaponType[] SmgWeapons =
        {
            eWeaponType.Scorpion,
            eWeaponType.FN_P90,
            eWeaponType.Uzi,
            eWeaponType.MP5,
        };

        private static readonly eWeaponType[] SniperWeapons =
        {
            eWeaponType.Scout,
            eWeaponType.Dragunov,
            eWeaponType.PSG1,
            eWeaponType.AWP,
        };

        private static readonly eWeaponType[] SpecialGunWeapons =
        {
            eWeaponType.LaserGun,
            eWeaponType.BubbleGun,
            eWeaponType.ChirstmasGun,
        };

        private static readonly eWeaponType[] GunnerWeapons =
        {
            eWeaponType.Glock,
            eWeaponType.DE,
        };

        private static readonly eWeaponType[] MachineGunWeapons =
        {
            eWeaponType.MG42,
            eWeaponType.M60,
            eWeaponType.FNMinimi_SAW,
        };

        private void Awake()
        {
            matchRoomManager = DependencyInjectionConfig.Resolve<MatchRoomManager>();
            AutoBindIfNeeded();
            SetupListeners();
            SyncUiFromState();
        }

        private void OnEnable()
        {
            if (matchRoomManager == null)
            {
                matchRoomManager = DependencyInjectionConfig.Resolve<MatchRoomManager>();
            }

            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }

            AutoBindIfNeeded();
            SetupListeners();
            SyncUiFromState();
        }

        public void Open()
        {
            gameObject.SetActive(true);
        }

        public void OnOK()
        {
            ApplySelection();
            Close();
        }

        public void OnOk()
        {
            OnOK();
        }

        public void onCancel()
        {
            SyncUiFromState();
            Close();
        }

        public void OnCancel()
        {
            onCancel();
        }

        private void ApplySelection()
        {
            if (matchRoomManager == null)
            {
                return;
            }

            ApplyCategory(ar, AssaultRifleWeapons);
            ApplyCategory(smg, SmgWeapons);
            ApplyCategory(sr, SniperWeapons);
            ApplyCategory(sg, SpecialGunWeapons);
            ApplyCategory(gr, GunnerWeapons);
            ApplyCategory(mg, MachineGunWeapons);
        }

        private void ApplyCategory(Toggle toggle, IReadOnlyList<eWeaponType> weapons)
        {
            if (toggle == null || weapons == null)
            {
                return;
            }

            if (toggle.isOn)
            {
                foreach (var weapon in weapons)
                {
                    matchRoomManager.WeaponLimit.Unban(weapon);
                }

                return;
            }

            foreach (var weapon in weapons)
            {
                matchRoomManager.WeaponLimit.Ban(weapon);
            }
        }

        private void SyncUiFromState()
        {
            if (matchRoomManager == null)
            {
                return;
            }

            SetToggleState(ar, AssaultRifleWeapons);
            SetToggleState(smg, SmgWeapons);
            SetToggleState(sr, SniperWeapons);
            SetToggleState(sg, SpecialGunWeapons);
            SetToggleState(gr, GunnerWeapons);
            SetToggleState(mg, MachineGunWeapons);
        }

        private void SetToggleState(Toggle toggle, IReadOnlyList<eWeaponType> weapons)
        {
            if (toggle == null)
            {
                return;
            }

            toggle.isOn = !IsAnyBanned(weapons);
        }

        private bool IsAnyBanned(IReadOnlyList<eWeaponType> weapons)
        {
            if (weapons == null)
            {
                return false;
            }

            foreach (var weapon in weapons)
            {
                if (matchRoomManager.WeaponLimit.IsBanned(weapon))
                {
                    return true;
                }
            }

            return false;
        }

        private void Close()
        {
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        private void AutoBindIfNeeded()
        {
            group ??= GetComponent<CanvasGroup>();
            okButton ??= FindButton("Ok") ?? FindButton("OKButton");
            cancelButton ??= FindButton("Cancel") ?? FindButton("CancelButtn");
            ar ??= FindToggle("AR");
            smg ??= FindToggle("SMG");
            sr ??= FindToggle("SR");
            sg ??= FindToggle("SG");
            gr ??= FindToggle("GR");
            mg ??= FindToggle("MG");
        }

        private void SetupListeners()
        {
            if (listenersRegistered)
            {
                return;
            }

            if (okButton != null)
            {
                okButton.onClick.AddListener(OnOK);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancel);
            }

            listenersRegistered = true;
        }

        private Button FindButton(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var candidate in buttons)
            {
                if (candidate != null && candidate.gameObject.name == objectName)
                {
                    return candidate;
                }
            }

            return null;
        }

        private Toggle FindToggle(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            var toggles = GetComponentsInChildren<Toggle>(true);
            foreach (var candidate in toggles)
            {
                if (candidate != null && candidate.gameObject.name == objectName)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
