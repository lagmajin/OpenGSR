using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace OpenGS
{
    public enum TutorialStepKind
    {
        Message,
        Checkpoint,
        DummyDefeated,
        Delay,
        Final
    }

    [Serializable]
    public class TutorialStep
    {
        [Tooltip("Optional stable id for debugging and trigger wiring.")]
        public string id;

        [Tooltip("Optional short title shown in the tutorial HUD.")]
        public string title;

        [TextArea(2, 6)]
        public string instruction;

        public TutorialStepKind kind = TutorialStepKind.Message;

        [Tooltip("Used when kind = Checkpoint.")]
        public string checkpointId;

        [Tooltip("Used when kind = DummyDefeated.")]
        public SandboxDummyEnemy targetDummy;

        [Tooltip("Used when kind = Delay.")]
        public float delaySeconds = 1.5f;
    }

    [DisallowMultipleComponent]
    public class TutorialMainScript : AbstractMatchMainScript
    {
        [Header("Spawn")]
        [SerializeField] private MissionReSpawnPoints respawnPoints;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI instructionText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private GameObject completionPanel;

        [Header("Flow")]
        [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();
        [SerializeField] private SandboxDummyEnemy defaultDummyTarget;
        [SerializeField] private string returnSceneName = "";
        [SerializeField] private bool autoReturnToLobby = true;
        [SerializeField] private float autoReturnDelaySeconds = 2.0f;

        private int currentStepIndex = -1;
        private float stepEnteredAt;
        private bool tutorialFinished;
        private bool returnQueued;
        private float returnQueuedAt;
        private Vector3 spawnPosition;

        private new void Start()
        {
            base.Start();
            if (!CompareTag("MainScript"))
            {
                gameObject.tag = "MainScript";
            }

            PrepareDefaultStepsIfNeeded();
            BeginTutorial();
        }

        private void Update()
        {
            if (HandleEscapeToBackScene(ReturnToLobbyOrConfiguredScene))
            {
                return;
            }

            if (tutorialFinished)
            {
                if (returnQueued && Time.time >= returnQueuedAt)
                {
                    returnQueued = false;
                    ReturnToLobbyOrConfiguredScene();
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                AdvanceStep("debug-next");
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                RestartTutorial();
            }

            var step = GetCurrentStep();
            if (step == null)
            {
                return;
            }

            if (step.kind == TutorialStepKind.Delay && Time.time - stepEnteredAt >= Mathf.Max(0f, step.delaySeconds))
            {
                AdvanceStep("delay");
                return;
            }

            if (step.kind == TutorialStepKind.Final)
            {
                FinishTutorial();
                return;
            }

            var targetDummy = step.kind == TutorialStepKind.DummyDefeated
                ? (step.targetDummy != null ? step.targetDummy : defaultDummyTarget)
                : null;

            if (targetDummy != null && targetDummy.CurrentHealth <= 0f)
            {
                AdvanceStep("dummy-defeated");
            }
        }

        public override void OnMyPlayerDead()
        {
            if (tutorialFinished)
            {
                return;
            }

            RespawnPlayer();
        }

        public void RestartTutorial()
        {
            tutorialFinished = false;
            returnQueued = false;
            currentStepIndex = -1;
            SpawnPlayer();
            AdvanceStep("restart", true);
        }

        public void AdvanceStep(string reason = "", bool force = false)
        {
            if (tutorialFinished && !force)
            {
                return;
            }

            if (steps.Count == 0)
            {
                FinishTutorial();
                return;
            }

            if (!force && currentStepIndex >= 0)
            {
                var current = steps[Mathf.Clamp(currentStepIndex, 0, steps.Count - 1)];
                if (current != null)
                {
                    switch (current.kind)
                    {
                        case TutorialStepKind.Checkpoint:
                            break;
                        case TutorialStepKind.Message:
                            break;
                        case TutorialStepKind.Delay:
                            break;
                        case TutorialStepKind.DummyDefeated:
                            break;
                        case TutorialStepKind.Final:
                            FinishTutorial();
                            return;
                    }
                }
            }

            currentStepIndex++;
            if (currentStepIndex >= steps.Count)
            {
                FinishTutorial();
                return;
            }

            stepEnteredAt = Time.time;
            UpdateStepUI(GetCurrentStep(), reason);
            Debug.Log($"[TutorialMainScript] Step {currentStepIndex + 1}/{steps.Count}: {GetCurrentStep()?.id} ({reason})");
        }

        public void CompleteCheckpoint(string checkpointId)
        {
            var step = GetCurrentStep();
            if (step == null || tutorialFinished)
            {
                return;
            }

            if (step.kind != TutorialStepKind.Checkpoint)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(step.checkpointId) && !string.Equals(step.checkpointId, checkpointId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            AdvanceStep($"checkpoint:{checkpointId}");
        }

        public void NotifyDummyDefeated(SandboxDummyEnemy dummy)
        {
            var step = GetCurrentStep();
            if (tutorialFinished || step == null || step.kind != TutorialStepKind.DummyDefeated)
            {
                return;
            }

            var targetDummy = step.targetDummy != null ? step.targetDummy : defaultDummyTarget;
            if (targetDummy != null && dummy != targetDummy)
            {
                return;
            }

            AdvanceStep("dummy-notify");
        }

        private void BeginTutorial()
        {
            isStarted = true;
            endFlag = false;
            SpawnPlayer();
            AdvanceStep("start", true);
            if (completionPanel != null)
            {
                completionPanel.SetActive(false);
            }
        }

        private void SpawnPlayer()
        {
            if (player != null)
            {
                Destroy(player);
                player = null;
            }

            spawnPosition = ResolveSpawnPosition();
            player = CreateMyPlayer(spawnPosition, ETeam.NoTeam);
        }

        private Vector3 ResolveSpawnPosition()
        {
            if (respawnPoints != null)
            {
                return GetRandomSpawnPoint(respawnPoints);
            }

            return Vector3.zero;
        }

        private void RespawnPlayer()
        {
            if (tutorialFinished)
            {
                return;
            }

            SpawnPlayer();
        }

        private void FinishTutorial()
        {
            if (tutorialFinished)
            {
                return;
            }

            tutorialFinished = true;
            endFlag = true;

            if (completionPanel != null)
            {
                completionPanel.SetActive(true);
            }

            UpdateCompletionUI();

            if (autoReturnToLobby)
            {
                returnQueued = true;
                returnQueuedAt = Time.time + Mathf.Max(0f, autoReturnDelaySeconds);
            }
        }

        private void ReturnToLobbyOrConfiguredScene()
        {
            if (!string.IsNullOrWhiteSpace(returnSceneName))
            {
                RequestSceneTransition(returnSceneName, "tutorial-return");
                return;
            }

            ReturnToTitle();
        }

        private void ReturnToTitle()
        {
            GoToTitle();
        }

        private void UpdateStepUI(TutorialStep step, string reason)
        {
            if (titleText != null)
            {
                titleText.text = step != null && !string.IsNullOrWhiteSpace(step.title)
                    ? step.title
                    : $"Tutorial {currentStepIndex + 1}/{steps.Count}";
            }

            if (instructionText != null)
            {
                instructionText.text = step != null ? step.instruction : "";
            }

            if (progressText != null)
            {
                progressText.text = step != null
                    ? $"{currentStepIndex + 1}/{steps.Count}"
                    : "";
            }

            if (!string.IsNullOrWhiteSpace(reason))
            {
                Debug.Log($"[TutorialMainScript] UI updated: {reason}");
            }
        }

        private void UpdateCompletionUI()
        {
            if (titleText != null)
            {
                titleText.text = "Tutorial Complete";
            }

            if (instructionText != null)
            {
                instructionText.text = "Great job. You are ready to move into the full game flow.";
            }

            if (progressText != null)
            {
                progressText.text = "Done";
            }
        }

        private TutorialStep GetCurrentStep()
        {
            if (currentStepIndex < 0 || currentStepIndex >= steps.Count)
            {
                return null;
            }

            return steps[currentStepIndex];
        }

        private void PrepareDefaultStepsIfNeeded()
        {
            if (steps != null && steps.Count > 0)
            {
                return;
            }

            steps = new List<TutorialStep>
            {
                new TutorialStep
                {
                    id = "move",
                    title = "Move",
                    instruction = "Move through the first checkpoint to learn basic movement.",
                    kind = TutorialStepKind.Checkpoint,
                    checkpointId = "move"
                },
                new TutorialStep
                {
                    id = "jump",
                    title = "Jump",
                    instruction = "Jump over the gap and pass the next checkpoint.",
                    kind = TutorialStepKind.Checkpoint,
                    checkpointId = "jump"
                },
                new TutorialStep
                {
                    id = "shoot",
                    title = "Shoot",
                    instruction = "Defeat the training dummy to practice aiming and firing.",
                    kind = TutorialStepKind.DummyDefeated,
                    targetDummy = null
                },
                new TutorialStep
                {
                    id = "finish",
                    title = "Finish",
                    instruction = "Reach the final checkpoint to finish the tutorial.",
                    kind = TutorialStepKind.Checkpoint,
                    checkpointId = "finish"
                }
            };
        }

        public override void PostEvent(AbstractGameEvent e)
        {
            if (e == null)
            {
                return;
            }

            if (e is GameStartEvent)
            {
                if (!isStarted)
                {
                    BeginTutorial();
                }
                return;
            }

            if (e is PlayerDeadEvent deadEvent)
            {
                var myPlayerId = player != null ? player.GetComponent<AbstractPlayer>()?.UniqueID().ToString() : null;
                if (!string.IsNullOrWhiteSpace(myPlayerId) && deadEvent.PlayerID() == myPlayerId)
                {
                    RespawnPlayer();
                }
                return;
            }

            Debug.Log($"[TutorialMainScript] PostEvent: {e.EventName}");
        }
    }
}
