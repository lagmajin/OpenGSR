using UnityEngine;

namespace OpenGS
{
    public interface IMissionClearGate
    {

    }

    [DisallowMultipleComponent]
    public class MissionClearGate : MonoBehaviour, IMissionClearGate
    {
        private void Start()
        {

        }

        private void MissionClear()
        {
            var mainScript = GameObject.Find("MissionMainScript");

        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var tags = collision.gameObject.GetComponent<MultipleTags>();

            if (tags.HasPlayerTag())
            {
                MissionClear();
            }
        }


    }



}
