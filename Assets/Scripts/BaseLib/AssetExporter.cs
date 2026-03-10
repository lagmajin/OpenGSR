using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class AssetExporter:MonoBehaviour
    {

        void Start()
        {

        }


        [Button("アセットテスト")]
        public void ExportAll(in string directory)
        {
            var renderers = GetComponentsInChildren<SpriteRenderer>();


            foreach (var r in renderers)
            {
                var png=r.sprite.texture.EncodeToPNG();

                

                var name=r.sprite.name;

                Debug.Log("Name"+name);


                //File.WriteAllBytes(name,png);

            }


            /*
            var clips = GetComponents<AudioClip>();


            foreach (var c in clips)
            {
                var name=c.name;

                //SavWav.Save(name,c);
            }


            */
        }

    }
}
