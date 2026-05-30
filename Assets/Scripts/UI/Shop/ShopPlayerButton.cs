using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class ShopPlayerButton : MonoBehaviour
    {
        private Button button;

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            CacheReferences();

            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
                button.onClick.AddListener(HandleClick);
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        private void CacheReferences()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }

        private void HandleClick()
        {
            var shopScene = FindFirstObjectByType<ShopScene>();
            if (shopScene != null)
            {
                shopScene.ChangeTab("player");
                return;
            }

            var shopUIManager = FindFirstObjectByType<ShopUIManager>();
            if (shopUIManager != null)
            {
                shopUIManager.SwitchCategory(EShopCategory.Character).Forget();
            }
        }
    }
}
