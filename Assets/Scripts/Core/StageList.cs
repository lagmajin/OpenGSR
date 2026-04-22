using System;
using System.Collections.Generic;
//using KanKikuchi.AudioManager;
using UnityEngine;
using UnityEngine.SceneManagement;



namespace Assets.Scripts.Resource
{
    class StageList:MonoBehaviour
    {
        public List<string> scenes;

        private void Start()
        {

        }


        public void stages()
        {
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < sceneCount; i++)
            {
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i));
                scenes.Add(sceneName);
                Debug.Log("Scene: " + sceneName);
            }

        }
        public void dm()
        {


        }

        public void tdm()
        {

        }

        public void ctf()
        {

        }


}
}
