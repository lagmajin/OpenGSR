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
            if (UnityEditor.EditorApplication.isPlaying && render != null)
            {
                render.sprite = null;
            }
#else
            Debug.Log("Any other platform");
#endif
        }

    }
}
