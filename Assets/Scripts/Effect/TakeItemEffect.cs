using DG.Tweening;
using UnityEngine;


namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class TakeItemEffect:MonoBehaviour
    {
        public bool playsound =true;
        public AudioClip sound;
        public Sprite effect;
        public float delay = 0.0f;
        public float soundDelay = 0.5f;
        public float time=0.3f;

        public float afterScale = 1.5f;
        public float opacity = 0.5f;

        private void Start()
        {
            
            var spritreRender = gameObject.GetComponent<SpriteRenderer>();

            spritreRender.color = new Color(1, 1, 1, opacity);
            
            
            
            var seq = DOTween.Sequence();
            seq.SetRelative();
            seq.SetDelay(delay);
            seq.SetLink(gameObject);
            seq.Append(gameObject.transform.DOScale(new Vector3(afterScale, afterScale, afterScale), time));
            //seq.Append(spritreRender.DOFade(0, 0.6));
            seq.OnComplete(DeleteThis);
            seq.Play();

            //PlaySound.PlaySE(sound,soundDelay);
        }

        private void Update()
        {

        }

        private void DeleteThis()
        {
            Destroy(this.gameObject);
        }
    }
}
