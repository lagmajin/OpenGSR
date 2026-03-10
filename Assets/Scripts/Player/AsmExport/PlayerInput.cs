
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

        private void Start()
        {

        }

        private void Update()
        {
            if (dash.frame >0)
            {
                dash.frame--;

                if (dash.frame == 0)
                {
                    dash.key = EKey.None;

                    Debug.Log("Expire");
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


            var spaceKey = current.spaceKey;

            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {

                if (dash.key == EKey.Left)
                {

                    Debug.Log("Left Dash");

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

                    Debug.Log("Right Dash");

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

                Debug.Log("Left");
            }


            if (Input.GetKeyUp(KeyCode.RightArrow))
            {
                dash.key = EKey.Right;
                dash.frame = 20;

                Debug.Log("Right");

            }

            if (Input.GetKeyUp(KeyCode.S))
            {
                if (player.Sitting())
                {

                    player.StandUp();

                }

            }



            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                player.DropCurrentWeapon();

            }


            if (Input.GetKeyDown(KeyCode.R))
            {
                if (!player.ReloadingNow())
                {
                    player.ReloadStart();
                }


            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                if (player.IsGround())
                {
                    //player.Jump();
                }


            }

            if (Input.GetKeyDown(KeyCode.X))
            {


                player.LieDown();


            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                if (!player.Sitting())
                {
                    player.Sit();
                }



            }

            if (Input.GetKeyUp(KeyCode.S))
            {


            }


            if (Input.GetKeyDown(KeyCode.Space))
            {


            }


            if (Input.GetKeyUp(KeyCode.Space))
            {
                //OpenGranade();


            }



            if (Input.GetKeyDown(KeyCode.Keypad1))
            {
                player.UseItem(1);


            }

            if (Input.GetKeyDown(KeyCode.Keypad2))
            {
                //UseItem(2
            }

            if (Input.GetKeyDown(KeyCode.Keypad3))
            {
                //UseItem(3);

            }


            if (Input.GetKeyDown(KeyCode.Tab))
            {
                SendDropWeapon();
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                SendSwapWeapon();
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {

            }

        }

        private void SendLeftDash()
        {
            //player?.LeftDash();
            
        }

        private void SendRightDash()
        {
            //player?.RightDash();
        }
        private void SendOpenGrenade()
        {

        }

        private void SendSwapWeapon()
        {
            player?.SwapWeapon();
        }

        private void SendDropWeapon()
        {
            player?.DropCurrentWeapon();
        }
    }



}
