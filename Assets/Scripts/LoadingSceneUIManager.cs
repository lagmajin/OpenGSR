using Sirenix.OdinInspector;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

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

        void Start()
        {
            AutoSet();

            if (onlineLoadingScene != null)
            {
                onlineLoadingScene.Progress
                    .Subscribe(value => ChangeLoadingProgress(Mathf.RoundToInt(value * 100f)))
                    .AddTo(this);
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

        public void SetMapName(string name)
        {
            ChangeLoadingText(name);
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
