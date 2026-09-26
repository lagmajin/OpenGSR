using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace OpenGS
{
    public enum EVirtualInputButtonAction
    {
        Fire,
        Booster,
        Jump,
        SwapWeapon,
        DropWeapon,
        Grenade,
        Reload
    }

    /// <summary>
    /// UIボタンの押下をVirtualInputServiceへ橋渡しする。
    /// 長押し入力はPointerUpで必ず解放し、画面遷移時の入力残留も防ぐ。
    /// </summary>
    public sealed class VirtualInputButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private EVirtualInputButtonAction action;

        [Inject] private VirtualInputService virtualInputService;

        private void Awake()
        {
            if (virtualInputService != null)
            {
                return;
            }

            try
            {
                virtualInputService = DependencyInjectionConfig.Resolve<VirtualInputService>();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[VirtualInputButton] VirtualInputService is unavailable: {ex.Message}");
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (virtualInputService == null)
            {
                return;
            }

            switch (action)
            {
                case EVirtualInputButtonAction.Fire:
                    virtualInputService.SetFire(true);
                    break;
                case EVirtualInputButtonAction.Booster:
                    virtualInputService.SetBooster(true);
                    break;
                case EVirtualInputButtonAction.Jump:
                    virtualInputService.PressJump();
                    break;
                case EVirtualInputButtonAction.SwapWeapon:
                    virtualInputService.PressSwapWeapon();
                    break;
                case EVirtualInputButtonAction.DropWeapon:
                    virtualInputService.PressDropWeapon();
                    break;
                case EVirtualInputButtonAction.Grenade:
                    virtualInputService.PressGrenade();
                    break;
                case EVirtualInputButtonAction.Reload:
                    virtualInputService.PressReload();
                    break;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ReleaseHeldInput();
        }

        private void OnDisable()
        {
            ReleaseHeldInput();
        }

        private void ReleaseHeldInput()
        {
            if (virtualInputService == null)
            {
                return;
            }

            switch (action)
            {
                case EVirtualInputButtonAction.Fire:
                    virtualInputService.SetFire(false);
                    break;
                case EVirtualInputButtonAction.Booster:
                    virtualInputService.SetBooster(false);
                    break;
            }
        }
    }
}
