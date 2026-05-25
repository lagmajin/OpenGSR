using UnityEngine;

namespace OpenGS
{
    public static class CustomRenderer
    {
        public static void SetRendererEnabled(Renderer renderer, bool enabled)
        {
            if (renderer != null)
            {
                renderer.enabled = enabled;
            }
        }

        public static void SetRendererVisible(Renderer renderer, bool visible)
        {
            SetRendererEnabled(renderer, visible);
        }

        public static void SetSpriteColor(SpriteRenderer renderer, Color color)
        {
            if (renderer != null)
            {
                renderer.color = color;
            }
        }

        public static void SetSpriteSortingOrder(SpriteRenderer renderer, int sortingOrder)
        {
            if (renderer != null)
            {
                renderer.sortingOrder = sortingOrder;
            }
        }

        public static void SetSpriteSortingLayer(SpriteRenderer renderer, string sortingLayerName)
        {
            if (renderer != null && !string.IsNullOrEmpty(sortingLayerName))
            {
                renderer.sortingLayerName = sortingLayerName;
            }
        }

        public static void SetSpriteAlpha(SpriteRenderer renderer, float alpha)
        {
            if (renderer == null)
            {
                return;
            }

            var color = renderer.color;
            color.a = Mathf.Clamp01(alpha);
            renderer.color = color;
        }
    }
}
