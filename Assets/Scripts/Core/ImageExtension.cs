
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    public static class ImageExtension
    {
        public static void SetOpacity(this Image image, float alpha)
        {
            var c = image.color;
            image.color = new Color(c.r, c.g, c.b, alpha);
        }

    }

    public static class SpriteRenderExtension
    {
        public static void SetOpacity(this SpriteRenderer render,float alpha)
        {

        }

    }

}
