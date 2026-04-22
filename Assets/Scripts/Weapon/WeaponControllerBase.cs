
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

        void Start()
        {

        }

        void Update()
        {
            var screenPos = Camera.main.WorldToScreenPoint(transform.position);
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

        private void OnCollisionEnter2D(Collision2D collision)
        {

        }

        public void shot()
        {

        }


    }
}
