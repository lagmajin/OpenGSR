using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenGS
{
    public class CreateNewAccountScene:AbstractScene
    {
        private void Awake()
        {
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
        }

        private void Start()
        {
            
        }

        private void OnApplicationQuit()
        {
            
        }

        public override SynchronizationContext MainThread()
        {
            throw new NotImplementedException();
        }
    }
}
