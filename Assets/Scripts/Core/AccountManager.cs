using OpenGSCore;

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
