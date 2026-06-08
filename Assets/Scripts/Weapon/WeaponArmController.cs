using UnityEngine;
using DG.Tweening;

namespace OpenGS
{
    /// <summary>
    /// Controls the weapon arm animations and positioning.
    /// Handles sit and stand up animations for the weapon arm.
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponArmController : MonoBehaviour
    {
        [SerializeField] private Vector3 sitLocalOffset = new Vector3(-0.02f, -0.01f, 0f);
        [SerializeField] private float sitTransitionTime = 0.2f;
        private Vector3 originalLocalPosition;
        private bool hasCachedPosition;

        private void Awake()
        {
            originalLocalPosition = transform.localPosition;
            hasCachedPosition = true;
        }

        /// <summary>
        /// Called when the player sits down - play sit animation for weapon arm
        /// </summary>
        public void Sit()
        {
            if (!hasCachedPosition)
            {
                originalLocalPosition = transform.localPosition;
                hasCachedPosition = true;
            }

            transform.DOKill();
            transform.DOLocalMove(originalLocalPosition + sitLocalOffset, sitTransitionTime).SetEase(Ease.OutSine);
        }

        /// <summary>
        /// Called when the player stands up - play stand up animation for weapon arm
        /// </summary>
        public void StandUp()
        {
            if (!hasCachedPosition)
            {
                originalLocalPosition = transform.localPosition;
                hasCachedPosition = true;
            }

            transform.DOKill();
            transform.DOLocalMove(originalLocalPosition, sitTransitionTime).SetEase(Ease.OutSine);
        }
    }
}
