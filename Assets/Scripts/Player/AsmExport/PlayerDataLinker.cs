using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class PlayerDataLinker : AbstractPlayerLinker
    {
        private AbstractPlayer cachedPlayer;
        private float nextResolveTime;
        private const float ResolveRetryInterval = 0.25f;

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
            nextResolveTime = 0f;
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

            var now = Time.unscaledTime;
            if (!float.IsFinite(now) || now < 0f)
            {
                return;
            }

            if (now < nextResolveTime)
            {
                return;
            }

            nextResolveTime = now + ResolveRetryInterval;

            cachedPlayer = GetComponent<AbstractPlayer>();
            SetPlayer(cachedPlayer);
            SetPlayerId(cachedPlayer != null ? cachedPlayer.UniqueID().ToString() : string.Empty);
        }
    }
}
