using OpenGSCore;
using TMPro;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// 待機部屋やショップで、選択中のプレイヤーキャラを表示専用で見せるためのコントローラー。
    /// 実際のキャラ見た目はシーン側のPrefab/オブジェクトに任せ、ここではモーションと表示状態を維持する。
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerCharacterShowcaseController : MonoBehaviour
    {
        private enum ShowcaseMotion
        {
            Standing,
            Jumping,
            Rolling
        }

        [Header("Optional UI")]
        [SerializeField] private TextMeshProUGUI characterNameText;

        [Header("Motion Loop")]
        [SerializeField] private bool useIdleMotion = true;
        [SerializeField] private float bobAmplitude = 0.06f;
        [SerializeField] private float bobFrequency = 1.5f;
        [SerializeField] private float swayDegrees = 2.5f;
        [SerializeField] private float standingDuration = 1.6f;
        [SerializeField] private float jumpDuration = 0.85f;
        [SerializeField] private float rollDuration = 0.65f;
        [SerializeField] private float jumpHeight = 0.35f;
        [SerializeField] private float rollSpinDegrees = 360f;
        [SerializeField] private float rollTiltDegrees = 18f;
        [SerializeField] private float poseBlend = 0.2f;

        [Header("Animator")]
        [SerializeField] private Animator animator;
        [SerializeField] private float animatorSpeed = 1f;
        [SerializeField] private bool restartAnimatorWhenCharacterChanges = true;

        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation;
        private Vector3 baseLocalScale;
        private EPlayerCharacter currentCharacter = EPlayerCharacter.Misty;
        private bool hasCapturedBasePose;
        private float phaseOffset;
        private ShowcaseMotion currentMotion = ShowcaseMotion.Standing;
        private float motionTimer;
        private float motionSeed;

        public EPlayerCharacter CurrentCharacter => currentCharacter;

        private void Awake()
        {
            CacheReferences();
            CaptureBasePose();
            phaseOffset = Random.Range(0f, 10f);
            motionSeed = Random.Range(0f, 10f);
            currentMotion = ShowcaseMotion.Standing;
            motionTimer = 0f;
        }

        private void OnEnable()
        {
            SyncFromGamePlayer();
        }

        private void LateUpdate()
        {
            if (!useIdleMotion)
            {
                return;
            }

            UpdateMotionState(Time.unscaledDeltaTime);
            ApplyIdleMotion();
        }

        /// <summary>
        /// GamePlayerManager に保存されている選択中キャラで表示を同期する。
        /// </summary>
        public void SyncFromGamePlayer()
        {
            SetCharacter(GamePlayerManager.Instance.SelectedPlayerCharacter());
        }

        /// <summary>
        /// 表示中のキャラクターを更新する。
        /// </summary>
        public void SetCharacter(EPlayerCharacter character)
        {
            currentCharacter = character;
            UpdateCharacterLabel();
            RefreshAnimator();
            RestartMotionLoop();
        }

        /// <summary>
        /// Idle モーションのオンオフを切り替える。
        /// </summary>
        public void SetIdleMotionEnabled(bool enabled)
        {
            useIdleMotion = enabled;

            if (!useIdleMotion)
            {
                RestoreBasePose();
            }
            else
            {
                RestartMotionLoop();
            }
        }

        private void CacheReferences()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (characterNameText == null)
            {
                characterNameText = GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        private void CaptureBasePose()
        {
            if (hasCapturedBasePose)
            {
                return;
            }

            baseLocalPosition = transform.localPosition;
            baseLocalRotation = transform.localRotation;
            baseLocalScale = transform.localScale;
            hasCapturedBasePose = true;
        }

        private void ApplyIdleMotion()
        {
            if (!hasCapturedBasePose)
            {
                CaptureBasePose();
            }

            var t = Time.unscaledTime + phaseOffset + motionSeed;
            var bob = Mathf.Sin(t * bobFrequency) * bobAmplitude;
            var sway = Mathf.Sin(t * bobFrequency * 0.7f) * swayDegrees;

            var localPosition = baseLocalPosition + new Vector3(0f, bob, 0f);
            var localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, sway);
            var localScale = baseLocalScale;

            var normalized = GetMotionNormalizedTime();
            switch (currentMotion)
            {
                case ShowcaseMotion.Standing:
                    localPosition += new Vector3(0f, 0f, 0f);
                    break;
                case ShowcaseMotion.Jumping:
                    localPosition += new Vector3(0f, Mathf.Sin(normalized * Mathf.PI) * jumpHeight, 0f);
                    var targetJumpScale = new Vector3(
                        baseLocalScale.x * 0.97f,
                        baseLocalScale.y * 1.06f,
                        baseLocalScale.z
                    );
                    localScale = Vector3.Lerp(baseLocalScale, targetJumpScale, Mathf.Sin(normalized * Mathf.PI) * poseBlend);
                    break;
                case ShowcaseMotion.Rolling:
                    localRotation *= Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, rollSpinDegrees, normalized));
                    localRotation *= Quaternion.Euler(0f, 0f, Mathf.Sin(normalized * Mathf.PI) * rollTiltDegrees);
                    localPosition += new Vector3(0.05f * Mathf.Sin(normalized * Mathf.PI), 0.02f * Mathf.Sin(normalized * Mathf.PI * 2f), 0f);
                    break;
            }

            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
            transform.localScale = localScale;
        }

        private void UpdateMotionState(float deltaTime)
        {
            motionTimer += Mathf.Max(0f, deltaTime);
            var currentDuration = GetCurrentMotionDuration();

            if (motionTimer < currentDuration)
            {
                return;
            }

            motionTimer = 0f;
            currentMotion = currentMotion switch
            {
                ShowcaseMotion.Standing => ShowcaseMotion.Jumping,
                ShowcaseMotion.Jumping => ShowcaseMotion.Rolling,
                ShowcaseMotion.Rolling => ShowcaseMotion.Standing,
                _ => ShowcaseMotion.Standing
            };

            if (currentMotion == ShowcaseMotion.Standing)
            {
                RestoreBasePose();
            }
        }

        private void RestartMotionLoop()
        {
            currentMotion = ShowcaseMotion.Standing;
            motionTimer = 0f;
            RestoreBasePose();
        }

        private float GetCurrentMotionDuration()
        {
            return currentMotion switch
            {
                ShowcaseMotion.Standing => Mathf.Max(0.1f, standingDuration),
                ShowcaseMotion.Jumping => Mathf.Max(0.1f, jumpDuration),
                ShowcaseMotion.Rolling => Mathf.Max(0.1f, rollDuration),
                _ => 1f
            };
        }

        private float GetMotionNormalizedTime()
        {
            return Mathf.Clamp01(motionTimer / Mathf.Max(0.1f, GetCurrentMotionDuration()));
        }

        private void RestoreBasePose()
        {
            if (!hasCapturedBasePose)
            {
                CaptureBasePose();
            }

            transform.localPosition = baseLocalPosition;
            transform.localRotation = baseLocalRotation;
            transform.localScale = baseLocalScale;
        }

        private void UpdateCharacterLabel()
        {
            if (characterNameText == null)
            {
                return;
            }

            characterNameText.text = GetCharacterDisplayName(currentCharacter);
        }

        private void RefreshAnimator()
        {
            if (animator == null)
            {
                return;
            }

            animator.speed = animatorSpeed;

            if (restartAnimatorWhenCharacterChanges)
            {
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static string GetCharacterDisplayName(EPlayerCharacter character)
        {
            return CharacterVisualResolver.GetDisplayName(character);
        }
    }
}
