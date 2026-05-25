using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class PlayerDataLinker : AbstractPlayerLinker
    {
        private AbstractPlayer cachedPlayer;

        public bool HasPlayer => cachedPlayer != null;

        public void Start()
        {
            EnsurePlayer();
        }

        protected virtual void OnEnable()
        {
            EnsurePlayer();
        }

        public void Update()
        {
            EnsurePlayer();
        }

        public void RefreshLink()
        {
            cachedPlayer = null;
            EnsurePlayer();
        }

        protected virtual void OnDestroy()
        {
            cachedPlayer = null;
            SetPlayer(null);
            SetPlayerId(string.Empty);
        }

        private void EnsurePlayer()
        {
            if (cachedPlayer != null)
            {
                return;
            }

            cachedPlayer = GetComponent<AbstractPlayer>();
            SetPlayer(cachedPlayer);
            SetPlayerId(cachedPlayer != null ? cachedPlayer.UniqueID().ToString() : string.Empty);
        }
    }
}
