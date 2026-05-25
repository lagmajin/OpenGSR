using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(IMultipleTags))]
    public class BurstArea : MonoBehaviour, IBurstArea
    {
        private MultipleTags tags;
        private void Start()
        {
            tags = GetComponent<MultipleTags>();
            if (tags != null && !tags.HasBurstAreaTag())
            {
                tags.AddTag("BurstArea");
            }
        }

        void Reset()
        {
            tags = GetComponent<MultipleTags>();
        }

        private void Update()
        {
        }

    }


}
