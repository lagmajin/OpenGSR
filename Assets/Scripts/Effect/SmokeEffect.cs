using System.Collections.Generic;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class SmokeEffect : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private SpriteRenderer sourceRenderer;

        [Header("Smoke Stack")]
        [SerializeField, Min(1)] private int count = 8;
        [SerializeField] private float riseStep = 0.12f;
        [SerializeField] private float sidewaysJitter = 0.05f;
        [SerializeField] private float scaleStep = 0.08f;
        [SerializeField, Range(0f, 1f)] private float alphaStep = 0.12f;
        [SerializeField] private float lifetime = 1.8f;

        [Header("Motion")]
        [SerializeField] private float driftSpeed = 0.15f;
        [SerializeField] private float driftJitter = 0.03f;
        [SerializeField] private float spinSpeed = 8f;

        private readonly List<Transform> smokePuffs = new List<Transform>();
        private readonly List<SpriteRenderer> puffRenderers = new List<SpriteRenderer>();
        private readonly List<Vector3> puffVelocities = new List<Vector3>();
        private readonly List<float> puffBaseAlphas = new List<float>();
        private float age;

        private void Awake()
        {
            count = Mathf.Max(1, count);
            riseStep = SanitizeNonNegative(riseStep, 0.12f);
            sidewaysJitter = SanitizeNonNegative(sidewaysJitter, 0.05f);
            scaleStep = SanitizeNonNegative(scaleStep, 0.08f);
            alphaStep = Mathf.Clamp01(float.IsFinite(alphaStep) ? alphaStep : 0.12f);
            lifetime = SanitizePositive(lifetime, 1.8f);
            driftSpeed = SanitizeFinite(driftSpeed, 0.15f);
            driftJitter = SanitizeNonNegative(driftJitter, 0.03f);
            spinSpeed = SanitizeFinite(spinSpeed, 8f);
        }

        private void Start()
        {
            if (sourceRenderer == null)
            {
                sourceRenderer = GetComponent<SpriteRenderer>();
            }

            if (sourceRenderer == null || sourceRenderer.sprite == null)
            {
                Destroy(gameObject);
                return;
            }

            BuildSmokeStack();
            Destroy(gameObject, Mathf.Max(0.1f, lifetime));
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            {
                return;
            }
            deltaTime = Mathf.Min(deltaTime, 0.1f);

            age = Mathf.Min(lifetime, age + deltaTime);
            var fade = 1f - Mathf.Clamp01(age / lifetime);

            var puffCount = Mathf.Min(
                smokePuffs.Count,
                Mathf.Min(puffVelocities.Count, Mathf.Min(puffRenderers.Count, puffBaseAlphas.Count)));
            for (var i = 0; i < puffCount; i++)
            {
                var puff = smokePuffs[i];
                if (puff == null)
                {
                    continue;
                }

                var velocity = puffVelocities[i];
                puff.localPosition += velocity * deltaTime;
                puff.Rotate(Vector3.forward, spinSpeed * deltaTime, Space.Self);

                var sr = puffRenderers[i];
                if (sr != null)
                {
                    var color = sr.color;
                    color.a = Mathf.Clamp01(puffBaseAlphas[i] * fade);
                    sr.color = color;
                }
            }
        }

        private static float SanitizeFinite(float value, float fallback)
        {
            return float.IsFinite(value) ? value : fallback;
        }

        private static float SanitizeNonNegative(float value, float fallback)
        {
            return Mathf.Max(0f, SanitizeFinite(value, fallback));
        }

        private static float SanitizePositive(float value, float fallback)
        {
            return Mathf.Max(0.1f, SanitizeFinite(value, fallback));
        }

        private void BuildSmokeStack()
        {
            smokePuffs.Clear();
            puffRenderers.Clear();
            puffVelocities.Clear();
            puffBaseAlphas.Clear();

            var baseSprite = sourceRenderer.sprite;
            var baseColor = sourceRenderer.color;
            var baseOrder = sourceRenderer.sortingOrder;

            sourceRenderer.enabled = false;

            for (var i = 0; i < count; i++)
            {
                var puff = new GameObject($"SmokePuff_{i}").transform;
                puff.SetParent(transform, false);

                var upOffset = riseStep * i;
                var xOffset = Random.Range(-sidewaysJitter, sidewaysJitter) * (i + 1);
                puff.localPosition = new Vector3(xOffset, upOffset, 0f);
                puff.localScale = Vector3.one * (1f + scaleStep * i);

                var sr = puff.gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = baseSprite;
                sr.sortingLayerID = sourceRenderer.sortingLayerID;
                sr.sortingOrder = baseOrder + i;

                var color = baseColor;
                color.a = Mathf.Clamp01(baseColor.a - alphaStep * i);
                sr.color = color;

                smokePuffs.Add(puff);
                puffRenderers.Add(sr);
                puffVelocities.Add(new Vector3(
                    Random.Range(-driftJitter, driftJitter),
                    driftSpeed + Random.Range(0f, driftJitter),
                    0f));
                puffBaseAlphas.Add(color.a);
            }
        }
    }
}
