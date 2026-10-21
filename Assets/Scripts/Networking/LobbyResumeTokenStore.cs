using System;
using Newtonsoft.Json;

namespace OpenGS
{
    /// <summary>
    /// The token a client reconnects to the lobby with, instead of its password.
    /// <para>
    /// The password belongs to the login screen and is deliberately never kept, so
    /// a client that has connected once has nothing to authenticate the next
    /// connection with. The server issues this token when a login succeeds and
    /// hands back a fresh one on every reconnect.
    /// </para>
    /// <para>
    /// It is stored beside the other saved client state rather than in
    /// PlayerPrefs, so it is not written in plaintext to a registry entry. It is
    /// dropped whenever the server refuses a login, because a refused token is no
    /// use and keeping it would make every later connection fail the same way.
    /// </para>
    /// </summary>
    public static class LobbyResumeTokenStore
    {
        private const string SaveFileName = "lobby_resume_token.json";

        [Serializable]
        private class Payload
        {
            public string Token = string.Empty;
            public string AccountId = string.Empty;
        }

        /// <summary>
        /// The token held for an account, or an empty string when there is none.
        /// <para>
        /// The account is checked as well as the token. A token is only good for
        /// the account it was issued to, so one left behind by a different account
        /// must not be sent as though it were this one's.
        /// </para>
        /// </summary>
        public static string TokenFor(string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return string.Empty;
            }

            if (!JsonStorage.TryLoad(SaveFileName, out Payload payload) || payload == null)
            {
                return string.Empty;
            }

            if (!string.Equals(payload.AccountId, accountId, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return payload.Token ?? string.Empty;
        }

        public static void Save(string accountId, string token)
        {
            if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            JsonStorage.Save(SaveFileName, new Payload { AccountId = accountId, Token = token });
        }

        /// <summary>
        /// Throws the token away. Used on logout, and whenever the server has
        /// already refused the one that was held.
        /// </summary>
        public static void Clear()
        {
            JsonStorage.Delete(SaveFileName);
        }
    }
}
