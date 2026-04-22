using UnityEngine;

namespace OpenGS
{
    public static class DebugLogExtension
    {
        public static void LogToString(this Debug debug, object obj)
        {
            Debug.Log(obj.ToString());
        }
    }
}
