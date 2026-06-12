using System;
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public sealed class RemoteGrenadeVisual : MonoBehaviour
    {
        [SerializeField] private float gravity = 18f;
        [SerializeField] private float collisionRadius = 0.08f;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private Color tint = new Color(1f, 1f, 1f, 0.95f);
        [SerializeField] private float spriteScale = 0.09f;

        private readonly ProjectileBallistics2D ballistics = new ProjectileBallistics2D();
        private SpriteRenderer spriteRenderer;
        private float lifetime;
        private float age;
        private bool launched;
        private bool exploded;
        private EGrenadeType grenadeType = EGrenadeType.Normal;
        private Action onFinished;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(Vector2 direction, float speed, float gravityStrength, float grenadeLifetime, EGrenadeType type, Action finishedCallback = null)
        {
            grenadeType = type;
            gravity = Mathf.Max(0f, gravityStrength);
            lifetime = Mathf.Max(0.1f, grenadeLifetime);
            age = 0f;
            launched = true;
            exploded = false;
            onFinished = finishedCallback;

            ballistics.Configure(direction, speed, true, gravity, true, 0f);
            ApplyVisual();
            UpdateRotation();
        }

        private void ApplyVisual()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = GrenadeVisualResolver.GetHudSprite(grenadeType)
                ?? Resources.Load<Sprite>("Sprites/Bullet/Circle");

            if (spriteRenderer.sprite == null)
            {
                Destroy(gameObject);
                return;
            }

            spriteRenderer.color = tint;
            transform.localScale = Vector3.one * spriteScale;
        }

        private void Update()
        {
            if (!launched)
            {
                return;
            }

            var dt = Time.deltaTime;
            age += dt;
            if (age >= lifetime)
            {
                Explode(transform.position);
                return;
            }

            var currentPosition = (Vector2)transform.position;
            var step = ballistics.Step(dt);
            if (step.sqrMagnitude <= Mathf.Epsilon)
            {
                UpdateRotation();
                return;
            }

            var hits = Physics2D.CircleCastAll(currentPosition, GetCollisionRadius(), step.normalized, step.magnitude, hitMask);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider == null)
                {
                    continue;
                }

                if (ProjectileHitUtility.IsStageHit(hit.collider.gameObject) ||
                    ProjectileHitUtility.TryGetTargetPlayer(hit.collider, out _))
                {
                    transform.position = hit.point - hit.normal * GetCollisionRadius();
                    Explode(hit.point);
                    return;
                }
            }

            transform.position = currentPosition + step;
            UpdateRotation();
        }

        private float GetCollisionRadius()
        {
            if (collisionRadius > 0f)
            {
                return collisionRadius;
            }

            if (spriteRenderer != null)
            {
                var extents = spriteRenderer.bounds.extents;
                return Mathf.Max(0.02f, Mathf.Max(extents.x, extents.y));
            }

            return 0.08f;
        }

        private void UpdateRotation()
        {
            transform.rotation = ballistics.GetRotation();
        }

        public void ForceExplosion(Vector2 position)
        {
            Explode(position);
        }

        private void Explode(Vector2 position)
        {
            if (exploded)
            {
                return;
            }

            exploded = true;
            SpawnExplosionVisual(position);
            onFinished?.Invoke();
            Destroy(gameObject);
        }

        private void SpawnExplosionVisual(Vector2 position)
        {
            var explosionEffect = GrenadeVisualResolver.GetExplosionEffect(grenadeType);
            if (explosionEffect != null)
            {
                Instantiate(explosionEffect, position, Quaternion.identity);
            }
            else
            {
                var flash = new GameObject("RemoteGrenadeExplosion");
                flash.transform.position = position;

                var renderer = flash.AddComponent<SpriteRenderer>();
                renderer.sprite = Resources.Load<Sprite>("Sprites/Bullet/Circle");
                if (renderer.sprite == null)
                {
                    Destroy(flash);
                    return;
                }

                renderer.color = grenadeType == EGrenadeType.Fire
                    ? new Color(1f, 0.45f, 0.1f, 0.9f)
                    : new Color(1f, 0.78f, 0.2f, 0.9f);
                flash.transform.localScale = Vector3.one * 0.18f;
                Destroy(flash, 0.18f);
            }

            if (SoundManager.Instance != null)
            {
                var sound = grenadeType == EGrenadeType.Fire
                    ? EGrenadeSound.ExplosionFireGrenade
                    : EGrenadeSound.ExplosionGrenade;
                SoundManager.Instance.PlayGrenadeExplosionSound(sound);
            }
        }
    }
}
