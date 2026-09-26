using Zenject;
using UnityEngine;

namespace OpenGS
{
    public sealed class ReplayInputService : IInputService, ITickable, ILateTickable
    {
        readonly UnityInputService liveInput;
        readonly ReplaySession session;

        public ReplayInputService(UnityInputService liveInput, ReplaySession session)
        {
            this.liveInput = liveInput;
            this.session = session;
        }

        public void Tick()
        {
        }

        public void LateTick()
        {
            if (session.IsRecording)
            {
                session.CaptureFrame(CaptureLiveFrame());
            }

            if (session.IsPlaying && !session.AdvancePlaybackFrame())
            {
                session.StopPlayback();
            }
        }

        public Vector2 GetAimWorldPosition()
        {
            return TryGetPlaybackFrame(out var frame) ? frame.aimWorldPosition : liveInput.GetAimWorldPosition();
        }

        public Vector2 GetAimDirection(Vector3 origin)
        {
            if (!float.IsFinite(origin.x) || !float.IsFinite(origin.y) || !float.IsFinite(origin.z))
            {
                return Vector2.right;
            }

            var direction = GetAimWorldPosition() - (Vector2)origin;
            return float.IsFinite(direction.x) && float.IsFinite(direction.y) &&
                direction.sqrMagnitude > Mathf.Epsilon
                ? direction.normalized
                : Vector2.right;
        }

        public bool IsFirePressed()
        {
            return TryGetPlaybackFrame(out var frame) ? frame.firePressed : liveInput.IsFirePressed();
        }

        public bool IsFireJustPressed()
        {
            return TryGetPlaybackFrame(out var frame) ? frame.fireJustPressed : liveInput.IsFireJustPressed();
        }

        public bool IsReloadJustPressed()
        {
            return TryGetPlaybackFrame(out var frame) ? frame.reloadJustPressed : liveInput.IsReloadJustPressed();
        }

        public bool IsSwapWeaponJustPressed()
        {
            return TryGetPlaybackFrame(out var frame) ? frame.swapWeaponJustPressed : liveInput.IsSwapWeaponJustPressed();
        }

        public bool IsDropWeaponJustPressed()
        {
            return TryGetPlaybackFrame(out var frame) ? frame.dropWeaponJustPressed : liveInput.IsDropWeaponJustPressed();
        }

        public bool IsGrenadeJustPressed() => TryGetPlaybackFrame(out var frame) ? frame.grenadeJustPressed : liveInput.IsGrenadeJustPressed();
        public bool IsGrenadePressed() => TryGetPlaybackFrame(out var frame) ? frame.grenadePressed : liveInput.IsGrenadePressed();
        public bool IsGrenadeJustReleased() => TryGetPlaybackFrame(out var frame) ? frame.grenadeJustReleased : liveInput.IsGrenadeJustReleased();

        public bool IsJumpJustPressed()
        {
            return TryGetPlaybackFrame(out var frame) ? frame.jumpJustPressed : liveInput.IsJumpJustPressed();
        }

        public bool IsSitJustPressed()
        {
            return TryGetPlaybackFrame(out var frame) ? frame.sitJustPressed : liveInput.IsSitJustPressed();
        }

        public bool IsLieDownJustPressed()
        {
            return TryGetPlaybackFrame(out var frame) ? frame.lieDownJustPressed : liveInput.IsLieDownJustPressed();
        }

        public int GetInstantItemSlotJustPressed()
        {
            return TryGetPlaybackFrame(out var frame) ? frame.instantItemSlotJustPressed : liveInput.GetInstantItemSlotJustPressed();
        }

        public float GetHorizontalAxis()
        {
            return TryGetPlaybackFrame(out var frame) ? frame.horizontal : liveInput.GetHorizontalAxis();
        }

        public float GetVerticalAxis()
        {
            return TryGetPlaybackFrame(out var frame) ? frame.vertical : liveInput.GetVerticalAxis();
        }

        public bool IsJumpPressed()
        {
            return TryGetPlaybackFrame(out var frame) ? frame.jumpPressed : liveInput.IsJumpPressed();
        }

        public bool IsBoosterPressed()
        {
            return TryGetPlaybackFrame(out var frame) ? frame.boosterPressed : liveInput.IsBoosterPressed();
        }

        public bool IsScoreboardJustPressed() => liveInput.IsScoreboardJustPressed();
        public bool IsScoreboardJustReleased() => liveInput.IsScoreboardJustReleased();
        public bool IsMoveRightJustPressed() => liveInput.IsMoveRightJustPressed();
        public bool IsMoveLeftJustPressed() => liveInput.IsMoveLeftJustPressed();
        public bool IsCrouchJustReleased() => liveInput.IsCrouchJustReleased();
        public bool IsLieDownJustReleased() => liveInput.IsLieDownJustReleased();
        public bool IsSprintPressed() => liveInput.IsSprintPressed();
        public bool IsDashJustPressed() => liveInput.IsDashJustPressed();

        public ReplaySession Session => session;

        ReplayFrame CaptureLiveFrame()
        {
            return new ReplayFrame
            {
                aimWorldPosition = liveInput.GetAimWorldPosition(),
                horizontal = liveInput.GetHorizontalAxis(),
                vertical = liveInput.GetVerticalAxis(),
                firePressed = liveInput.IsFirePressed(),
                fireJustPressed = liveInput.IsFireJustPressed(),
                reloadJustPressed = liveInput.IsReloadJustPressed(),
                swapWeaponJustPressed = liveInput.IsSwapWeaponJustPressed(),
                dropWeaponJustPressed = liveInput.IsDropWeaponJustPressed(),
                grenadeJustPressed = liveInput.IsGrenadeJustPressed(),
                grenadePressed = liveInput.IsGrenadePressed(),
                grenadeJustReleased = liveInput.IsGrenadeJustReleased(),
                jumpJustPressed = liveInput.IsJumpJustPressed(),
                sitJustPressed = liveInput.IsSitJustPressed(),
                lieDownJustPressed = liveInput.IsLieDownJustPressed(),
                instantItemSlotJustPressed = liveInput.GetInstantItemSlotJustPressed(),
                jumpPressed = liveInput.IsJumpPressed(),
                boosterPressed = liveInput.IsBoosterPressed(),
            };
        }

        bool TryGetPlaybackFrame(out ReplayFrame frame)
        {
            return session.TryGetCurrentPlaybackFrame(out frame);
        }
    }
}
