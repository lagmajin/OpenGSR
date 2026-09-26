using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;


namespace OpenGS
{

    [DisallowMultipleComponent]
    public class ArrowNavigatorEffects : MonoBehaviour
    {
        public bool direction = false;
        public bool color = false;

        [SerializeField, Range(0.1f, 1.0f)] public float sec = 0.1f;
        [SerializeField, Range(10f, 100f)] public float mov = 50f;
        [SerializeField, Range(1, 6)] public int loopCount = 6;

        //public GameObject arrow;

        // public Sprite blueArrow;
        //public Sprite redArrow;


        public Image img;

        private void Awake()
        {
            sec = Mathf.Max(0.01f, float.IsFinite(sec) ? sec : 0.1f);
            mov = float.IsFinite(mov) ? mov : 50f;
            loopCount = Mathf.Clamp(loopCount, 1, 6);
        }

        private void Start()
        {
            if (img == null)
            {
                img = GetComponent<Image>();
            }

            if (img == null)
            {
                Destroy(gameObject);
                return;
            }


            //transform.DOLocalMoveX(new Vector3(5f,0f,0f),1f).SetLoops(3,LoopType.Restart);


            var c = img.color;

            img.color = new Color(c.r, c.g, c.b, 0.4f);

            float temp = 0.0f;

            if (direction)
            {
                temp = mov;
            }
            else
            {
                temp = -mov;
            }

            gameObject.transform.DOLocalMoveX(temp, sec).SetRelative().SetLoops(loopCount, LoopType.Restart)
                .OnComplete(DeleteThis);




        }

        void Reset()
        {
            img = GetComponent<Image>();
        }

        void DeleteThis()
        {
            Destroy(this.gameObject);
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }

    }
}
