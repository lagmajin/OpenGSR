using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class TutorialCheckpointTrigger : MonoBehaviour
    {
        [SerializeField] private TutorialMainScript tutorial;
        [SerializeField] private string checkpointId = "move";
        [SerializeField] private bool triggerOnce = true;

        private bool consumed;

        private void Reset()
        {
            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed && triggerOnce)
            {
                return;
            }

            if (other == null)
            {
                return;
            }

            var player = other.GetComponentInParent<AbstractPlayer>();
            if (player == null || player.PlayerType() != EPlayerType.MyPlayer)
            {
                return;
            }

            tutorial ??= GetComponentInParent<TutorialMainScript>();
            if (tutorial == null)
            {
                return;
            }

            tutorial.CompleteCheckpoint(checkpointId);

            if (triggerOnce)
            {
                consumed = true;
            }
        }
    }
}
