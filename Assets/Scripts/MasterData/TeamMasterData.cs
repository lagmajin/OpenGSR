using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace OpenGS
{
    [CreateAssetMenu(menuName = "Team/Team")]
    public class TeamMasterData : ScriptableObject
    {
        public Color32 NoTeamColor = Color.white;
        public Color32 RedTeamColor = Color.red;
        public Color32 BlueTeamColor = Color.blue;


        #if UNITY_EDITOR
        [Button("")]
        public void AutoSet()
        {
            var asset = AssetDatabase.FindAssets("");
        }

        #endif
    }
}
