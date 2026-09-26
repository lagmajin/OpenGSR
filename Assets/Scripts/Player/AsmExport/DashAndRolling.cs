using System;
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
        private bool rollPush = false;

        [SerializeField] private AbstractPlayer player;
        public event Action<EDirection> DashRequested;
        public event Action RollRequested;

        public bool IsLeftPressed => leftPush;
        public bool IsRightPressed => rightPush;
        public bool IsRollPressed => rollPush;
        // Start is called before the first frame update
        void Start()
        {
            if (player == null)
            {
                player = GetComponent<AbstractPlayer>();
            }
        }

        // Update is called once per frame
        void Update()
        {




            leftPush = Input.GetKey(KeyCode.A);
            rightPush = Input.GetKey(KeyCode.D);
            rollPush = Input.GetKey(KeyCode.W);

            if (Input.GetKeyDown(KeyCode.A))
            {
                SendLeftDash(EDirection.Left);
            }

            if (Input.GetKeyUp(KeyCode.A))
            {
                leftPush = false;
            }



            if (Input.GetKeyDown(KeyCode.D))
            {
                SendLeftDash(EDirection.Right);

            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                SendLoling();
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                leftPush = false;
                rightPush = false;
                rollPush = false;

            }


    

            if (Input.GetKeyUp(KeyCode.D))
            {
                rightPush = false;
            }


        }

        void SendLeftDash(EDirection direction)
        {
            InvokeSafely(DashRequested, direction, nameof(DashRequested));


        }

        void SendLoling()
        {
            InvokeSafely(RollRequested, nameof(RollRequested));


        }

        private static void InvokeSafely(Action<EDirection> handlers, EDirection direction, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<EDirection> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(direction);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DashAndRolling] {eventName} subscriber failed: {ex}");
                }
            }
        }

        private static void InvokeSafely(Action handlers, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action handler in handlers.GetInvocationList())
            {
                try
                {
                    handler();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DashAndRolling] {eventName} subscriber failed: {ex}");
                }
            }
        }

    }

    


}
