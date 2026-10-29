using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// Lightweight visual replay for projectiles simulated authoritatively by the server.
    /// </summary>
    public sealed class RemoteProjectileFlight : MonoBehaviour
    {
        private Vector2 velocity;
        private float gravityScale;
        private float remainingLifetime;

        public void Initialize(Vector2 initialVelocity, float projectileGravityScale, float lifetime)
        {
            velocity = initialVelocity;
            gravityScale = projectileGravityScale;
            remainingLifetime = lifetime;
        }

        private void Update()
        {
            var delta = Time.deltaTime;
            velocity += Vector2.down * (9.81f * gravityScale * delta);
            transform.position += (Vector3)(velocity * delta);
            if (velocity.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.Euler(
                    0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            }

            remainingLifetime -= delta;
            if (remainingLifetime <= 0f) Destroy(gameObject);
        }
    }
}
