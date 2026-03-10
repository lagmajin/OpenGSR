using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{

    [DisallowMultipleComponent]
    public class PlayerServerLinker:MonoBehaviour
    {
        [Required]public AbstractPlayer player;
        void Start()
        {

        }

        void Update()
        {
            if (player)
            {

                var position = player.transform.position;



            }


        }

        private void SendPlayerDataToServer()
        {
            
            
            


        }


    }
}
