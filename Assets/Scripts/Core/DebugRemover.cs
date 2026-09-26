#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OpenGS
{
    public class DebugRemover : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(UnityEngine.SceneManagement.Scene scene, BuildReport report)
        {
            if (report == null || !report.summary.options.HasFlag(BuildOptions.Development))
            {
                return;
            }
        }
    }
}
#endif
