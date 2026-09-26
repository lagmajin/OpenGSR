using UnityEngine;

namespace OpenGS
{
    public static class TransformExtension
    {
        public static void InvertLocalX(this Transform self)
        {
            if (self == null)
            {
                return;
            }

            var local = self.localScale;
            local.x = float.IsFinite(local.x) ? local.x * -1f : -1f;
            self.localScale = local;
        }

        public static void SetLocalScaleX(this Transform self, float scale)
        {
            if (self == null)
            {
                return;
            }

            var local = self.localScale;
            local.x = float.IsFinite(scale) ? scale : 1f;
            self.localScale = local;
        }

        public static void SetLocalScaleY(this Transform self, float scale)
        {
            if (self == null)
            {
                return;
            }

            var local = self.localScale;
            local.y = float.IsFinite(scale) ? scale : 1f;
            self.localScale = local;
        }

        public static void AddLocalPosition()
        {
            Debug.LogWarning("[TransformExtension] AddLocalPosition called without target transform");
        }

        public static void AddLocalPosition(this Transform self, Vector3 delta)
        {
            if (self != null && IsFinite(delta))
            {
                self.localPosition += delta;
            }
        }

        private static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        }

        public static void SetRotationX()
        {
            Debug.LogWarning("[TransformExtension] SetRotationX called without target transform");
        }

        public static void SetRotationX(this Transform self, float x)
        {
            if (self == null)
            {
                return;
            }

            var euler = self.localEulerAngles;
            euler.x = float.IsFinite(x) ? x : 0f;
            self.localEulerAngles = euler;
        }

        public static void SetRotationZ(this Transform self)
        {
            if (self == null)
            {
                return;
            }

            var euler = self.localEulerAngles;
            euler.z = 0f;
            self.localEulerAngles = euler;
        }

        public static void SetRotationZ(this Transform self, float z)
        {
            if (self == null)
            {
                return;
            }

            var euler = self.localEulerAngles;
            euler.z = float.IsFinite(z) ? z : 0f;
            self.localEulerAngles = euler;
        }



    }

}
