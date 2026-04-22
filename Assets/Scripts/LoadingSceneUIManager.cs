using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
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
        [SerializeField]private LoadingSceneCanvas canvas;


        [SerializeField] private TextMeshProUGUI textField;
        [SerializeField] private Slider progressbar;

        // Start is called before the first frame update
        void Start()
        {

            //textField.text = "a";


        }


        void OnDestroy()
        {
            //OnlineLoadingManager.Instance.UnSubscribe(this);

            //Debug.Log("Destract");
        }

        void OnApplicationQuit()
        {

        }

        public LoadingSceneCanvas LoadingSceneCanvas()
        {
            return canvas;
        }

        public void SetGameMode(OpenGSCore.EGameMode mode)
        {

        }

        public void ChangeLoadingText(string text)
        {

        }

        public void ChangeLoadingProgress(int progress)
        {
            progress = Mathf.Clamp(progress, 0, 100);

        }

        public void SetMapName(string name)
        {

        }
        [Button("�����Z�b�g")]
        public void AutoSet()
        {
            TryAssign(ref mediateObject);
            TryAssign(ref canvas);
           // TryAssign(ref textField);
        }

        private void TryAssign<T>(ref T field) where T : UnityEngine.Object
        {
            if (field != null) return;

            // �����̎q�I�u�W�F�N�g����T��
            field = GetComponentInChildren<T>(true);
            if (field != null) return;

            // �V�[���S�̂���T���i�񐄏������Ǖی��j
            field = FindFirstObjectByType<T>();
        }




    }

}