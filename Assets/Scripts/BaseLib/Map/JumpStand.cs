using UnityEngine;

namespace OpenGS
{
    public interface IJumpable { 

    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class JumpStand : MonoBehaviour,IJumpable
    {
        
        public float jumpPower=10.0f;

        private void Start()
        {

        }

        private void Update()
        {

        }

        private void AddForce(GameObject obj)
        {
           


        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            

        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            
        }

    }
}
