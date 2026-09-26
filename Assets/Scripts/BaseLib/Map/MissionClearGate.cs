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

        public void MissionClear()
        {
            if (cleared) return;
            cleared = true;

            var mainScript = GameObject.Find("MissionMainScript");
            if (mainScript != null)
            {
                mainScript.SendMessage("MissionClear", SendMessageOptions.DontRequireReceiver);
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
