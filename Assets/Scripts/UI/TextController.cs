using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace OpenGS
{


    [DisallowMultipleComponent]
    public class TextController : MonoBehaviour
    {
        private string t = "";

        [SerializeField] [Required] private TextMeshProUGUI text;


        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void Set(string t)
        {
            this.t = t;
        }

        [Button("AAA")]
        public void SetText(int i=0)
        {

            //text.text=t+i;

            text.SetText(t+i);

        }
    }


}