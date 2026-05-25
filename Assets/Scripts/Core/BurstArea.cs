using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(IMultipleTags))]
    public class BurstArea : MonoBehaviour, IBurstArea
    {
        private MultipleTags tags;

        public bool IsBurstArea => tags != null && tags.HasBurstAreaTag();

        private void Start()
        {
            tags = GetComponent<MultipleTags>();
            EnsureBurstAreaTag();
        }

        void Reset()
        {
            tags = GetComponent<MultipleTags>();
            EnsureBurstAreaTag();
        }

        private void Update()
        {
        }

        private void EnsureBurstAreaTag()
        {
            if (tags != null && !tags.HasBurstAreaTag())
            {
                tags.AddTag("BurstArea");
            }
        }

    }


}
