using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public interface IPlayerMatchFinalScore
    {
        public float CalcTotalPoint()=>0;
    }

    public abstract class AbstractPlayerMatchFinalScore
    {
        //public 
        public string PlayerName{ get; private set; }

        public string PlayerId { get; private set; }

        public int Kill { get; private set; } = 0;

        public int Death { get; private set; } = 0;

        public int Suicide { get; private set; } = 0;

        public int? Rank { get; private set; } = null;

        public float TotalPoint { get; private set; } = 0;

        public EGameMode Mode { get; private set; }

        public AbstractPlayerMatchFinalScore()
        {
            PlayerName = string.Empty;
            PlayerId = string.Empty;
            Mode = EGameMode.Unknown;
        }

        public void Initialize(string playerName, string playerId, EGameMode mode)
        {
            SetPlayerInfo(playerName, playerId);
            SetMode(mode);
            ResetScore();
        }

        protected void SetPlayerInfo(string playerName, string playerId)
        {
            PlayerName = playerName ?? string.Empty;
            PlayerId = playerId ?? string.Empty;
        }

        protected void SetMode(EGameMode mode)
        {
            Mode = mode;
        }

        public void ResetScore()
        {
            Kill = 0;
            Death = 0;
            Suicide = 0;
            Rank = null;
            TotalPoint = 0;
        }

        public void SetScore(int kill, int death, int suicide)
        {
            Kill = Math.Max(0, kill);
            Death = Math.Max(0, death);
            Suicide = Math.Max(0, suicide);
        }

        public void SetTotalPoint(float totalPoint)
        {
            TotalPoint = float.IsFinite(totalPoint) ? totalPoint : 0f;
        }

        public void AddKill(int value = 1)
        {
            if (value <= 0) return;
            Kill += value;
        }

        public void AddDeath(int value = 1)
        {
            if (value <= 0) return;
            Death += value;
        }

        public void AddSuicide(int value = 1)
        {
            if (value <= 0) return;
            Suicide += value;
        }

        public void SetRank(int? rank)
        {
            Rank = rank;
        }

        public virtual float CalcTotalPoint()
        {
            TotalPoint = Kill * 100f - Death * 50f - Suicide * 100f;
            return TotalPoint;
        }

    }
    
    
    public class PlayerDeathMatchFinalScore:AbstractPlayerMatchFinalScore
    {
        public override float CalcTotalPoint()
        {
            return base.CalcTotalPoint();
        }

    }
    
    public class PlayerTeamDeathMatchFinalScore:AbstractPlayerMatchFinalScore
    {
        public override float CalcTotalPoint()
        {
            return base.CalcTotalPoint();
        }

    }

    public class PlayerCTFMatchFinalScore: AbstractPlayerMatchFinalScore
    {
        public override float CalcTotalPoint()
        {
            return base.CalcTotalPoint();
        }

    }
}
