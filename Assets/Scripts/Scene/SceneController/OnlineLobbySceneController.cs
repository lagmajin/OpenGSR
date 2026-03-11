using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
//using Unity.VisualScripting;
using UnityEngine;

namespace OpenGS
{

    public class OnlineLobbySceneController : MonoBehaviour
    {
        [SerializeField] [Required] private WaitRoomMediateObject mediateObject;


        void Start()
        {


        }


        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {

                //mediateObject.

            }

            if (Input.GetKeyDown(KeyCode.R))
            {

            }




        }


    }
}
