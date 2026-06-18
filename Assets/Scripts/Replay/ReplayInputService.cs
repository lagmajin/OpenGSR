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
            return (GetAimWorldPosition() - (Vector2)origin).normalized;
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
