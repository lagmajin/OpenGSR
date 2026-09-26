using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace OpenGS
{


    /*
    public struct PlayerNameIdentifier
    {
        GameObject target;
        public string nameText;
    }

    */

    [DisallowMultipleComponent]
    public class PlayerNameCanvas : MonoBehaviour
    {
        [SerializeField] private List<Transform> targets = new();
        [SerializeField] private bool autoRefresh = true;
        [SerializeField] private float cleanupInterval = 0.5f;
        private float nextCleanupTime;

        private void OnValidate()
        {
            if (!float.IsFinite(cleanupInterval)) cleanupInterval = 0.5f;
            cleanupInterval = Mathf.Max(0.05f, cleanupInterval);
        }

        public int TargetCount => targets.Count;

        void Start()
        {
            RefreshTargets();
        }

        void Update()
        {
            if (!autoRefresh)
            {
                return;
            }

            var now = Time.unscaledTime;
            if (!float.IsFinite(now) || now < 0f || now < nextCleanupTime)
            {
                return;
            }

            nextCleanupTime = now + cleanupInterval;
            targets.RemoveAll(target => target == null);
        }


        public void AddTarget()
        {
            RefreshTargets();
        }

        public void RefreshTargets()
        {
            targets.Clear();
            foreach (var player in FindObjectsByType<AbstractPlayer>(FindObjectsSortMode.None))
            {
                if (player != null)
                {
                    targets.Add(player.transform);
                }
            }

        }
    }


}
