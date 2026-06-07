using System.Threading;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class MissionResultUIDirector : MonoBehaviour
    {
        [SerializeField] private GameObject successPanel;
        [SerializeField] private GameObject failPanel;
        [SerializeField] private TMPro.TextMeshProUGUI lifeText;
        [SerializeField] private TMPro.TextMeshProUGUI scoreText;

        private SynchronizationContext mainThread;

        private void Awake()
        {
            mainThread = SynchronizationContext.Current ?? new SynchronizationContext();

            if (successPanel != null) successPanel.SetActive(false);
            if (failPanel != null) failPanel.SetActive(false);
        }

        public void ShowMissionResult(int lifeRemaining, int score, bool success)
        {
            if (lifeText != null)
            {
                lifeText.text = $"Life: {lifeRemaining}";
            }

            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
            }

            if (successPanel != null)
            {
                successPanel.SetActive(success);
            }

            if (failPanel != null)
            {
                failPanel.SetActive(!success);
            }
        }

        public void BackToMissionLobby()
        {
            var scene = GeneralSceneMasterData.Instance().MissionLobbyScene();
            if (!string.IsNullOrWhiteSpace(scene))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
            }
        }
    }
}