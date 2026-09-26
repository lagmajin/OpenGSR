using UnityEngine;

namespace OpenGS.Network
{
    /// <summary>
    /// Quaternion helpers. UnityEngine.Quaternion has no sqrMagnitude, so any
    /// code that needs to reject a degenerate rotation before normalising it
    /// has to measure the length explicitly.
    /// </summary>
    public static class QuaternionMath
    {
        private const float DegenerateEpsilon = 0.000001f;

        public static float SqrMagnitude(Quaternion value)
        {
            return value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w;
        }

        public static bool IsDegenerate(Quaternion value)
        {
            return !float.IsFinite(value.x) || !float.IsFinite(value.y) ||
                   !float.IsFinite(value.z) || !float.IsFinite(value.w) ||
                   SqrMagnitude(value) < DegenerateEpsilon;
        }

        public static Quaternion Normalize(Quaternion value)
        {
            return IsDegenerate(value) ? Quaternion.identity : Quaternion.Normalize(value);
        }
    }
}