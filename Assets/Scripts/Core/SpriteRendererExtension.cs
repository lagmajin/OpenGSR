using UnityEngine;

namespace OpenGS
{
    public static class SpriteRendererExtension
    {
        public static void SetOpacity(SpriteRenderer self, float alpha = 1f)
        {
            if (self == null)
            {
                return;
            }

            var color = self.color;
            color.a = Mathf.Clamp01(alpha);
            self.color = color;
        }
    }
}
