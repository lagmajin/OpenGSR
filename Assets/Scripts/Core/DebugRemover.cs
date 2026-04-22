using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenGS
{
    public class DebugRemover : IProcessSceneWithReport
    {

        public int callbackOrder
        {
            get { return 0; }
        }

        public void OnProcessScene(UnityEngine.SceneManagement.Scene scene, BuildReport report)
        {
            if (report == null)
            {
                Debug.LogError("BuildReport is null.");
                return;
            }

            if (scene == null)
            {
                Debug.LogError("Scene is null.");
                return;
            }

            if (report.summary.options.HasFlag(BuildOptions.Development))
            {


            }
        }
    }
}
