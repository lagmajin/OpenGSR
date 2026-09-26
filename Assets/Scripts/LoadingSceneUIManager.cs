using Sirenix.OdinInspector;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace OpenGS
{
    public interface ILoadingSceneUIManagerProvider
    {
    }

    [DisallowMultipleComponent]
    public class LoadingSceneUIManager : MonoBehaviour
    {
        [SerializeField] private OnlineLoadingSceneMediateObject mediateObject;
        [SerializeField] private LoadingSceneCanvas canvas;
        [SerializeField] private TextMeshProUGUI textField;
        [SerializeField] private Slider progressbar;
        [SerializeField] private OnlineLoadingScene onlineLoadingScene;

        [Inject] private OnlineLoadingManager onlineLoadingManager;

        private void OnDestroy()
        {
            if (onlineLoadingManager != null)
            {
                onlineLoadingManager.LoadingMessageUpdated -= ChangeLoadingText;
                onlineLoadingManager.LoadingInfoUpdated -= HandleLoadingInfoUpdated;
                onlineLoadingManager.LoadingProgressUpdated -= HandleNetworkLoadingProgress;
            }
        }

        void Start()
        {
            AutoSet();

            if (onlineLoadingManager == null)
            {
                onlineLoadingManager = OnlineLoadingManager.Instance;
                Debug.LogWarning("[LoadingSceneUIManager] OnlineLoadingManager was not injected; using shared fallback instance.");
            }

            if (onlineLoadingScene != null)
            {
                onlineLoadingScene.Progress
                    .Subscribe(value =>
                    {
                        var safeProgress = float.IsFinite(value) ? Mathf.Clamp01(value) : 0f;
                        ChangeLoadingProgress(Mathf.RoundToInt(safeProgress * 100f));
                    })
                    .AddTo(this);
            }

            if (onlineLoadingManager != null)
            {
                onlineLoadingManager.LoadingMessageUpdated += ChangeLoadingText;
                onlineLoadingManager.LoadingInfoUpdated += HandleLoadingInfoUpdated;
                if (onlineLoadingScene == null)
                {
                    onlineLoadingManager.LoadingProgressUpdated += HandleNetworkLoadingProgress;
                }
                HandleLoadingInfoUpdated(onlineLoadingManager.LoadingInfo);
                ChangeLoadingText(onlineLoadingManager.LoadingMessage);
            }
        }

        public LoadingSceneCanvas LoadingSceneCanvas()
        {
            return canvas;
        }

        public void SetGameMode(OpenGSCore.EGameMode mode)
        {
            ChangeLoadingText(mode.ToString());
        }

        public void ChangeLoadingText(string text)
        {
            if (textField != null)
            {
                textField.text = text ?? string.Empty;
            }
        }

        public void ChangeLoadingProgress(int progress)
        {
            progress = Mathf.Clamp(progress, 0, 100);
            if (progressbar != null)
            {
                progressbar.value = progress / 100f;
            }
        }

        private void HandleNetworkLoadingProgress(string playerId, float progress)
        {
            if (onlineLoadingScene != null)
            {
                return;
            }

            progress = float.IsFinite(progress) ? Mathf.Clamp01(progress) : 0f;
            ChangeLoadingProgress(Mathf.RoundToInt(progress * 100f));
        }

        public void SetMapName(string name)
        {
            ChangeLoadingText(name);
        }

        private void HandleLoadingInfoUpdated(LoadingInfo info)
        {
            if (info == null)
            {
                return;
            }

            SetMapName(info.MapName);
        }

        [Button("AutoSet")]
        public void AutoSet()
        {
            TryAssign(ref mediateObject);
            TryAssign(ref canvas);
            TryAssign(ref textField);
            TryAssign(ref progressbar);
            TryAssign(ref onlineLoadingScene);
        }

        private void TryAssign<T>(ref T field) where T : UnityEngine.Object
        {
            if (field != null)
            {
                return;
            }

            field = GetComponentInChildren<T>(true);
            if (field != null)
            {
                return;
            }

            field = FindFirstObjectByType<T>();
        }
    }
}
