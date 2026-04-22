using UnityEngine;

namespace OpenGS
{
    public static class TransformExtension
    {
        public static void InvertLocalX(this Transform self)
        {
            var local = self.localScale;

            local.x = local.x*-1;

            self.localScale = local;

        }

        public static void  SetLocalScaleX(this Transform self,float scale)
        {
            var local=self.localScale;

            local.x = scale;

            self.localScale = local;

        }

        public static void SetLocalScaleY(this Transform self, float scale)
        {
            var local = self.localScale;

            local.y = scale;

            self.localScale = local;

        }

        public static void AddLocalPosition()
        {

        }

        public static void SetRotationX()
        {

        }

        public static void SetRotationZ(this Transform self)
        {

        }



    }

}
