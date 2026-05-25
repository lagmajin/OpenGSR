using System.Collections.Generic;
using System.Linq;
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
            FilterScenes("DM", "DeathMatch");
        }

        public void tdm()
        {
            FilterScenes("TDM", "TeamDeathMatch");
        }

        public void ctf()
        {
            FilterScenes("CTF", "CaptureTheFlag");
        }

        private void FilterScenes(params string[] keywords)
        {
            stages();
            scenes = scenes.Where(scene => keywords.Any(keyword => scene.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            Debug.Log($"[StageList] filtered => {string.Join(",", scenes)}");
        }
    }
}
