using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace OpenGS
{



    [DisallowMultipleComponent]
    public class FPSAndPing : MonoBehaviour
    {
        int frameCount;
        float prevTime;

        float fps;

        // Start is called before the first frame update
        void Start()
        {
#if UNITY_EDITOR
            frameCount = 0;
            prevTime = 0.0f;

#endif
        }

        // Update is called once per frame
        void Update()
        {
#if UNITY_EDITOR
            frameCount++;
            float time = Time.realtimeSinceStartup - prevTime;

            if (time >= 0.5f)
            {
                fps = frameCount / time;
                //Debug.Log(fps);

                frameCount = 0;
                prevTime = Time.realtimeSinceStartup;
            }

#endif

        }

        //[Conditional("UNITY_EDITOR")]
        private void OnGUI()
        {
#if UNITY_EDITOR
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 36;  // フォントサイズを20に設定
            labelStyle.normal.textColor = Color.red;  // テキストカラーを赤に設定
            GUILayout.Label("[FPS]" + fps.ToString(),labelStyle);
            GUILayout.Label("[Ping]" + "" + "ms",labelStyle);
#endif
        }
    }

}