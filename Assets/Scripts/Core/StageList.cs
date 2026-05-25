using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenGS
{
    class StageList : MonoBehaviour
    {
        public List<string> scenes = new();

        public void stages()
        {
            scenes.Clear();

            int sceneCount = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < sceneCount; i++)
            {
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
                scenes.Add(sceneName);
                Debug.Log("Scene: " + sceneName);
            }
        }

        public void dm()
        {
            Debug.Log("[StageList] dm");
        }

        public void tdm()
        {
            Debug.Log("[StageList] tdm");
        }

        public void ctf()
        {
            Debug.Log("[StageList] ctf");
        }
    }
}
