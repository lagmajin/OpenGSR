using System.Collections.Generic;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// The size of the box an AI watches for targets.
    /// </summary>
    public enum eAISearchBoxSize
    {
        Small,
        Medium,
        Large
    }

    public class AISearchCollision : MonoBehaviour
    {
        [Header("Owner")]
        [Tooltip("The AI that owns this search box. Falls back to a parent AI.")]
        public GameObject player;

        [Header("Search Box")]
        [SerializeField] private eAISearchBoxSize searchBoxSize = eAISearchBoxSize.Medium;

        // Half extent of the box per size. A search box is square, so the two
        // extents are the same number.
        private static readonly Dictionary<eAISearchBoxSize, float> HalfExtents =
            new Dictionary<eAISearchBoxSize, float>
            {
                [eAISearchBoxSize.Small] = 2f,
                [eAISearchBoxSize.Medium] = 4f,
                [eAISearchBoxSize.Large] = 7f,
            };

        private Collider2D searchBox;

        private void Awake()
        {
            searchBox = GetComponent<Collider2D>();
            ApplySearchBoxSize();
        }

        private void ApplySearchBoxSize()
        {
            if (searchBox == null)
            {
                return;
            }

            if (!HalfExtents.TryGetValue(searchBoxSize, out var half))
            {
                half = HalfExtents[eAISearchBoxSize.Medium];
            }

            if (searchBox is BoxCollider2D box)
            {
                // A box whose size was authored in the prefab is overwritten here,
                // so the size field is the one thing that decides how far the AI
                // can see rather than a number nobody can find.
                box.size = new Vector2(half * 2f, half * 2f);
                box.offset = Vector2.zero;
            }
            else if (searchBox is CircleCollider2D circle)
            {
                circle.radius = half;
                circle.offset = Vector2.zero;
            }

            // Without this the box would not fire a single callback, which is why
            // a search box that appeared to do nothing reported no targets.
            searchBox.isTrigger = true;
        }

        private AIPlayerController ResolveAI()
        {
            if (player == null)
            {
                return null;
            }

            var ai = player.GetComponent<AIPlayerController>();
            if (ai == null)
            {
                ai = player.GetComponentInParent<AIPlayerController>();
            }

            return ai;
        }

        private void SendNewTarget(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            var ai = ResolveAI();
            if (ai == null)
            {
                return;
            }

            // Only a player can be a target, and never the AI looking at itself.
            var targetPlayer = target.GetComponentInParent<IBattlePlayer>();
            if (targetPlayer == null || targetPlayer.Root == ai.gameObject)
            {
                return;
            }

            ai.AddToTargetList(targetPlayer.Root);
        }

        private void SendOutTarget(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            // Dropping the target is what stops an AI chasing a player who has
            // left its search box: the list kept the stale entry and the AI kept
            // firing at an empty room.
            var targetPlayer = target.GetComponentInParent<IBattlePlayer>();
            if (targetPlayer == null)
            {
                return;
            }

            ResolveAI()?.RemoveToTargetList(targetPlayer.Root);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            SendNewTarget(collision.gameObject);
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            SendOutTarget(collision.gameObject);
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
        }
    }
}
