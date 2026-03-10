
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

        private void Start()
        {



        }

        private void Update()
        {
            if (isStart)
            {
                var t=Time.deltaTime;

                time -= t;

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

            timeupEvent.Invoke();

            Debug.Log("タイムアップ");
        }

        [Button("タイマーセット")]
        public void SetTime(float t)
        {
            // タイマーの時間をセット
            time = t;

            // タイマーがすでに動いている場合、再スタートを考慮
            if (isStart)
            {
                Debug.Log("タイマーの時間が変更されました");
            }
        }

        [Button("タイマーテスト")]
        public void StartTimer()
        {
            if (time == 0.0f)
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

        }


        public void TestFunc()
        {
            Debug.Log("TRest");
        }

        


    }
}
