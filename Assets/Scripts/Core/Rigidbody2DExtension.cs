using UnityEngine;

namespace OpenGS
{
    public static class Rigidbody2DExtension
    {
        public static void FreezeRotation()
        {
            foreach (var body in Object.FindObjectsOfType<Rigidbody2D>())
            {
                body.freezeRotation = true;
            }
        }

        public static void EnableRotation(Rigidbody2D self)
        {
            if (self != null)
            {
                self.freezeRotation = false;
            }
        }

        public static void DisableRotation(Rigidbody2D self)
        {
            if (self != null)
            {
                self.freezeRotation = true;
            }
        }

        public static void EnableGravity(Rigidbody2D self)
        {
            if (self != null)
            {
                self.gravityScale = 1f;
            }
        }

        public static void DisableGravity(Rigidbody2D self)
        {
            if (self != null)
            {
                self.gravityScale = 0f;
            }
        }
    }
}
