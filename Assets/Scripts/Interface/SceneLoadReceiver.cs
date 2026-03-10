using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGS
{
    public class SceneLoadData
    {
        public string message;

        public int errorCode;
        public bool shouldPlayWarningSound;

    }
    public interface ISceneLoadReceiver
    {

    }
}
