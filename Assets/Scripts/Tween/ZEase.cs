namespace OpenGSR.Tween {
    public enum Ease { Linear, InQuad, OutQuad, InOutQuad, InCubic, OutCubic }

    public static class ZEase {
        public static float Calculate(Ease ease, float t) {
            switch (ease) {
                case Ease.Linear: return t;
                case Ease.InQuad: return t * t;
                case Ease.OutQuad: return t * (2 - t);
                case Ease.InOutQuad: return t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;
                case Ease.InCubic: return t * t * t;
                case Ease.OutCubic: return 1 - UnityEngine.Mathf.Pow(1 - t, 3);
                default: return t;
            }
        }
    }
}
