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

        private void Awake()
        {
            if (player == null)
            {
                player = GetComponent<AbstractPlayer>();
            }
        }


        void Update()
        {
            var current = Mouse.current;
            if (current == null || player == null)
            {
                return;
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
