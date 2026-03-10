using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenGSR.Tween {
    public class ZTweenManager : MonoBehaviour {
        private static ZTweenManager _instance;
        public static ZTweenManager Instance {
            get {
                if (_instance == null) {
                    _instance = FindFirstObjectByType<ZTweenManager>();
                    if (_instance == null) {
                        var go = new GameObject("ZTweenManager");
                        _instance = go.AddComponent<ZTweenManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private readonly List<ITweener> _activeTweens = new List<ITweener>(100);
        private readonly List<ITweener> _toRemove = new List<ITweener>(100);

        private void Update() {
            float dt = Time.deltaTime;
            for (int i = 0; i < _activeTweens.Count; i++) {
                if (_activeTweens[i].Update(dt)) {
                    _toRemove.Add(_activeTweens[i]);
                }
            }
            if (_toRemove.Count > 0) {
                for (int i = 0; i < _toRemove.Count; i++) {
                    _activeTweens.Remove(_toRemove[i]);
                }
                _toRemove.Clear();
            }
        }

        public void AddTween(ITweener tweener) => _activeTweens.Add(tweener);

        public interface ITweener {
            bool Update(float deltaTime);
        }
    }
}
