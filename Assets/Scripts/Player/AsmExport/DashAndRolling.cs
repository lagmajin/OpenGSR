using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;



namespace OpenGS
{

    struct PushFrame
    {
        bool push;
        private int frameCount;



    }


    [DisallowMultipleComponent]
    public class DashAndRolling : MonoBehaviour
    {


        private bool leftPush=false;
        private bool rightPush = false;

        private bool downPush = false;

        [SerializeField] private AbstractPlayer player;
        [SerializeField] private float frame = 10.0f;

        [ShowInInspector]private float dashDelay = 12.0f;

        [ShowInInspector]private float dashDelayLeft = 0.0f;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {




            if (Input.GetKeyDown(KeyCode.A))
            {
                if (leftPush)
                {
                   // player.LeftDash();
                }
                else
                {
                    //leftPush = true;
                }

                

            }

            if (Input.GetKeyUp(KeyCode.A))
            {

                leftPush = true;

            }



            if (Input.GetKeyDown(KeyCode.D))
            {



                rightPush = true;

            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                downPush = false;

            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                downPush = true;
                leftPush = false;
                rightPush = false;

            }


    

            if (Input.GetKeyUp(KeyCode.D))
            {

            }


        }

        void SendLeftDash(EDirection direction)
        {
            if (player)
            {

            }



        }

        void SendLoling()
        {
            if (player)
            {
                
            }


        }

    }

    


}