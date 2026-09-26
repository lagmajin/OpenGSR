

using System;
using System.Collections;
using UnityEngine;

namespace OpenGS
{

    public class Functions
    {
        public static IEnumerator WaitAfterAction(Action func,float time = 0.0f)
        {
            var safeTime = float.IsFinite(time) ? Mathf.Max(0f, time) : 0f;
            yield return new WaitForSeconds(safeTime);

            if (func == null)
            {
                yield break;
            }

            try
            {
                func.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Functions] delayed action failed: {ex}");
            }
        }

        public static void NullFunc()
        {


        }

    }

}
