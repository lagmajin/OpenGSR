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


        public void Set(string t)
        {
            this.t = t;
        }

        [Button("AAA")]
        public void SetText(int i=0)
        {

            //text.text=t+i;

            if (text != null)
            {
                text.SetText(t + i);
            }

        }
    }


}
