using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OpenGS
{
    [System.Serializable]
    public class ClientSessionInfo
    {
        public bool IsOnline { get; set; } = false; // ユーザーがオン ラインかどうか
        public string IpAddress { get; set; }  // クライアントのIPアドレス
        public string Uuid { get; set; }  // ユーザーを一意に識別するUUID


        public ClientSessionInfo()
        {
            Debug.Log("Create ClientSessionData");
        }
    }




}
