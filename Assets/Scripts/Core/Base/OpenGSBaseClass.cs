using System;

using UnityEngine;

namespace OpenGS
{
    public class OpenGSBaseClass:MonoBehaviour
    {
        string guid = Guid.NewGuid().ToString("N");






        public void OnOriginalEvent(AbstractGameEvent ev)
        {
            
        }
    }



}
