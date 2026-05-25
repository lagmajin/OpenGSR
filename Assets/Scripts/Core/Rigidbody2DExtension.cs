using UnityEngine;

namespace OpenGS
{
    public static class Rigidbody2DExtension
    {
        public static void FreezeRotation()
        {
            foreach (var body in Object.FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None))
            {
                if (body != null)
                {
                    body.freezeRotation = true;
                }
            }
        }

        public static void SetFreezeRotation(Rigidbody2D self, bool freeze)
        {
            if (self != null)
            {
                self.freezeRotation = freeze;
            }
        }

        public static void EnableRotation(Rigidbody2D self)
        {
            SetFreezeRotation(self, false);
        }

        public static void DisableRotation(Rigidbody2D self)
        {
            SetFreezeRotation(self, true);
        }

        public static void SetGravityScale(Rigidbody2D self, float gravityScale)
        {
            if (self != null)
            {
                self.gravityScale = Mathf.Max(0f, gravityScale);
            }
        }

        public static void EnableGravity(Rigidbody2D self)
        {
            SetGravityScale(self, 1f);
        }

        public static void DisableGravity(Rigidbody2D self)
        {
            SetGravityScale(self, 0f);
        }

        public static void ResetVelocity(Rigidbody2D self)
        {
            if (self == null)
            {
                return;
            }

            self.linearVelocity = Vector2.zero;
            self.angularVelocity = 0f;
        }
    }
}
