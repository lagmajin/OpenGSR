using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace OpenGS
{

    [DisallowMultipleComponent]

    public class MissionWaitroomScript : MonoBehaviour
    {
        // Start is called before the first frame update


        [TabGroup("")][SerializeField][Required] public MissionWaitroomScript script;

        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }


        void OnApplicationQuit()
        {

        }
        
    }


}