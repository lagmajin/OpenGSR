
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
                transform.position += Vector3.up * riseSpeed * Time.deltaTime;
                yield return null;
            }

            isFalling = true;
            // 落下フェーズ
            while (isFalling && transform.position.y > startPos.y)
            {
                transform.position += Vector3.down * fallSpeed * Time.deltaTime;
                yield return null;
            }

            transform.position = new Vector3(transform.position.x, startPos.y, transform.position.z);
            isPlaying = false;
        }
    }
}
