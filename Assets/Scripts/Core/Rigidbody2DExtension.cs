using UnityEngine;

namespace OpenGS
{
    public static class Rigidbody2DExtension
    {
        public static void FreezeRotation()
        {

        }

        public static void EnableRotation(Rigidbody2D self)
        {
            self.freezeRotation = true;
        }
        public static void DisableRotation(Rigidbody2D self)
        {
            self.freezeRotation = false;
        }

        public static void EnableGravity(Rigidbody2D self)
        {
           
        }

        public static void DisableGravity(Rigidbody2D self)
        {

        }
    


    }
}
