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
            if (jumpPower < 0f)
            {
                jumpPower = 0f;
            }
        }

        private void Update()
        {
        }

        private void AddForce(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            if (obj.TryGetComponent<Rigidbody2D>(out var body))
            {
                body.velocity = new Vector2(body.velocity.x, 0f);
                body.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
                return;
            }

            if (obj.TryGetComponent<IPlayer>(out var player) && obj.TryGetComponent<Rigidbody2D>(out var playerBody))
            {
                playerBody.velocity = new Vector2(playerBody.velocity.x, 0f);
                playerBody.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            AddForce(collision.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            AddForce(collision.gameObject);
        }

    }
}
