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

        [Button("Change Text")]
        public void ChangeText(string str)
        {
            //text?.text = str;

            if (text != null) text.text = str ?? string.Empty;
        }
        [Button("Show Text")]
        public void ShowText()
        {
            if (text != null) text.enabled = true;
        }

        [Button("Hide Text")]
        public void HideText()
        {
            if (text != null) text.enabled = false;
        }

        public void ClearText()
        {
            if (text != null) text.text = string.Empty;
        }

        [Button("Set Team Color")]
        public void SetTeamColor(ETeam team)
        {
            if (text == null)
            {
                return;
            }

            var colors = teamMasterData;
            if (colors == null)
            {
                Debug.LogWarning("[HudPlayerNameText] TeamMasterData is not assigned.");
                return;
            }

            text.color = team switch
            {
                ETeam.Red => colors.RedTeamColor,
                ETeam.Blue => colors.BlueTeamColor,
                _ => colors.NoTeamColor
            };
        }

        [Button("Set Color")]
        public void SetColor()
        {
            if (text != null) text.color = Color.black;
            
        }

        public void DeleteThis()
        {
            Destroy(gameObject);
        }

    }


}
