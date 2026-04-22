
using UnityEngine;


namespace OpenGS
{
    [DisallowMultipleComponent]
    public class DefenceUpItem : AbstractFieldItem
    {
        public float time = 30.0f;
        //private int heal = 25;

        //private float fHeal = 0.25f;

        private float step_time=0.0f;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            step_time += Time.deltaTime;

            // 3秒後に画面遷移（scene2へ移動）
            if (step_time >= 3.0f)
            {
               // SceneManager.LoadScene("scene2");
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            
        }
    }

}