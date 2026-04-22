

using UnityEngine;

namespace OpenGS
{
    public interface IWarpPoint
    {

    }
    [DisallowMultipleComponent]
    public class WarpPoint:MonoBehaviour,IWarpPoint
    {
        public bool enableWarp = true;

        public GameObject point1;
        public GameObject point2;

        public GameObject warpEffectPosition;
        public GameObject warpEffect;

        public AudioClip warpsound;

        private void Start()
        {
            
        }

        private void Update()
        {
            
        }

        private void Warp()
        {

        }

        private void OnCollisionEnter2D(Collision2D collision)
        {



        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            var gameObject = collision.gameObject;

            var tags = gameObject.GetComponent<IMultipleTags>();


            /*

            if (tags.HasPlayerTag())
            {
                if(gameObject.TryGetComponent<AbstractPlayer>(out var player))
                {
                    collision.gameObject.transform.position = point2.transform.position;

                    Instantiate(warpEffect, point2.transform.position, Quaternion.identity);
                }


   

            }


            */
        }



    }



}
