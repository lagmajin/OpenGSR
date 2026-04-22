
using UnityEngine;
using KanKikuchi.AudioManager;
using System.Collections;

namespace OpenGS
{


    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class AbstractGrenadeController : MonoBehaviour
    {
        [SerializeField]
        public float damage = 0;
        [SerializeField]
        public float expTime = 3.0f;
        Coroutine c;
        [SerializeField]
        public GameObject expEffect;
        [SerializeField]
        public Rigidbody2D body;

        public MultipleTags myTags;

        [SerializeField] private AudioSource audioSource;

        protected void SetVariables()
        {
            body = gameObject.GetComponent<Rigidbody2D>();

            myTags = gameObject.GetComponent<MultipleTags>();
        }

        private void Start()
        {


            c = StartCoroutine(Functions.WaitAfterAction(Exp, expTime));

        }

        public void Reset()
        {


        }
        void Update()
        {
            var time = Time.deltaTime;


        }

        public virtual void Exp()
        {
            var obj = Instantiate(expEffect, gameObject.transform);

            Destroy(this.gameObject);
        }

        public virtual void OnExplosion()
        {

        }

        public void StopMoving()
        {
            body.linearVelocity = new Vector2();
        }

        public void EnableGravity()
        {
            body.bodyType = RigidbodyType2D.Dynamic;
        }

        public void DisableGravity()
        {
            body.bodyType = RigidbodyType2D.Kinematic;
        }


        IEnumerator BombTimer()
        {
            yield return new WaitForSeconds(expTime);




        }

    }




}
