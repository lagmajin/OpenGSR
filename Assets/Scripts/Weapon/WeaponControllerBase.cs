
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

#pragma warning disable 0414

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class WeaponControllerBase : OpenGSBaseClass
    {
        public Transform gun;

        Vector2 direction;

        private AudioSource aSource = null;
        private Camera cachedCamera;

        void Update()
        {
            if (cachedCamera == null)
            {
                cachedCamera = Camera.main;
            }

            var camera = cachedCamera;
            if (camera == null)
            {
                return;
            }

            var screenPos = camera.WorldToScreenPoint(transform.position);
            var direction = Input.mousePosition - screenPos;

            var trans = transform.localScale;
            if (direction.x >= 0)
            {


                trans.x = 1;

            }
            else
            {

                trans.x = -1;


            }

            transform.localScale = trans;
        }

        public void shot()
        {

        }


    }
}
