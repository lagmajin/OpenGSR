
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    public static class ImageExtension
    {
        public static void SetOpacity(this Image image, float alpha)
        {
            if (image == null)
            {
                return;
            }

            var c = image.color;
            image.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(alpha));
        }

    }

    public static class SpriteRenderExtension
    {
        public static void SetOpacity(this SpriteRenderer render,float alpha)
        {
            if (render == null)
            {
                return;
            }

            var c = render.color;
            render.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(alpha));
        }

    }

}
