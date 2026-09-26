
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;


#pragma warning disable 0414
#pragma warning disable 0219


namespace OpenGS
{

    public enum EKey
    {
        None,
        Left,
        Right,
        Up,
        Down,
        Space

    }



    public struct KeyData
    {
        public int frame;

        public EKey key;

        public KeyData(EKey key,int frame=5 )
        {
            this.key = key;


            this.frame = frame;

        }

        public void Clear()
        {
            frame = 0;
            key = EKey.None;
        }

    }


    [DisallowMultipleComponent]
    public class PlayerInput : MonoBehaviour, IPlayerInput
    {

        [SerializeField] [Required] private AbstractPlayer player;
        [SerializeField][Required] private AbstractBattleSceneMediateObject mediateObject;

        private bool leftFlag = false;

        private bool rightFlag = false;

        private bool downFlag = false;

        [SerializeField] private bool testMode = true;

        [ShowInInspector]private KeyData dash = new();

        private KeyData grenade=new();
        //private KeyData right;

        public bool TestMode => testMode;

        private void Awake()
        {
            if (player == null)
            {
                player = GetComponent<AbstractPlayer>();
            }
        }

        private void Update()
        {
            if (player != null && player.IsDead())
            {
                return;
            }

            if (dash.frame >0)
            {
                dash.frame--;

                if (dash.frame == 0)
                {
                    dash.key = EKey.None;

                }

            }

            if (grenade.frame > 0)
            {
                grenade.frame--;

                if (grenade.frame == 0)
                {
                    grenade.key = EKey.None;
                }
            }

            var current = Keyboard.current;

            // キーボード接続チェック
            if (current == null)
            {
                // キーボードが接続されていないと
                // Keyboard.currentがnullになる
                return;
            }


            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {

                if (dash.key == EKey.Left)
                {

                    //player.LeftDash();

                    dash.Clear();
                    
                }else if (dash.key == EKey.Right)
                {
                    dash.Clear();
                }




            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (dash.key == EKey.Right)
                {

                    //player.RightDash();

                    dash.Clear();
                    
                }else if (dash.key == EKey.Left)
                {

                    dash.Clear();
                }


            }



            if (Input.GetKeyUp(KeyCode.LeftArrow))
            {
                dash.key = EKey.Left;
                dash.frame = 20;

            }


            if (Input.GetKeyUp(KeyCode.RightArrow))
            {
                dash.key = EKey.Right;
                dash.frame = 20;


            }

            if (testMode && Input.GetKeyDown(KeyCode.F1))
            {
                ToggleTestMode();
                ResetInputBuffer();
            }

            if (testMode && Input.GetKeyDown(KeyCode.G))
            {
                grenade.key = EKey.Space;
                grenade.frame = 20;
            }

        }

        public void ToggleTestMode()
        {
            testMode = !testMode;
        }

        public void SetTestMode(bool enabled)
        {
            testMode = enabled;
        }

        public void ResetInputBuffer()
        {
            dash.Clear();
            grenade.Clear();
            leftFlag = false;
            rightFlag = false;
            downFlag = false;
        }

        public bool HasQueuedGrenade()
        {
            return grenade.key != EKey.None;
        }

    }



}
