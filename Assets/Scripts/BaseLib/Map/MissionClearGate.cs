using UnityEngine;

namespace OpenGS
{
    public interface IMissionClearGate
    {

    }

    [DisallowMultipleComponent]
    public class MissionClearGate : MonoBehaviour, IMissionClearGate
    {
        private bool cleared;

        private void Start()
        {
            // Quest scenes created before the gate prefab existed may only contain
            // the MissionClearGate component. Make those gates collidable too.
            if (GetComponent<Collider2D>() == null)
            {
                var gateCollider = gameObject.AddComponent<BoxCollider2D>();
                gateCollider.size = Vector2.one;
            }
        }

        /// <summary>
        /// Clears the mission this gate belongs to.
        /// <para>
        /// It used to find the mission script by GameObject name and SendMessage a
        /// method onto it. Neither half held: the mission scenes name that object
        /// "MainScript", and the one class that does have a MissionClear method is
        /// in no scene at all, so reaching a gate did nothing and failed quietly.
        /// The script is found by type now, and a scene with neither is reported
        /// rather than passed over.
        /// </para>
        /// </summary>
        public void MissionClear()
        {
            if (cleared) return;
            cleared = true;

            var mainScript = FindAnyObjectByType<MissionMainScript>();
            if (mainScript != null)
            {
                mainScript.MissionClear();
            }
            else
            {
                Debug.LogWarning("[MissionClearGate] MissionMainScript was not found.");
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision == null || collision.collider == null) return;

            var tags = collision.collider.GetComponentInParent<MultipleTags>();
            if (tags == null) return;

            if (tags.HasPlayerTag())
            {
                MissionClear();
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision == null) return;

            var tags = collision.GetComponentInParent<MultipleTags>();
            if (tags != null && tags.HasPlayerTag())
            {
                MissionClear();
            }
        }


    }


}
