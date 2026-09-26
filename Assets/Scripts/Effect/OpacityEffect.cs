using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class OpacityEffect : MonoBehaviour
    {
        [Range(0f, 1f)]
        public float opacity = 1.0f;

        public Image img;
        public SpriteRenderer render;

        private void Awake()
        {
            AutoBind();
            ApplyOpacity();
        }

        private void OnValidate()
        {
            opacity = Mathf.Clamp01(opacity);
            AutoBind();
            ApplyOpacity();
        }

        public void SetOpacity(float value)
        {
            opacity = Mathf.Clamp01(value);
            ApplyOpacity();
        }

        private void AutoBind()
        {
            if (img == null) img = GetComponent<Image>();
            if (render == null) render = GetComponent<SpriteRenderer>();
        }

        private void ApplyOpacity()
        {
            var alpha = Mathf.Clamp01(opacity);
            if (img != null)
            {
                var color = img.color;
                color.a = alpha;
                img.color = color;
            }

            if (render != null)
            {
                var color = render.color;
                color.a = alpha;
                render.color = color;
            }
        }
    }
}
