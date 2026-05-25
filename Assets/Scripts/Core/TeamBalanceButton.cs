
using UnityEngine;

namespace OpenGS
{
    interface ITeamBalanceButton{

    }

    public class TeamBalanceButton : MonoBehaviour
    {
        [SerializeField] private GameObject targetObject;
        [SerializeField] private bool startEnabled = true;

        // Start is called before the first frame update
        void Start()
        {
            if (targetObject == null)
            {
                targetObject = gameObject;
            }

            if (startEnabled)
            {
                TurnOn();
            }
            else
            {
                TurnOff();
            }
        }

        public void TurnOn()
        {
            if (targetObject != null)
            {
                targetObject.SetActive(true);
            }
        }

        public void TurnOff()
        {
            if (targetObject != null)
            {
                targetObject.SetActive(false);
            }
        }
    }


}
