using System.Collections;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
   
    internal interface IAbstractPlayerAgent
    {

    }

    [DisallowMultipleComponent]
    public class AbstractPlayerAgent : MonoBehaviour
    {
        [SerializeField] Transform playerTransform;
        
        private string? playerID;
        private EPlayerType playerType;

        private void Start()
        {
            
        }

        public void SetPlayerID(PlayerID id)
        {

        }

        public EPlayerType PlayerType()
        {
            return playerType;
        }

        public void SetPlayerType(EPlayerType type = EPlayerType.Unknown)
        {
            playerType = type;

        }


        private void OnApplicationQuit()
        {
            
        }

    }
}