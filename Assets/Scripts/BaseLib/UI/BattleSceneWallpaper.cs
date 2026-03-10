
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{

    [DisallowMultipleComponent]
    public class BattleSceneWallpaper : MonoBehaviour
    {
        List<string> backgroundName = new List<string>();



        [Required][SerializeField]public List<GameObject> images;


        private void Start()
        {
            var r1 = new System.Random();

            var count = images.Count;


            var r = r1.Next(0, count);

            images[r].SetActive(true);


            


        }

        public void test(string test)
        {

        }

    }

}
