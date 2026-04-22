

using System;
using System.Collections;
using UnityEngine;

namespace OpenGS
{

    public class Functions
    {
        public static IEnumerator WaitAfterAction(Action func,float time = 0.0f)
        {
            yield return new WaitForSeconds(time);

            func();
        }

        public static void NullFunc()
        {


        }

    }

}
