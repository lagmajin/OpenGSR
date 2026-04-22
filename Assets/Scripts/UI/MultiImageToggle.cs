
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class MultiImageToggle : MonoBehaviour
    {
        [SerializeField] private Sprite checkedSprite;
        [SerializeField] private Sprite unCheckedSprite;
        [SerializeField] private Toggle toggle;
        [SerializeField] private Image img;

        void Start()
        {
            //toggle = GetComponent<Toggle>();


        }

        void Reset()
        {
            toggle = GetComponent<Toggle>();

        }

        public void OnValueChanged(bool b)
        {
            if (b == true)
            {
                img.sprite = checkedSprite;
            }
            else
            {
                img.sprite = unCheckedSprite;
            }



        }
    }
}
