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

        private void Awake()
        {
            jumpPower = float.IsFinite(jumpPower) ? Mathf.Max(0f, jumpPower) : 0f;
        }

        private void OnValidate()
        {
            if (!float.IsFinite(jumpPower)) jumpPower = 0f;
            jumpPower = Mathf.Max(0f, jumpPower);
        }

        private void Start()
        {
            if (jumpPower < 0f)
            {
                jumpPower = 0f;
            }
        }

        private void AddForce(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            var body = obj.GetComponentInParent<Rigidbody2D>();
            if (body != null)
            {
                var velocityX = float.IsFinite(body.linearVelocity.x) ? body.linearVelocity.x : 0f;
                body.linearVelocity = new Vector2(velocityX, 0f);
                body.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
                return;
            }

            var player = obj.GetComponentInParent<IPlayer>();
            var playerBody = obj.GetComponentInParent<Rigidbody2D>();
            if (player != null && playerBody != null)
            {
                var velocityX = float.IsFinite(playerBody.linearVelocity.x) ? playerBody.linearVelocity.x : 0f;
                playerBody.linearVelocity = new Vector2(velocityX, 0f);
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
