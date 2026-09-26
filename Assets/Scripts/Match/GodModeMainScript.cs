using System.Collections.Generic;
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    public class GodModeMainScript : AbstractMatchMainScript, IGodModeMainScript
    {
        public new void Start()
        {
            base.Start();
            Debug.Log("[GodModeMainScript] Start");
        }

        private void Update()
        {
            if (HandleEscapeToBackScene())
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                EndMatch();
            }
        }

        private void EndMatch()
        {
            if (!TryBeginMatchEnd())
            {
                return;
            }

            StoreOfflineMatchResult();
            ScheduleResultSceneTransition(0f);
        }

        private void StoreOfflineMatchResult()
        {
            if (GameManager != null && GameManager.IsOnlineGameMode)
            {
                return;
            }

            var manager = matchRoomManager ?? MatchRoomManager();
            var players = manager?.WaitRoom?.AllPlayers() ?? new List<PlayerInfo>();
            var evaluator = MatchResultEvaluatorFactory.CreateEvaluator(EGameMode.DeathMatch);
            var result = evaluator.Evaluate(null, players);
            result["GodMode"] = true;
            manager?.StoreOfflineMatchResult(result);
        }

        public override void PostEvent(AbstractGameEvent e)
        {
            Debug.Log($"[GodModeMainScript] PostEvent: {e?.EventName ?? "null"}");
        }
    }
}
