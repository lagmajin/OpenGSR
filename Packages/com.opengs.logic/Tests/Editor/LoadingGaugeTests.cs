using NUnit.Framework;

namespace OpenGSCore.Tests
{
    public class LoadingGaugeTests
    {
        [Test]
        public void Constructor_ClampsToNormalizedRange()
        {
            var gauge = new LoadingGauge(1.5f);

            Assert.That(gauge.Gauge, Is.EqualTo(1f));
        }

        [Test]
        public void SetPercent_ConvertsPercentToNormalizedRatio()
        {
            var gauge = new LoadingGauge();

            gauge.SetPercent(55f);

            Assert.That(gauge.Gauge, Is.EqualTo(0.55f).Within(0.0001f));
            Assert.That(gauge.ToString(), Is.EqualTo("55%"));
        }

        [Test]
        public void SetRatio_ClampsAndCompletionTracksNormalizedProgress()
        {
            var gauge = new LoadingGauge();

            gauge.SetRatio(1.2f);

            Assert.That(gauge.Gauge, Is.EqualTo(1f));
            Assert.That(gauge.IsComplete(), Is.True);
        }

        [Test]
        public void Clear_ResetsProgress()
        {
            var gauge = new LoadingGauge(0.7f);

            gauge.Clear();

            Assert.That(gauge.Gauge, Is.EqualTo(0f));
            Assert.That(gauge.IsComplete(), Is.False);
        }
    }
}
