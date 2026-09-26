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
        private static Sprite fallbackSprite;

        private void OnValidate()
        {
            if (!float.IsFinite(gravity)) gravity = 18f;
            if (!float.IsFinite(collisionRadius)) collisionRadius = 0.08f;
            if (!float.IsFinite(spriteScale)) spriteScale = 0.09f;
            gravity = Mathf.Max(0f, gravity);
            collisionRadius = Mathf.Max(0.02f, collisionRadius);
            spriteScale = Mathf.Max(0.01f, spriteScale);
        }

        private void Awake()
        {
            gravity = float.IsFinite(gravity) ? Mathf.Max(0f, gravity) : 18f;
            collisionRadius = float.IsFinite(collisionRadius) ? Mathf.Max(0.02f, collisionRadius) : 0.08f;
            spriteScale = float.IsFinite(spriteScale) ? Mathf.Max(0.01f, spriteScale) : 0.09f;
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(Vector2 direction, float speed, float gravityStrength, float grenadeLifetime, EGrenadeType type, Action finishedCallback = null)
        {
            grenadeType = type;
            gravity = float.IsFinite(gravityStrength) ? Mathf.Max(0f, gravityStrength) : 18f;
            lifetime = float.IsFinite(grenadeLifetime) ? Mathf.Max(0.1f, grenadeLifetime) : 10f;
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
                ?? ResolveFallbackSprite();

            if (spriteRenderer.sprite == null)
            {
                Destroy(gameObject);
                return;
            }

            spriteRenderer.color = tint;
            // Grenades are world projectiles. Keep them in front of stage/player
            // sprites just like the regular Bullet prefab.
            spriteRenderer.sortingOrder = 10;
            transform.localScale = Vector3.one * spriteScale;
        }

        private void Update()
        {
            if (!launched)
            {
                return;
            }

            var dt = Time.deltaTime;
            if (!float.IsFinite(dt) || dt <= 0f || !IsFinite(transform.position))
            {
                Destroy(gameObject);
                return;
            }
            // Bound one simulation step so a frame hitch does not tunnel the visual grenade.
            dt = Mathf.Min(dt, 0.1f);

            age += dt;
            if (!float.IsFinite(age) || age >= lifetime)
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
            if (!IsFinite(transform.position))
            {
                Destroy(gameObject);
                return;
            }
            UpdateRotation();
        }

        private static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
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
            Explode(IsFinite(position) ? position : (Vector2)transform.position);
        }

        private void Explode(Vector2 position)
        {
            if (exploded)
            {
                return;
            }

            exploded = true;
            SpawnExplosionVisual(position);
            NotifyFinished();
            Destroy(gameObject);
        }

        private void NotifyFinished()
        {
            if (onFinished == null)
            {
                return;
            }

            foreach (Action handler in onFinished.GetInvocationList())
            {
                try
                {
                    handler();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[RemoteGrenadeVisual] finish callback failed: {ex}");
                }
            }
        }

        private void SpawnExplosionVisual(Vector2 position)
        {
            var explosionEffect = GrenadeVisualResolver.GetExplosionEffect(grenadeType);
            if (explosionEffect != null)
            {
                var spawnedEffect = Instantiate(explosionEffect, position, Quaternion.identity);
                // Some effect prefabs self-delete, but replayed network effects
                // must still have a hard upper bound when a prefab is misconfigured.
                Destroy(spawnedEffect, 5f);
            }
            else
            {
                var flash = new GameObject("RemoteGrenadeExplosion");
                flash.transform.position = position;

                var renderer = flash.AddComponent<SpriteRenderer>();
                renderer.sprite = ResolveFallbackSprite();
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

        private static Sprite ResolveFallbackSprite()
        {
            var sprite = Resources.Load<Sprite>("Sprites/Bullet/Circle");
            if (sprite != null)
            {
                return sprite;
            }

            if (fallbackSprite == null && Texture2D.whiteTexture != null)
            {
                fallbackSprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f);
                fallbackSprite.name = "RemoteGrenadeFallbackSprite";
            }

            return fallbackSprite;
        }

        private static bool IsFinite(Vector2 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y);
        }
    }
}
