



using Sirenix.OdinInspector;
using System.Threading;
using UnityEngine;

namespace OpenGS
{
    public class AbstractLoadingScene:AbstractNonBattleScene
    {
        //[SerializeField] protected GameTimer timer; 

        [SerializeField][Required]public MapSceneMasterData mapSelectMasterData;

        public override SynchronizationContext MainThread()
        {
            return SynchronizationContext.Current;
        }

        public MatchRoomManager MatchRoomManager()
        {
            return DependencyInjectionConfig.Resolve<MatchRoomManager>();
            
        }


    }
}
