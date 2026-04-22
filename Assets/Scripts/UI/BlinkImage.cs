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
        [SerializeField]
        [Range(0.1f, 10.0f)] float duration = 1.0f;
        [Range(0.1f, 10.0f)] public float speed;
        // Start is called before the first frame update
        void Start()
        {

        }

        void Reset()
        {
            img = GetComponent<Image>();
        }

        // Update is called once per frame
        void Update()
        {
            Color color = img.color;
            time += Time.deltaTime * speed;
            // Mathf.Sin()ÇÕ-1Å`1Çï‘Ç∑
            // colorÇÕ0Å`1Ç≈éwíËÇ∑ÇÈ
            color.a = Mathf.Sin(time) * 0.5f + 0.5f;
            img.color = color;

        }
    }

}