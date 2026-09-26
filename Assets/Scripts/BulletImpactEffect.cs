using System;
using System.Collections;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class BulletImpactEffect : MonoBehaviour
    {
        [SerializeField] private float defaultLifetime = 0.2f;

        private Coroutine lifetimeCoroutine;
        private Action<GameObject> releaseAction;

        private void Awake()
        {
            defaultLifetime = float.IsFinite(defaultLifetime) ? Mathf.Max(0f, defaultLifetime) : 0.2f;
        }

        private void OnDisable()
        {
            if (lifetimeCoroutine != null)
            {
                StopCoroutine(lifetimeCoroutine);
                lifetimeCoroutine = null;
            }

            releaseAction = null;
        }

        public void Play(float lifetime, Action<GameObject> onRelease = null)
        {
            var activeLifetime = float.IsFinite(lifetime) && lifetime > 0f ? lifetime : defaultLifetime;
            releaseAction = onRelease;

            if (!isActiveAndEnabled)
            {
                return;
            }

            if (lifetimeCoroutine != null)
            {
                StopCoroutine(lifetimeCoroutine);
            }

            lifetimeCoroutine = StartCoroutine(ReleaseAfterDelay(activeLifetime));
        }

        private IEnumerator ReleaseAfterDelay(float lifetime)
        {
            if (lifetime > 0f)
            {
                yield return new WaitForSeconds(lifetime);
            }

            var action = releaseAction;
            releaseAction = null;
            lifetimeCoroutine = null;

            if (action != null)
            {
                action(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
