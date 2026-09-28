namespace OpenGSCore
{
    public class LoadingGauge
    {
        private float gauge;

        public LoadingGauge(float gauge = 0.0f)
        {
            Gauge = gauge;
        }

        /// <summary>
        /// Normalized loading progress in the range 0..1.
        /// </summary>
        public float Gauge
        {
            get => gauge;
            set => gauge = Clamp01(value);
        }

        /// <summary>
        /// Sets the progress using a percentage value in the range 0..100.
        /// </summary>
        public void SetPercent(float percent)
        {
            Gauge = percent / 100f;
        }

        /// <summary>
        /// Sets the progress using a normalized ratio in the range 0..1.
        /// </summary>
        public void SetRatio(float ratio)
        {
            Gauge = ratio;
        }

        public void Full()
        {
            Gauge = 1f;
        }

        public bool IsComplete()
        {
            return Gauge >= 1.0f;
        }

        public void Clear()
        {
            Gauge = 0.0f;
        }

        public override string ToString()
        {
            return $"{Gauge * 100f:0.##}%";
        }

        public static implicit operator float(LoadingGauge gauge)
        {
            return gauge?.Gauge ?? 0f;
        }

        public static implicit operator LoadingGauge(float gauge)
        {
            return new LoadingGauge(gauge);
        }

        private static float Clamp01(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }
    }
}
