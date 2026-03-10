using System;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;



#pragma warning disable 0414

namespace OpenGS
{


    public class RegisterAccount : MonoBehaviour
    {
        private TcpClient client = null;


        void Start()
        {


        }

        void Update()
        {

        }

        void registry(string name, string accountName, string pass)
        {
            var json = new JObject();

            json["Name"] = name;
            json["AccountName"] = accountName;
            json["Password"] = pass;


        }

    }


}
