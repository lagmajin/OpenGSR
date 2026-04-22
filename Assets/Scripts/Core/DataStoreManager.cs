using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;


namespace OpenGS
{


    public class DataStoreManager
    {

        public static DataStoreManager Instance { get; } = new DataStoreManager();

        private DataStoreManager()
        {


        }



        public void LoadGlobalServerInfo()
        {
            
            
            
            var json = new JObject();

            



        }


        /*
        public ServerInfo LocalServerInfo()
        {
            var result = new ServerInfo(50000,"127.0.01");

            return result;
        }


        */





    }

}