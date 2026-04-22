using UnityEditor;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class DebugRenderer : MonoBehaviour
    {
        public SpriteRenderer render;
        public Color wallColor = Color.green;
        private void Start()
        {
#if UNITY_EDITOR
            Debug.Log("Unity Editor");
#else
    Debug.Log("Any other platform");

#endif

#if UNITY_EDITOR

            if (EditorApplication.isPlaying)
            {

                render.sprite = null;


            }

#endif

        }

        private void Update()
        {

        }

        private void OnCollisionEnter2D(Collision2D collision)
        {

        }

    }
}