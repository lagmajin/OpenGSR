using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace OpenGS
{



    public class GroundCheck : MonoBehaviour
    {
        private bool isGround;
        private bool isGroundEnter = false;
        private bool isGroundStay = false;
        private bool isGroundExit = false;



        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public bool IsGround()
        {
            if (isGroundEnter || isGroundStay)
            {
                isGround = true;
            }
            else if (isGroundExit)
            {
                isGround = false;
            }

            isGroundEnter = false;
            isGroundStay = false;
            isGroundExit = false;

            return isGround;
        }


        void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log("OnTriggerEnter2D: " + other.tag);

            if ("StageObject" == other.tag)
            {
                Debug.Log("Ground");
                //isGround = true;

                isGroundEnter = true;
            }

        }

        void OnTriggerStay2D(Collider2D other)
        {
            if ("StageObject" == other.tag)
            {
                Debug.Log("Ground");
                //isGround = true;

                isGroundStay = true;
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if ("StageObject" == other.tag)
            {
                Debug.Log("Ground");
                //isGround = true;

                isGroundExit = true;
            }
        }
    }


}