using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGSCore;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    public class DeathMatchResultUIManager: AbstractMatchResultUIManager
    {
        [SerializeField] public Image background;
        [SerializeField]public Image attackMVPImage;
        [SerializeField]public Image defenceMPVImage;

        void Awake()
        {

        }
        public void ShowResult(DeathMatchFinalScore score)
        {
            if (score == null)
            {
                UpdateResultList(null);
                return;
            }

            var allPlayerScore = score.AllPlayerFinalScores()?.AllPlayerFinalScore();
            if (allPlayerScore == null)
            {
                UpdateResultList(null);
                return;
            }

            var resultRows = allPlayerScore
                .Where(player => player != null)
                .Select(player => new PlayerMatchResultData
                {
                    PlayerId = player.PlayerId,
                    PlayerName = player.PlayerName,
                    Team = string.Empty,
                    Kills = player.Kill,
                    Deaths = player.Death,
                    Score = Mathf.RoundToInt(player.TotalPoint)
                })
                .ToList();

            UpdateResultList(resultRows);


        }

    }
}
