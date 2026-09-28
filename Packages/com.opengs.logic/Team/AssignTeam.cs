#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public class AssignTeamResult
    {
        public List<string> RedTeam { get; private set; } = new();
        public List<string> BlueTeam { get; private set; } = new();

        public AssignTeamResult(List<string> redTeam, List<string> blueTeam)
        {
            RedTeam = redTeam;
            BlueTeam = blueTeam;
        }
    }

    public class AssignTeam
    {
        public static AssignTeamResult? Random(List<string> idList)
        {
            if (idList.Count == 0)
            {
                Console.WriteLine("[AssignTeam] Cannot assign teams from an empty id list.");
                return null;
            }

            // シャッフル用のコピーを作る
            var shuffled = new List<string>(idList);
            var random = new Random();

            // Fisher-Yatesシャッフル
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            // 前半をRedTeam、後半をBlueTeamに分ける
            int mid = shuffled.Count / 2;
            var redTeam = shuffled.GetRange(0, mid);
            var blueTeam = shuffled.GetRange(mid, shuffled.Count - mid);

            var result = new AssignTeamResult(redTeam, blueTeam);

            return result;
        }

    }
}
