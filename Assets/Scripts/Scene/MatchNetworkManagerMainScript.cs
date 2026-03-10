using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class MatchNetworkManagerScript : MonoBehaviour, INetworkManagerScript
    {
        public void TestFunc()
        {
        }

        public void ParseNetworkMatchMessageFromServer(JObject json)
        {
        }

        public void OnConnected()
        {
        }

        public void OnDisconnected()
        {
        }
    }

    public class MatchNetworkManagerMainScript : MatchNetworkManagerScript
    {
    }
}
