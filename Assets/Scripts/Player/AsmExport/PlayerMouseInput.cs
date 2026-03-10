using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

//using UnityEngine.Device.Input;

namespace OpenGS
{
    public class PlayerMouseInput : MonoBehaviour
    {
        [SerializeField] [Required] private AbstractPlayer player;



        void Update()
        {

            var current = Mouse.current;


            var cursorPosition = current.position.ReadValue();

            var leftButton = current.leftButton;
            if (leftButton.wasPressedThisFrame)
            {
                Debug.Log($"左ボタンが押された！ {cursorPosition}");
            }


            var rightButton = current.rightButton;

            if (rightButton.wasPressedThisFrame)
            {
                if (!player.IsDead())
                {
                    //player.Booster();

                }
            }

            /*

            if (Input.GetMouseButton(0))
            {
                //Shot();

            }

            if (Input.GetMouseButton(1))
            {

                player.Booster();

            }

            */

        }

    }


}
