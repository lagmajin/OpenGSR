using UnityEngine;

namespace OpenGS
{
    public class GodModeMainScript : AbstractMatchMainScript, IGodModeMainScript
    {
        public new void Start()
        {
            base.Start();
            Debug.Log("[GodModeMainScript] Start");
        }

        private void Update()
        {
        }

        public override void PostEvent(AbstractGameEvent e)
        {
            Debug.Log($"[GodModeMainScript] PostEvent: {e?.EventName ?? "null"}");
        }
    }
}
