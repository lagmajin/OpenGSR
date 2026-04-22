
using UnityEngine;
using DG.Tweening;


namespace OpenGS
{
    [DisallowMultipleComponent]
    public class BoosterSparkEffect:MonoBehaviour{

        public float time = 3.0f;
        public float moveY = 3.0f;
        public float randRange1 = 0.05f;
        public float randRange2 = 0.05f;

        private void Start()
        {
            var rand1 = Random.Range(randRange1, 0.30f);
            var rand2 = Random.Range(randRange2, 0.30f);

            var myTransform = this.transform;
            Vector3 pos = myTransform.position;
            pos.x += rand1;
            pos.y += rand2;

            myTransform.position = pos;

            var render = gameObject.GetComponent<SpriteRenderer>();

            var seq = DOTween.Sequence();
            seq.SetLink(gameObject);
            seq.Append(transform.DOLocalMoveY(moveY, time).SetRelative(true));
            seq.Join(transform.DOLocalRotate(new Vector3(0, 0, 360f), 6f, RotateMode.FastBeyond360));
            seq.Join(render.DOFade(0, time));
            

            seq.OnComplete(() => Destroy(gameObject));

            //transform.DOLocalMoveY(moveY, time).SetRelative().OnComplete(() => { Destroy(gameObject); }); ;

            //Destroy(gameObject, 2.0f);
        }

        private void Update()
        {

        }
    }



}
