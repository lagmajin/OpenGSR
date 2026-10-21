using System;
using Newtonsoft.Json.Linq;

namespace OpenGS
{
    /// <summary>
    /// How a mission went, as the server said it went.
    /// <para>
    /// This was an empty class, so a mission had nowhere to keep what it had
    /// achieved and a result screen had nothing to read. The numbers here are the
    /// ones the mission evaluator sends, so what a player is shown is what the
    /// server decided rather than what this client worked out.
    /// </para>
    /// </summary>
    public class MissionScore
    {
        /// <summary>Whether the mission was completed.</summary>
        public bool Success { get; private set; }

        /// <summary>How it ended: Clear, Fail or TimeUp.</summary>
        public string Result { get; private set; } = string.Empty;

        public int LifeRemaining { get; private set; }
        public int Score { get; private set; }
        public int Kills { get; private set; }
        public int Deaths { get; private set; }

        /// <summary>How much of the objective was done, and how much was asked for.</summary>
        public int ObjectiveProgress { get; private set; }

        public int ObjectiveCount { get; private set; }

        /// <summary>
        /// Reads a mission result the server sent.
        /// <para>
        /// A result with no numbers in it is a result that is not a mission result,
        /// and guessing at the missing ones would put a number on a screen that was
        /// never told one. It returns nothing instead.
        /// </para>
        /// </summary>
        public static MissionScore? FromMissionResult(JObject json)
        {
            if (json == null)
            {
                return null;
            }

            var result = json["Result"]?.ToString() ?? string.Empty;
            var resultType = json["ResultType"]?.ToString() ?? string.Empty;
            var isMission = result.Length > 0
                ? string.Equals(resultType, "Mission", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(result, "Clear", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(result, "TimeUp", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(result, "Fail", StringComparison.OrdinalIgnoreCase)
                : json["Success"] != null;

            if (!isMission)
            {
                return null;
            }

            return new MissionScore
            {
                Success = json["Success"]?.ToObject<bool>() ?? false,
                Result = result,
                LifeRemaining = ReadInt(json, "LifeRemaining"),
                Score = ReadInt(json, "Score"),
                Kills = ReadInt(json, "Kills"),
                Deaths = ReadInt(json, "Deaths"),
                ObjectiveProgress = ReadInt(json, "ObjectiveProgress"),
                ObjectiveCount = ReadInt(json, "ObjectiveCount")
            };
        }

        private static int ReadInt(JObject json, string key)
        {
            var value = json[key]?.ToObject<int>() ?? 0;
            return value < 0 ? 0 : value;
        }
    }
}