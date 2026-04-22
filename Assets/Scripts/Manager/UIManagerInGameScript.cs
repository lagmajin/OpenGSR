



using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenGSCore;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class UIManagerInGameScript:MonoBehaviour
    {
        [SerializeField]private LobbySceneMediateObject mediateObject;

        [ShowInInspector] private List<CommonCanvas> canvas = new();

        void Start()
        {

            FindAllUICanvas();

        }
        public static T[] GetComponentsInActiveScene<T>(bool includeInactive = true)
        {
            // ActiveなSceneのRootにあるGameObject[]を取得する
            var rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();

            // 空の IEnumerable<T>
            IEnumerable<T> resultComponents = (T[])Enumerable.Empty<T>();
            foreach (var item in rootGameObjects)
            {
                // includeInactive = true を指定するとGameObjectが非活性なものからも取得する
                var components = item.GetComponentsInChildren<T>(includeInactive);
                resultComponents = resultComponents.Concat(components);
            }
            return resultComponents.ToArray();
        }
        void FindAllUICanvas()
        {
            var list=GetComponentsInActiveScene<CommonCanvas>();

            canvas = new List<CommonCanvas>(list);

        }

        [Button("UI暗転テスト")]
        public void DisableUIAllUI()
        {
            foreach (var c in canvas)
            {
                c.DisableUI();
                
            }

        }

        [Button("")]
        public void ShowCreateRoomDialog()
        {

            Debug.Log("Test");

            mediateObject.createNewRoomDialog.ShowDialog();
        }




    }
}
