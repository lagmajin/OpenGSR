using System;
using System.Linq;
using NUnit.Framework;

namespace OpenGSCore.Tests
{
    /// <summary>
    /// C1 requires every supported mode to have a concrete rule path and
    /// unsupported modes to fail clearly instead of drifting silently.
    /// </summary>
    public class MatchResultEvaluatorFactoryTests
    {
        [Test]
        public void EveryModeExceptUnknownHasAnEvaluator()
        {
            // Unity compiles this package with C# 9, so the generic
            // Enum.GetValues<T>() overload is not available here.
            foreach (EGameMode mode in Enum.GetValues(typeof(EGameMode)))
            {
                if (mode == EGameMode.Unknown)
                {
                    continue;
                }

                Assert.That(
                    MatchResultEvaluatorFactory.CreateEvaluator(mode),
                    Is.Not.Null,
                    $"{mode} must resolve to a result evaluator");
            }
        }

        [Test]
        public void UnknownModeFailsLoudly()
        {
            Assert.Throws<NotSupportedException>(
                () => MatchResultEvaluatorFactory.CreateEvaluator(EGameMode.Unknown));
        }

        [Test]
        public void UndefinedModeValueFailsLoudly()
        {
            Assert.Throws<NotSupportedException>(
                () => MatchResultEvaluatorFactory.CreateEvaluator((EGameMode)200));
        }

        [Test]
        public void TeamModesUseTeamEvaluators()
        {
            Assert.That(
                MatchResultEvaluatorFactory.CreateEvaluator(EGameMode.TeamDeathMatch),
                Is.TypeOf<TeamDeathMatchResultEvaluator>());

            Assert.That(
                MatchResultEvaluatorFactory.CreateEvaluator(EGameMode.CaptureTheFlag),
                Is.TypeOf<CaptureTheFlagResultEvaluator>());
        }

        [Test]
        public void SoloModesUseDeathMatchEvaluator()
        {
            // These modes have no dedicated evaluator yet, but they are still
            // explicitly routed so the intent stays visible.
            Assert.That(
                MatchResultEvaluatorFactory.CreateEvaluator(EGameMode.OneShotKill),
                Is.TypeOf<DeathMatchResultEvaluator>());
        }
    }
}