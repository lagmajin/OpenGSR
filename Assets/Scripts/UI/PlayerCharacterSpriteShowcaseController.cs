using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OpenGSCore;

namespace OpenGS
{
    /// <summary>
    /// UI 上でプレイヤーキャラのスプライトアニメを再生するコントローラー。
    /// standing -> jump -> roll を順番に回し、各モーションは Sprite 配列で再生する。
    /// RectTransform を直接動かして、Canvas 内で自然に見えるようにしている。
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerCharacterSpriteShowcaseController : MonoBehaviour
    {
        private enum MotionState
        {
            Standing,
            Jumping,
            Rolling
        }

        [Header("Target")]
        [SerializeField] private Image targetImage;
        [SerializeField] private RectTransform targetRectTransform;
        [SerializeField] private TextMeshProUGUI characterNameText;

        [Header("Standing Frames")]
        [SerializeField] private Sprite[] standingFrames;
        [SerializeField] private float standingFps = 6f;
        [SerializeField] private float standingDuration = 1.5f;

        [Header("Jump Frames")]
        [SerializeField] private Sprite[] jumpingFrames;
        [SerializeField] private float jumpingFps = 10f;
        [SerializeField] private float jumpingDuration = 0.8f;

        [Header("Roll Frames")]
        [SerializeField] private Sprite[] rollingFrames;
        [SerializeField] private float rollingFps = 12f;
        [SerializeField] private float rollingDuration = 0.7f;

        [Header("UI Motion")]
        [SerializeField] private float standingBobAmplitude = 4f;
        [SerializeField] private float standingBobFrequency = 1.5f;
        [SerializeField] private float standingSwayDegrees = 1.5f;
        [SerializeField] private float jumpHeight = 18f;
        [SerializeField] private float rollTravelDistance = 12f;
        [SerializeField] private float rollSpinDegrees = 360f;
        [SerializeField] private float rollTiltDegrees = 16f;

        [Header("Behavior")]
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private bool loopSequence = true;
        [SerializeField] private bool resetToFirstFrameOnLoop = true;
        [SerializeField] private bool preserveImageAspect = true;

        private MotionState currentState = MotionState.Standing;
        private float stateTimer;
        private float frameTimer;
        private int frameIndex;
        private EPlayerCharacter currentCharacter = EPlayerCharacter.Misty;
        private Sprite cachedInitialSprite;
        private Vector3 cachedAnchoredPosition;
        private Quaternion cachedLocalRotation;
        private Vector3 cachedLocalScale;
        private bool hasCachedRectPose;
        private float motionSeed;

        public EPlayerCharacter CurrentCharacter => currentCharacter;

        private void Awake()
        {
            CacheReferences();
            CacheInitialPose();
            CacheInitialSprite();
            motionSeed = Random.Range(0f, 10f);
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                RestartShowcase();
            }
        }

        private void Update()
        {
            if (!playOnEnable)
            {
                return;
            }

            var deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            Tick(deltaTime);
        }

        /// <summary>
        /// 外部から現在の選択キャラを同期する。
        /// </summary>
        public void SyncFromGamePlayer()
        {
            SetCharacter(GamePlayerManager.Instance.SelectedPlayerCharacter());
        }

        /// <summary>
        /// 表示中のキャラクター名を更新する。
        /// </summary>
        public void SetCharacter(EPlayerCharacter character)
        {
            currentCharacter = character;
            if (characterNameText != null)
            {
                characterNameText.text = GetCharacterDisplayName(character);
            }
        }

        /// <summary>
        /// 再生を先頭からやり直す。
        /// </summary>
        public void RestartShowcase()
        {
            currentState = MotionState.Standing;
            stateTimer = 0f;
            frameTimer = 0f;
            frameIndex = 0;
            RestoreInitialPose();
            ApplyCurrentFrame(forceFirstFrame: true);
        }

        /// <summary>
        /// 再生を止めて初期状態へ戻す。
        /// </summary>
        public void StopShowcase()
        {
            playOnEnable = false;
            RestoreInitialSprite();
            RestoreInitialPose();
        }

        private void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                deltaTime = 0f;
            }

            stateTimer += deltaTime;
            frameTimer += deltaTime;

            if (stateTimer >= GetCurrentDuration())
            {
                AdvanceState();
            }

            ApplyCurrentFrame(forceFirstFrame: false);
            ApplyCurrentPose();
        }

        private void AdvanceState()
        {
            stateTimer = 0f;
            frameTimer = 0f;
            frameIndex = 0;

            currentState = currentState switch
            {
                MotionState.Standing => MotionState.Jumping,
                MotionState.Jumping => MotionState.Rolling,
                MotionState.Rolling => loopSequence ? MotionState.Standing : MotionState.Rolling,
                _ => MotionState.Standing
            };

            if (!loopSequence && currentState == MotionState.Rolling)
            {
                stateTimer = GetCurrentDuration();
            }

            if (resetToFirstFrameOnLoop)
            {
                ApplyCurrentFrame(forceFirstFrame: true);
            }
        }

        private void ApplyCurrentFrame(bool forceFirstFrame)
        {
            var frames = GetCurrentFrames();
            if (frames == null || frames.Length == 0)
            {
                if (currentState == MotionState.Standing && cachedInitialSprite != null)
                {
                    SetSprite(cachedInitialSprite);
                }

                return;
            }

            if (forceFirstFrame)
            {
                frameIndex = 0;
                frameTimer = 0f;
                SetSprite(frames[frameIndex]);
                return;
            }

            var fps = GetCurrentFps();
            if (fps <= 0f || frames.Length == 1)
            {
                SetSprite(frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)]);
                return;
            }

            var frameInterval = 1f / fps;
            while (frameTimer >= frameInterval)
            {
                frameTimer -= frameInterval;
                frameIndex++;

                if (frameIndex >= frames.Length)
                {
                    if (loopSequence)
                    {
                        frameIndex = 0;
                    }
                    else
                    {
                        frameIndex = frames.Length - 1;
                        break;
                    }
                }
            }

            SetSprite(frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)]);
        }

        private void ApplyCurrentPose()
        {
            if (targetRectTransform == null)
            {
                return;
            }

            var normalized = Mathf.Clamp01(stateTimer / Mathf.Max(0.1f, GetCurrentDuration()));
            var basePos = cachedAnchoredPosition;
            var baseRot = cachedLocalRotation;
            var baseScale = cachedLocalScale;

            switch (currentState)
            {
                case MotionState.Standing:
                {
                    var t = Time.unscaledTime + motionSeed;
                    var bob = Mathf.Sin(t * standingBobFrequency) * standingBobAmplitude;
                    var sway = Mathf.Sin(t * standingBobFrequency * 0.7f) * standingSwayDegrees;
                    targetRectTransform.anchoredPosition3D = basePos + new Vector3(0f, bob, 0f);
                    targetRectTransform.localRotation = baseRot * Quaternion.Euler(0f, 0f, sway);
                    targetRectTransform.localScale = baseScale;
                    break;
                }
                case MotionState.Jumping:
                {
                    var height = Mathf.Sin(normalized * Mathf.PI) * jumpHeight;
                    var squeeze = 1f - Mathf.Sin(normalized * Mathf.PI) * 0.05f;
                    targetRectTransform.anchoredPosition3D = basePos + new Vector3(0f, height, 0f);
                    targetRectTransform.localRotation = baseRot;
                    targetRectTransform.localScale = new Vector3(
                        baseScale.x * squeeze,
                        baseScale.y * (1f + 0.06f * Mathf.Sin(normalized * Mathf.PI)),
                        baseScale.z
                    );
                    break;
                }
                case MotionState.Rolling:
                {
                    var travel = Mathf.Sin(normalized * Mathf.PI) * rollTravelDistance;
                    var spin = Mathf.Lerp(0f, rollSpinDegrees, normalized);
                    var tilt = Mathf.Sin(normalized * Mathf.PI) * rollTiltDegrees;
                    targetRectTransform.anchoredPosition3D = basePos + new Vector3(travel, 0f, 0f);
                    targetRectTransform.localRotation = baseRot * Quaternion.Euler(0f, 0f, spin + tilt);
                    targetRectTransform.localScale = baseScale;
                    break;
                }
            }
        }

        private void CacheReferences()
        {
            if (targetImage == null)
            {
                targetImage = GetComponentInChildren<Image>(true);
            }

            if (targetRectTransform == null)
            {
                if (targetImage != null)
                {
                    targetRectTransform = targetImage.rectTransform;
                }
                else
                {
                    targetRectTransform = GetComponent<RectTransform>();
                }
            }

            if (characterNameText == null)
            {
                characterNameText = GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        private void CacheInitialPose()
        {
            if (targetRectTransform == null || hasCachedRectPose)
            {
                return;
            }

            cachedAnchoredPosition = targetRectTransform.anchoredPosition3D;
            cachedLocalRotation = targetRectTransform.localRotation;
            cachedLocalScale = targetRectTransform.localScale;
            hasCachedRectPose = true;
        }

        private void CacheInitialSprite()
        {
            cachedInitialSprite = GetCurrentSprite();
        }

        private void RestoreInitialPose()
        {
            if (targetRectTransform == null)
            {
                return;
            }

            if (!hasCachedRectPose)
            {
                CacheInitialPose();
            }

            targetRectTransform.anchoredPosition3D = cachedAnchoredPosition;
            targetRectTransform.localRotation = cachedLocalRotation;
            targetRectTransform.localScale = cachedLocalScale;
        }

        private void RestoreInitialSprite()
        {
            if (cachedInitialSprite != null)
            {
                SetSprite(cachedInitialSprite);
            }
        }

        private void SetSprite(Sprite sprite)
        {
            if (targetImage == null || sprite == null)
            {
                return;
            }

            targetImage.sprite = sprite;
            targetImage.preserveAspect = preserveImageAspect;
        }

        private Sprite GetCurrentSprite()
        {
            return targetImage != null ? targetImage.sprite : null;
        }

        private Sprite[] GetCurrentFrames()
        {
            return currentState switch
            {
                MotionState.Standing => standingFrames,
                MotionState.Jumping => jumpingFrames,
                MotionState.Rolling => rollingFrames,
                _ => standingFrames
            };
        }

        private float GetCurrentFps()
        {
            return currentState switch
            {
                MotionState.Standing => standingFps,
                MotionState.Jumping => jumpingFps,
                MotionState.Rolling => rollingFps,
                _ => standingFps
            };
        }

        private float GetCurrentDuration()
        {
            return currentState switch
            {
                MotionState.Standing => Mathf.Max(0.1f, standingDuration),
                MotionState.Jumping => Mathf.Max(0.1f, jumpingDuration),
                MotionState.Rolling => Mathf.Max(0.1f, rollingDuration),
                _ => 1f
            };
        }

        private static string GetCharacterDisplayName(EPlayerCharacter character)
        {
            return CharacterVisualResolver.GetDisplayName(character);
        }
    }
}
