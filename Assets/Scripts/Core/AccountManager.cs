using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    [System.Serializable]
    public sealed class AccountProfile
    {
        public string AccountName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string GlobalUserId { get; set; } = "";
        public string GlobalMyIP { get; set; } = "";
        public bool IsOnline { get; set; } = false;
        public long Credits { get; set; } = 1000;
        public int Level { get; set; } = 1;
        public long Exp { get; set; } = 0L;
    }

    //#AccountManager
    public class AccountManager
    {
        public static AccountManager Instance { get; } = new();

        public AccountProfile CurrentProfile { get; private set; } = new AccountProfile();
        public OpenGSCore.PlayerInfo PlayerInfo { get; private set; } = new OpenGSCore.PlayerInfo();

        private AccountManager()
        {
        }

        public void LoginData(in string accountName, in string globalMyIP, in string globalid)
        {
            EnsureProfile();

            CurrentProfile.AccountName = accountName?.Trim() ?? "";
            CurrentProfile.DisplayName = string.IsNullOrWhiteSpace(CurrentProfile.AccountName) ? "Player" : CurrentProfile.AccountName;
            CurrentProfile.GlobalMyIP = globalMyIP?.Trim() ?? "";
            CurrentProfile.GlobalUserId = globalid?.Trim() ?? "";
            CurrentProfile.IsOnline = true;

            PlayerInfo.Id = CurrentProfile.GlobalUserId;
            PlayerInfo.Name = CurrentProfile.DisplayName;
            PlayerInfo.CurrentIp = CurrentProfile.GlobalMyIP;
            PlayerInfo.IsBot = false;
            PlayerInfo.Team = OpenGSCore.ETeam.NoTeam;
            PlayerInfo.IsReady = false;
        }

        public void Logout()
        {
            EnsureProfile();
            CurrentProfile.IsOnline = false;
        }

        /// <summary>
        /// Raised when a match takes the account up a level.
        /// <para>
        /// The server is what decides a level, and it says so on the result. The
        /// numbers it reports were being thrown away, so a player finished a match
        /// having gone up a level with nothing on screen saying so and nothing on
        /// the profile remembering it.
        /// </para>
        /// </summary>
        public event System.Action<int> LeveledUp;

        /// <summary>
        /// Takes the progress the server reports for this account.
        /// </summary>
        public void ApplyServerProgress(int level, long exp)
        {
            EnsureProfile();

            // A level below one is not a level, and an experience total that went
            // backwards would read as a level the player never lost.
            CurrentProfile.Level = System.Math.Max(1, level);
            CurrentProfile.Exp = System.Math.Max(0L, exp);
        }

        /// <summary>
        /// Takes the progress a finished match earned.
        /// <para>
        /// The reported total is adopted rather than the gain added, because a
        /// result that arrives twice must not credit the same match twice.
        /// </para>
        /// </summary>
        public void ApplyMatchProgress(int level, long totalExp, long xpGained)
        {
            EnsureProfile();

            var previousLevel = CurrentProfile.Level;
            ApplyServerProgress(level, totalExp);

            if (xpGained > 0)
            {
                Debug.Log($"[AccountManager] Earned {xpGained} EXP; level {CurrentProfile.Level}.");
            }

            // A gain that did not reach the next level is not a level up, and a
            // repeated result has already been adopted at the same level, so this
            // fires once per real level.
            if (CurrentProfile.Level > previousLevel)
            {
                LeveledUp?.Invoke(CurrentProfile.Level);
            }
        }

        public void SetCredits(long credits)
        {
            EnsureProfile();
            CurrentProfile.Credits = System.Math.Max(0L, credits);
        }

        public long GetCredits()
        {
            EnsureProfile();
            return CurrentProfile.Credits;
        }

        public void AddCredits(long amount)
        {
            EnsureProfile();
            if (amount <= 0)
            {
                return;
            }

            CurrentProfile.Credits = amount > long.MaxValue - CurrentProfile.Credits
                ? long.MaxValue
                : CurrentProfile.Credits + amount;
        }

        public bool SpendCredits(long amount)
        {
            EnsureProfile();
            if (amount <= 0)
            {
                return false;
            }

            if (CurrentProfile.Credits < amount)
            {
                return false;
            }

            CurrentProfile.Credits -= amount;
            return true;
        }

        private void EnsureProfile()
        {
            if (CurrentProfile == null)
            {
                CurrentProfile = new AccountProfile();
            }

            if (PlayerInfo == null)
            {
                PlayerInfo = new OpenGSCore.PlayerInfo();
            }
        }
    }
}
