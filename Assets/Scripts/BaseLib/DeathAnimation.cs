
using DG.Tweening;
using OpenGSCore;
using System.Collections;
using UnityEngine;



namespace OpenGS
{




    [DisallowMultipleComponent]
    public class DeathAnimation : MonoBehaviour, IDeathAnimation
    {
        public bool playImmediately = false;
        public Rigidbody2D body;
        [SerializeField, Range(1f, 100f)]
        public float force = 100.0f;
        public Animator animator;
        [SerializeField, Range(1f, 20f)]
        public float activeTime = 5.0f;
        //public float eDirection d=eDirection.;

        public float riseSpeed = 3f;
        public float fallSpeed = 1f;
        public float peakHeight = 2f;

        private bool isFalling = false;
        private bool isPlaying = false;
        private Vector2 startPos;

        [SerializeField] private new Transform transform;

        private void Awake()
        {
            if (transform == null)
            {
                transform = base.transform;
            }

            riseSpeed = Mathf.Max(0.01f, float.IsFinite(riseSpeed) ? riseSpeed : 3f);
            fallSpeed = Mathf.Max(0.01f, float.IsFinite(fallSpeed) ? fallSpeed : 1f);
            peakHeight = Mathf.Max(0f, float.IsFinite(peakHeight) ? peakHeight : 2f);
        }

        private void Start()
        {
            startPos = transform.position;
            if (playImmediately)
            {
                Play();
            }
        }

        void Reset()
        {
            //body = gameObject.GetComponent<Rigidbody2D>();
        }
        public void Play()
        {
            if (isPlaying)
            {
                return;
            }

            startPos = transform.position;
            StopAllCoroutines();
            StartCoroutine(AnimateFloat());
        }

        private IEnumerator AnimateFloat()
        {
            isPlaying = true;
            isFalling = false;
            // 上昇フェーズ
            while (!isFalling && transform.position.y < startPos.y + peakHeight)
            {
                var deltaTime = Time.deltaTime;
                if (!float.IsFinite(deltaTime) || deltaTime < 0f)
                {
                    yield return null;
                    continue;
                }
                deltaTime = Mathf.Min(deltaTime, 0.1f);

                transform.position += Vector3.up * riseSpeed * deltaTime;
                yield return null;
            }

            isFalling = true;
            // 落下フェーズ
            while (isFalling && transform.position.y > startPos.y)
            {
                var deltaTime = Time.deltaTime;
                if (!float.IsFinite(deltaTime) || deltaTime < 0f)
                {
                    yield return null;
                    continue;
                }
                deltaTime = Mathf.Min(deltaTime, 0.1f);

                transform.position += Vector3.down * fallSpeed * deltaTime;
                yield return null;
            }

            transform.position = new Vector3(transform.position.x, startPos.y, transform.position.z);
            isPlaying = false;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            isPlaying = false;
            isFalling = false;
        }
    }
}
