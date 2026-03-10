using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using OpenGSCore;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class MatchEventProvider:MonoBehaviour
    {
        [SerializeField][Required] private MatchNetworkManagerScript networkManager;
        [SerializeField][Required] private AbstractMatchMainScript mainScript;

        

        private void Start()
        {
            if (networkManager == null) { };
            if(mainScript==null) { };


        }

        public void UseGrenade()
        {

        }

        public void UseInstantItem(EInstantItemType type)
        {
            //networkManager.SendMessage()

        }

    }
}
