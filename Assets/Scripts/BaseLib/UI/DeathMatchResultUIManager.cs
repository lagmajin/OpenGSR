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
            var allPlayerScore = score.AllPlayerFinalScores().AllPlayerFinalScore();

            var sortedList=allPlayerScore.OrderBy(s => s.Rank)
                                  .ToList();




            //var sorted=allPlayerScore.Orderd


        }

    }
}
