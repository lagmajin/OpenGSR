using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{


    [DisallowMultipleComponent]
    public class BlinkImage : MonoBehaviour
    {

        [SerializeField] private Image img;
        private float time = 0;
        [Range(0.1f, 10.0f)] public float speed;
        // Start is called before the first frame update
        void Start()
        {
            if (img == null)
            {
                img = GetComponent<Image>();
            }
        }

        void Reset()
        {
            img = GetComponent<Image>();
        }

        // Update is called once per frame
        void Update()
        {
            if (img == null)
            {
                return;
            }

            Color color = img.color;
            time += Time.deltaTime * speed;
            // Mathf.Sin()��-1�`1��Ԃ�
            // color��0�`1�Ŏw�肷��
            color.a = Mathf.Sin(time) * 0.5f + 0.5f;
            img.color = color;

        }

        public void ResetBlink()
        {
            time = 0f;
            if (img != null)
            {
                var color = img.color;
                color.a = 1f;
                img.color = color;
            }
        }
    }

}
