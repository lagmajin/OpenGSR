
using DG.Tweening;
using OpenGSCore;
using System.Collections;
using UnityEngine;



namespace OpenGS
{




    [DisallowMultipleComponent]
    public class DeathAnimation : MonoBehaviour
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

        private Vector2 startPos;

        [SerializeField] private new Transform transform;

        private void Start()
        {
            startPos = transform.position;


            if (playImmediately)
            {
                StartCoroutine(AnimateFloat());
            }
        }

        void Reset()
        {
            //body = gameObject.GetComponent<Rigidbody2D>();
        }
        IEnumerator AnimateFloat()
        {
            // 上昇フェーズ
            while (transform.position.y < startPos.y + peakHeight)
            {
                transform.position += Vector3.up * riseSpeed * Time.deltaTime;
                yield return null;
            }

            // 落下フェーズ
            while (true)
            {
                transform.position += Vector3.down * fallSpeed * Time.deltaTime;
                yield return null;
            }
        }
    }
}
