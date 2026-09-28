
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public enum EFlagState
    {
        FlagOnStand,
        FlagOnGround,
        FlagCapturedPlayer

    }

    public static class FlagStateExtensions
    {
        public static bool IsStable(this EFlagState state)
        {
            return state == EFlagState.FlagOnStand;
        }

        public static bool IsCarried(this EFlagState state)
        {
            return state == EFlagState.FlagCapturedPlayer;
        }

        public static bool IsDropped(this EFlagState state)
        {
            return state == EFlagState.FlagOnGround;
        }
    }
}
