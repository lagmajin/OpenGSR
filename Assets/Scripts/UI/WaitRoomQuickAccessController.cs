using System;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class WaitRoomQuickAccessController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button characterButton;
        [SerializeField] private Button mapButton;

        [Header("Panels")]
        [SerializeField] private GameObject characterPanel;
        [SerializeField] private GameObject mapPanel;

        [Header("Behavior")]
        [SerializeField] private bool hideOtherPanels = true;
        [SerializeField] private GameObject[] panelsToHide;

        private Action characterButtonHandler;
        private Action mapButtonHandler;

        private void Awake()
        {
            AutoBind();
            SetupHandlers();
        }

        private void OnEnable()
        {
            BindButtons();
        }

        private void OnDisable()
        {
            UnbindButtons();
        }

        public void ShowCharacterPanel()
        {
            if (hideOtherPanels)
            {
                HideConfiguredPanelsExcept(characterPanel);
            }

            SetPanelActive(characterPanel, true);
        }

        public void ShowMapPanel()
        {
            if (hideOtherPanels)
            {
                HideConfiguredPanelsExcept(mapPanel);
            }

            SetPanelActive(mapPanel, true);
        }

        public void HideAllPanels()
        {
            SetPanelActive(characterPanel, false);
            SetPanelActive(mapPanel, false);

            if (panelsToHide == null)
            {
                return;
            }

            foreach (var panel in panelsToHide)
            {
                SetPanelActive(panel, false);
            }
        }

        private void AutoBind()
        {
            if (characterButton == null)
            {
                characterButton = FindButtonByName("CharacterButton", "CharaButton", "Character");
            }

            if (mapButton == null)
            {
                mapButton = FindButtonByName("MapButton", "MapSelectButton", "Map");
            }
        }

        private void SetupHandlers()
        {
            characterButtonHandler = ShowCharacterPanel;
            mapButtonHandler = ShowMapPanel;
        }

        private void BindButtons()
        {
            if (characterButton != null)
            {
                characterButton.onClick.RemoveListener(characterButtonHandler.Invoke);
                characterButton.onClick.AddListener(characterButtonHandler.Invoke);
            }

            if (mapButton != null)
            {
                mapButton.onClick.RemoveListener(mapButtonHandler.Invoke);
                mapButton.onClick.AddListener(mapButtonHandler.Invoke);
            }
        }

        private void UnbindButtons()
        {
            if (characterButton != null)
            {
                characterButton.onClick.RemoveListener(characterButtonHandler.Invoke);
            }

            if (mapButton != null)
            {
                mapButton.onClick.RemoveListener(mapButtonHandler.Invoke);
            }
        }

        private void HideConfiguredPanelsExcept(GameObject keepPanel)
        {
            if (panelsToHide != null)
            {
                foreach (var panel in panelsToHide)
                {
                    if (panel != null && panel != keepPanel)
                    {
                        panel.SetActive(false);
                    }
                }
            }

            if (characterPanel != null && characterPanel != keepPanel)
            {
                characterPanel.SetActive(false);
            }

            if (mapPanel != null && mapPanel != keepPanel)
            {
                mapPanel.SetActive(false);
            }
        }

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }

        private Button FindButtonByName(params string[] names)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            if (buttons == null || buttons.Length == 0)
            {
                return null;
            }

            foreach (var button in buttons)
            {
                if (button == null)
                {
                    continue;
                }

                foreach (var name in names)
                {
                    if (!string.IsNullOrWhiteSpace(name) && string.Equals(button.name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return button;
                    }
                }
            }

            return null;
        }
    }
}
