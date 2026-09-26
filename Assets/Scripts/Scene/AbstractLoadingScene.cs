



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
            return SynchronizationContext.Current ?? new SynchronizationContext();
        }

        public MatchRoomManager MatchRoomManager()
        {
            try
            {
                return DependencyInjectionConfig.Resolve<MatchRoomManager>();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] MatchRoomManager is not available: {ex.Message}");
                return null;
            }
        }


    }
}
