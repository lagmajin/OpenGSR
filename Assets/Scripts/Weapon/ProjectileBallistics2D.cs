using UnityEngine;

namespace OpenGS
{
    internal sealed class ProjectileBallistics2D
    {
        private Vector2 direction = Vector2.right;
        private Vector2 velocity = Vector2.right;
        private float speed;
        private float gravityStrength = 18f;
        private bool gravityEnabled;
        private bool alignToVelocity = true;
        private float spriteAngleOffset;

        public Vector2 Direction => direction;
        public Vector2 Velocity => velocity;
        public float Speed => speed;

        public void Configure(
            Vector2 initialDirection,
            float initialSpeed,
            bool enableGravity,
            float gravity,
            bool alignVelocity,
            float angleOffset)
        {
            direction = initialDirection.sqrMagnitude > 0f ? initialDirection.normalized : Vector2.right;
            speed = Mathf.Max(0f, initialSpeed);
            gravityStrength = Mathf.Max(0f, gravity);
            gravityEnabled = enableGravity;
            alignToVelocity = alignVelocity;
            spriteAngleOffset = angleOffset;
            velocity = direction * speed;
        }

        public void SetDirection(Vector2 newDirection)
        {
            direction = newDirection.sqrMagnitude > 0f ? newDirection.normalized : Vector2.right;
            velocity = direction * speed;
        }

        public void SetSpeed(float newSpeed)
        {
            speed = Mathf.Max(0f, newSpeed);
            velocity = direction * speed;
        }

        public void SetGravityEnabled(bool enabled)
        {
            gravityEnabled = enabled;
            if (gravityEnabled)
            {
                velocity = direction * speed;
            }
        }

        public void SetGravityStrength(float newGravity)
        {
            gravityStrength = Mathf.Max(0f, newGravity);
        }

        public void SetAlignment(bool alignVelocity)
        {
            alignToVelocity = alignVelocity;
        }

        public void SetSpriteAngleOffset(float angleOffset)
        {
            spriteAngleOffset = angleOffset;
        }

        public Vector2 Step(float dt)
        {
            if (gravityEnabled)
            {
                velocity += Vector2.down * gravityStrength * dt;
                return velocity * dt;
            }

            return direction * speed * dt;
        }

        public Quaternion GetRotation()
        {
            if (!alignToVelocity)
            {
                return Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spriteAngleOffset);
            }

            var basis = gravityEnabled ? velocity : direction * Mathf.Max(speed, 0f);
            if (basis.sqrMagnitude <= Mathf.Epsilon)
            {
                basis = direction;
            }

            var angle = Mathf.Atan2(basis.y, basis.x) * Mathf.Rad2Deg + spriteAngleOffset;
            return Quaternion.Euler(0f, 0f, angle);
        }
    }
}
