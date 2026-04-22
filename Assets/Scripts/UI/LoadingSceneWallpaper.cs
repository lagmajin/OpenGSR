
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    internal interface IBattleSceneWallpaper
    {

    }

    [DisallowMultipleComponent]
    public class LoadingSceneWallpaper : MonoBehaviour
    {

        [SerializeField][Required]public Image viewer;



        [SerializeField] public Sprite[] images;

        //public GameObject[] objects;


        private void Start()
        {
            if (images != null && images.Length > 0)
            {
                var r = new System.Random();
                int index = r.Next(0, images.Length);
                viewer.sprite = images[index];
            }



        }
        private void gotoBattleScene()
        {

        }

        public void test(string test)
        {

        }

    }

}
