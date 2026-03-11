using UnityEngine;

namespace OpenGS
{
    public class OnlineLobbySceneController : MonoBehaviour
    {
        public void TickInput(
            bool canInput,
            ref int updateCount,
            int maxUpdateCount,
            System.Action onUpdateRooms,
            System.Action onBackToTitle,
            System.Action onOpenShop)
        {
            if (!canInput)
            {
                return;
            }

            if (Input.anyKeyDown)
            {
                updateCount = 0;
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                onUpdateRooms?.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.F6) || Input.GetKey(KeyCode.Escape))
            {
                onBackToTitle?.Invoke();
                return;
            }

            if (Input.GetKey(KeyCode.S))
            {
                onOpenShop?.Invoke();
            }

            if (updateCount >= maxUpdateCount)
            {
                onBackToTitle?.Invoke();
                return;
            }

            updateCount++;
        }
    }
}
