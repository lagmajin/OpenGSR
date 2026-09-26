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
        private bool transitionRequested;

        private void Awake()
        {
            mainThread = SynchronizationContext.Current ?? new SynchronizationContext();

            if (successPanel != null) successPanel.SetActive(false);
            if (failPanel != null) failPanel.SetActive(false);
        }

        public void ShowMissionResult(int lifeRemaining, int score, bool success)
        {
            lifeRemaining = Mathf.Max(0, lifeRemaining);
            score = Mathf.Max(0, score);

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
            if (transitionRequested)
            {
                return;
            }

            var scene = GeneralSceneMasterData.Instance().MissionLobbyScene();
            if (string.IsNullOrWhiteSpace(scene))
            {
                Debug.LogWarning("[MissionResultUIDirector] Mission lobby scene is not configured.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogError($"[MissionResultUIDirector] Mission lobby scene is not in build settings: {scene}");
                return;
            }

            transitionRequested = true;
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scene);
        }
    }
}
