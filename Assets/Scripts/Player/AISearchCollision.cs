using UnityEngine;


namespace OpenGS
{
    public enum eAISearchBoxSize
    {

    }

    public class AISearchCollision : MonoBehaviour
    {
        public GameObject player;

        private void SendNewTarget(GameObject target)
        {
            var aiif = player.GetComponent<IAIPlayerController>();

            aiif?.SetStrength();


        }

        private void SendOutTarget(GameObject target)
        {

        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            //Debug.Log("Enter");

            SendNewTarget(collision.gameObject);
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            //Debug.Log("Stay");
        }

        private void OnCollisionExit2D(Collision2D collision)
        {

            SendOutTarget(collision.gameObject);
        }

    }
}
