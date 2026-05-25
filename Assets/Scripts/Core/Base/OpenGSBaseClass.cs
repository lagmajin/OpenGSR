using System;
using UnityEngine;

namespace OpenGS
{
    public class OpenGSBaseClass : MonoBehaviour
    {
        private readonly string guid = System.Guid.NewGuid().ToString("N");

        public string Guid => guid;

        public void OnOriginalEvent(AbstractGameEvent ev)
        {
            Debug.Log($"[OpenGSBaseClass] Event received: {ev?.EventName ?? "null"}");
        }
    }
}
