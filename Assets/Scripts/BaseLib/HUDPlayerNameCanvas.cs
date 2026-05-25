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

            Debug.Log($"[PlayerNameCanvas] TargetCount={targets.Count}");
        }
    }


}
