using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace OpenGSR.Tween {
    public static class ZTweenExtensions {
        public static Tweener<Vector3> ZMove(this Transform target, Vector3 endValue, float duration) {
            var tweener = new Tweener<Vector3>(
                () => target.position,
                val => { if (target != null) target.position = val; },
                endValue, duration, (s, e, t) => Vector3.Lerp(s, e, t));
            ZTweenManager.Instance.AddTween(tweener);
            return tweener;
        }

        public static Tweener<float> ZFade(this CanvasGroup target, float endValue, float duration) {
            var tweener = new Tweener<float>(
                () => target.alpha,
                val => { if (target != null) target.alpha = val; },
                endValue, duration, Mathf.Lerp);
            ZTweenManager.Instance.AddTween(tweener);
            return tweener;
        }

        public static Tweener<Vector3> ZScale(this Transform target, Vector3 endValue, float duration) {
            var tweener = new Tweener<Vector3>(
                () => target.localScale,
                val => { if (target != null) target.localScale = val; },
                endValue, duration, (s, e, t) => Vector3.Lerp(s, e, t));
            ZTweenManager.Instance.AddTween(tweener);
            return tweener;
        }
    }

    public class Tweener<T> : ZTweenManager.ITweener {
        private readonly Func<T> _getter;
        private readonly Action<T> _setter;
        private T _startValue;
        private T _endValue;
        private float _duration;
        private Func<T, T, float, T> _lerp;
        private float _elapsed;
        private Ease _ease = Ease.OutQuad;
        private UniTaskCompletionSource _tcs;

        public Tweener(Func<T> getter, Action<T> setter, T endValue, float duration, Func<T, T, float, T> lerp) {
            _getter = getter;
            _setter = setter;
            _startValue = getter();
            _endValue = endValue;
            _duration = duration;
            _lerp = lerp;
        }

        public Tweener<T> SetEase(Ease ease) {
            _ease = ease;
            return this;
        }

        public bool Update(float deltaTime) {
            if (_duration <= 0) {
                _setter(_endValue);
                _tcs?.TrySetResult();
                return true;
            }

            _elapsed += deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            float easedT = ZEase.Calculate(_ease, t);
            
            _setter(_lerp(_startValue, _endValue, easedT));

            if (t >= 1f) {
                _tcs?.TrySetResult();
                return true;
            }
            return false;
        }

        public UniTask ToUniTask() {
            if (_tcs == null) _tcs = new UniTaskCompletionSource();
            return _tcs.Task;
        }

        public UniTask.Awaiter GetAwaiter() => ToUniTask().GetAwaiter();
    }
}
