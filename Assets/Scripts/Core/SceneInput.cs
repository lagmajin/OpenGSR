using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    public class SceneInput : MonoBehaviour
    {
        [SerializeField] private AbstractScene scene;

        void Start()
        {
            if (scene == null)
            {
                scene = GetComponentInParent<AbstractScene>();
            }
        }

        void Update()
        {
            if (Input.anyKeyDown)
            {
                SendKeyToSceneScript();
            }
        }

        private void SendKeyToSceneScript()
        {
            if (scene == null)
            {
                scene = GetComponentInParent<AbstractScene>();
            }

            scene?.KeyPress();
        }
    }
}
