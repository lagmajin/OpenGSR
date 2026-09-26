
using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace OpenGS
{

    [DisallowMultipleComponent]
    public class GameTimer:MonoBehaviour
    {
        [SerializeField]public float maxTime = 10.0f;
        [SerializeField] public float time = 0.0f;
        [SerializeField] public bool isStart = false;

        [SerializeField] [ShowInInspector]public UnityEvent timeupEvent;

        private void Awake()
        {
            maxTime = float.IsFinite(maxTime) ? Mathf.Max(0f, maxTime) : 0f;
            time = float.IsFinite(time) ? Mathf.Max(0f, time) : 0f;
        }

        private void OnValidate()
        {
            if (!float.IsFinite(maxTime) || maxTime < 0f) maxTime = 0f;
            if (!float.IsFinite(time) || time < 0f) time = 0f;
        }

        private void Update()
        {
            if (isStart)
            {
                var t=Time.deltaTime;

                if (!float.IsFinite(t) || t < 0f) return;

                // アプリ復帰時などの巨大なフレーム時間で即時消化しない。
                time -= Mathf.Min(t, 0.1f);

                if (time <= 0.0f)
                {
                    TimeUp();
                }
            }


        }

        private void TimeUp()
        {
            isStart = false;
            time = 0.0f;

            if (timeupEvent != null)
            {
                try
                {
                    timeupEvent.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameTimer] timeup listener failed: {ex}");
                }
            }

            Debug.Log("タイムアップ");
        }

        [Button("タイマーセット")]
        public void SetTime(float t)
        {
            // タイマーの時間をセット
            time = float.IsFinite(t) ? Mathf.Max(0f, t) : 0f;

            // タイマーがすでに動いている場合、再スタートを考慮
            if (isStart)
            {
                Debug.Log("タイマーの時間が変更されました");
            }
        }

        [Button("タイマーテスト")]
        public void StartTimer()
        {
            if (!float.IsFinite(time) || time <= 0.0f)
            {
                Debug.Log("");
            }
            else
            {
                isStart = true;
            }


        }

        public void ReStartTimer()
        {
            time = Mathf.Max(0.0f, maxTime);
            isStart = time > 0.0f;
        }

        private void OnDisable()
        {
            isStart = false;
        }


        public void TestFunc()
        {
            Debug.Log("TRest");
        }

        


    }
}
