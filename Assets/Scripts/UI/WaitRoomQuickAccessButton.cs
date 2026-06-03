using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class WaitRoomQuickAccessButton : MonoBehaviour
    {
        public enum EQuickAccessType
        {
            Character,
            Map
        }

        [SerializeField] private EQuickAccessType accessType = EQuickAccessType.Character;
        [SerializeField] private WaitRoomQuickAccessController controller;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (controller == null)
            {
                controller = GetComponentInParent<WaitRoomQuickAccessController>();
            }
        }

        private void OnEnable()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        private void HandleClick()
        {
            if (controller == null)
            {
                controller = GetComponentInParent<WaitRoomQuickAccessController>();
            }

            if (controller == null)
            {
                Debug.LogWarning($"[WaitRoomQuickAccessButton] Controller not found for {name}.");
                return;
            }

            switch (accessType)
            {
                case EQuickAccessType.Character:
                    controller.ShowCharacterPanel();
                    break;
                case EQuickAccessType.Map:
                    controller.ShowMapPanel();
                    break;
            }
        }
    }
}
