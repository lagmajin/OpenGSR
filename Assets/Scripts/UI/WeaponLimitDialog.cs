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
        private bool isEditable = true;
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

        private static readonly eWeaponType[] ShotgunWeapons =
        {
            eWeaponType.Shotgun,
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

        private void Reset()
        {
            AutoBindIfNeeded();
        }

        private void OnValidate()
        {
            AutoBindIfNeeded();
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
            ApplyInteractableState();
        }

        public void Open()
        {
            Open(true);
        }

        public void Open(bool editable)
        {
            isEditable = editable;
            gameObject.SetActive(true);
        }

        public void OpenDialog()
        {
            Open();
        }

        public void OpenDialog(bool editable)
        {
            Open(editable);
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

        public void ApplyAndClose()
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

        public void CancelAndClose()
        {
            onCancel();
        }

        private void ApplySelection()
        {
            if (matchRoomManager == null || !isEditable)
            {
                return;
            }

            ApplyCategory(ar, AssaultRifleWeapons);
            ApplyCategory(smg, SmgWeapons);
            ApplyCategory(sr, SniperWeapons);
            ApplyCategory(sg, SpecialGunWeapons);
            ApplyCategory(sg, ShotgunWeapons);
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
            SetToggleState(sg, ShotgunWeapons);
            SetToggleState(gr, GunnerWeapons);
            SetToggleState(mg, MachineGunWeapons);
        }

        private void ApplyInteractableState()
        {
            if (okButton != null)
            {
                okButton.interactable = isEditable;
            }

            SetToggleInteractable(ar);
            SetToggleInteractable(smg);
            SetToggleInteractable(sr);
            SetToggleInteractable(sg);
            SetToggleInteractable(gr);
            SetToggleInteractable(mg);
        }

        private void SetToggleState(Toggle toggle, IReadOnlyList<eWeaponType> weapons)
        {
            if (toggle == null)
            {
                return;
            }

            toggle.isOn = !IsAnyBanned(weapons);
        }

        private void SetToggleInteractable(Toggle toggle)
        {
            if (toggle == null)
            {
                return;
            }

            toggle.interactable = isEditable;
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
                group.blocksRaycasts = false;
                group.interactable = false;
            }

            gameObject.SetActive(false);
        }

        private void AutoBindIfNeeded()
        {
            group ??= GetComponent<CanvasGroup>() ?? GetComponentInChildren<CanvasGroup>(true);
            okButton ??= FindButton("Ok", "OK", "OKButton", "OkButton");
            cancelButton ??= FindButton("Cancel", "CancelButtn", "CancelButton", "CancelBtn");
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

        private void LateUpdate()
        {
            ApplyInteractableState();
        }

        private Button FindButton(params string[] objectNames)
        {
            if (objectNames == null || objectNames.Length == 0)
            {
                return null;
            }

            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var candidate in buttons)
            {
                if (candidate == null)
                {
                    continue;
                }

                foreach (var objectName in objectNames)
                {
                    if (string.IsNullOrWhiteSpace(objectName))
                    {
                        continue;
                    }

                    if (candidate.gameObject.name == objectName)
                    {
                        return candidate;
                    }
                }
            }

            Debug.LogWarning($"[WeaponLimitDialog] Button not found: {string.Join(", ", objectNames)}");
            return null;
        }

        private Toggle FindToggle(params string[] objectNames)
        {
            if (objectNames == null || objectNames.Length == 0)
            {
                return null;
            }

            var toggles = GetComponentsInChildren<Toggle>(true);
            foreach (var candidate in toggles)
            {
                if (candidate == null)
                {
                    continue;
                }

                foreach (var objectName in objectNames)
                {
                    if (string.IsNullOrWhiteSpace(objectName))
                    {
                        continue;
                    }

                    if (candidate.gameObject.name == objectName)
                    {
                        return candidate;
                    }
                }
            }

            Debug.LogWarning($"[WeaponLimitDialog] Toggle not found: {string.Join(", ", objectNames)}");
            return null;
        }
    }
}
