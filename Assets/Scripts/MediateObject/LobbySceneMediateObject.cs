using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class LobbySceneMediateObject : AbstractMediateObject, ILobbyMediateObject
    {
        [SerializeField, FormerlySerializedAs("createNewRoomDialog")] private AbstractCreateNewRoomDialog createNewRoomDialogPrefab;

        private AbstractCreateNewRoomDialog runtimeCreateNewRoomDialog;

        public AbstractCreateNewRoomDialog CurrentCreateNewRoomDialog => runtimeCreateNewRoomDialog;

        public GeneralSceneMasterData GeneralSceneMasterData()
        {
            return OpenGS.GeneralSceneMasterData.Instance();
        }

        public AbstractCreateNewRoomDialog GetOrCreateCreateNewRoomDialog(Transform parent = null)
        {
            if (runtimeCreateNewRoomDialog != null)
            {
                return runtimeCreateNewRoomDialog;
            }

            if (createNewRoomDialogPrefab == null)
            {
                Debug.LogWarning("LobbySceneMediateObject: createNewRoomDialogPrefab is not assigned.");
                return null;
            }

            runtimeCreateNewRoomDialog = Instantiate(createNewRoomDialogPrefab);
            runtimeCreateNewRoomDialog.gameObject.name = createNewRoomDialogPrefab.gameObject.name;

            if (parent != null)
            {
                runtimeCreateNewRoomDialog.transform.SetParent(parent, false);
            }

            ConfigureRuntimeDialog(runtimeCreateNewRoomDialog);
            runtimeCreateNewRoomDialog.gameObject.SetActive(false);
            return runtimeCreateNewRoomDialog;
        }

        public void ShowCreateNewRoomDialog(Transform parent = null)
        {
            var dialog = GetOrCreateCreateNewRoomDialog(parent);
            if (dialog == null)
            {
                return;
            }

            dialog.ShowDialog();
        }

        public void HideCreateNewRoomDialog()
        {
            if (runtimeCreateNewRoomDialog != null)
            {
                runtimeCreateNewRoomDialog.gameObject.SetActive(false);
            }
        }

        private static void ConfigureRuntimeDialog(AbstractCreateNewRoomDialog dialog)
        {
            var canvas = dialog.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
                canvas.overrideSorting = true;
                canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 20);
            }

            if (dialog.transform is not RectTransform rect)
            {
                return;
            }

            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.anchoredPosition3D = Vector3.zero;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
