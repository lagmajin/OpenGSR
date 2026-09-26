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

        private void Awake()
        {
            delay = float.IsFinite(delay) ? Mathf.Max(0f, delay) : 0f;
            soundDelay = float.IsFinite(soundDelay) ? Mathf.Max(0f, soundDelay) : 0.5f;
            time = float.IsFinite(time) ? Mathf.Max(0.01f, time) : 0.3f;
            afterScale = float.IsFinite(afterScale) ? Mathf.Max(0f, afterScale) : 1.5f;
            opacity = float.IsFinite(opacity) ? Mathf.Clamp01(opacity) : 0.5f;
        }

        private void OnValidate()
        {
            if (!float.IsFinite(delay)) delay = 0f;
            if (!float.IsFinite(soundDelay)) soundDelay = 0.5f;
            if (!float.IsFinite(time)) time = 0.3f;
            if (!float.IsFinite(afterScale)) afterScale = 1.5f;
            if (!float.IsFinite(opacity)) opacity = 0.5f;
            delay = Mathf.Max(0f, delay);
            soundDelay = Mathf.Max(0f, soundDelay);
            time = Mathf.Max(0.01f, time);
            afterScale = Mathf.Max(0f, afterScale);
            opacity = Mathf.Clamp01(opacity);
        }

        private void Start()
        {
            
            var spritreRender = gameObject.GetComponent<SpriteRenderer>();

            if (spritreRender != null)
            {
                var color = spritreRender.color;
                color.a = Mathf.Clamp01(opacity);
                spritreRender.color = color;
            }
            
            
            
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

        private void DeleteThis()
        {
            Destroy(this.gameObject);
        }
    }
}
