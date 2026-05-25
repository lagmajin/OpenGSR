using UnityEngine;

namespace OpenGS
{
    public class ArmMainScript : AbstractMatchMainScript
    {
        public override void PostEvent(AbstractGameEvent e)
        {
            if (e == null)
            {
                return;
            }

            Debug.Log($"[ArmMainScript] PostEvent: {e.EventName}");
        }
    }
}
