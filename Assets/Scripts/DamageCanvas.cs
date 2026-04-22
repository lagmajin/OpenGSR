using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;


namespace OpenGS
{

    [DisallowMultipleComponent]
    public class DamageCanvas : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private GameObject comboTextPrefab;
        [SerializeField] private GameObject damageTextPrefab;

        [SerializeField] private GameObject misty;



        // Start is called before the first frame update
        void Start()
        {

        }

        void Reset()
        {
            canvas = GetComponent<Canvas>();
        }

        // Update is called once per frame
        void Update()
        {

        }

        void Set()
        {

        }

        public void AutoSet()
        {

        }

        public void ShowDamage()
        {

        }
        public void ShowDamageText(Transform transform, float time = 1.0f)
        {
            var obj2 = Instantiate(comboTextPrefab, gameObject.transform, false);


        }

        public void ShowComboText(Transform transform, float time = 1.0f)
        {

        }

        [Button("Show Damage Text")]
        public void ShowDamageText()
        {
            var image = Instantiate(comboTextPrefab, gameObject.transform, true);

        }

        [Button("Show Combo Text")]
        public void ShowComboText()
        {
            var image = Instantiate(comboTextPrefab, gameObject.transform, true);



            //var offset = new Vector3(0, -0.0f, 0);

            var rectTransform = image.GetComponent<RectTransform>();

            image.transform.position = misty.transform.position;


            //rectTransform.position = RectTransformUtility.WorldToScreenPoint(Camera.main, misty.transform.position + offset);

        }

    }
}
