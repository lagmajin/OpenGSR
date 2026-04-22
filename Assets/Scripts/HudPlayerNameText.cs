using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;



namespace OpenGS
{

    [DisallowMultipleComponent]
    public class HudPlayerNameText : MonoBehaviour
    {
        public TextMeshProUGUI text;

        // Start is called before the first frame update

        private RectTransform myRectTfm;
        private Vector3 offset = new Vector3(0, 1.5f, 0);

        public TeamMasterData teamMasterData;
        void Start()
        {
            myRectTfm = GetComponent<RectTransform>();

        }

        // Update is called once per frame
        void Update()
        {



        }


        [Button("Change Text")]
        public void ChangeText(string str)
        {
            //text?.text = str;

            text.text = str;
        }
        [Button("Show Text")]
        public void ShowText()
        {
            text.enabled = true;
        }

        [Button("Hide Text")]
        public void HideText()
        {
            text.enabled = false;
        }

        public void ClearText()
        {
            text.text = "";
        }

        [Button("Set Team Color")]
        public void SetTeamColor(ETeam team)
        {
            

        }

        [Button("Set Color")]
        public void SetColor()
        {
            text.color=Color.black;
            
        }

        public void DeleteThis()
        {
            Destroy(gameObject);
        }

    }


}