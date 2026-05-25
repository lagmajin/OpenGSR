using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class PlayerDataLinker : AbstractPlayerLinker
    {
        private AbstractPlayer cachedPlayer;

        public void Start()
        {
            EnsurePlayer();
        }

        public void Update()
        {
            EnsurePlayer();
        }

        private void EnsurePlayer()
        {
            if (cachedPlayer != null)
            {
                return;
            }

            cachedPlayer = GetComponent<AbstractPlayer>();
            SetPlayer(cachedPlayer);
            if (cachedPlayer != null)
            {
                SetPlayerId(cachedPlayer.UniqueID().ToString());
            }
        }
    }
}
