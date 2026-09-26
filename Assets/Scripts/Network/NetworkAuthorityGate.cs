namespace OpenGS
{
    /// <summary>
    /// Single place that decides whether the client talks to the authoritative
    /// server or to the in-process local simulation.
    /// <para>
    /// The S1 milestone asks for the authoritative path to be the only source
    /// of truth for lobby state. Previously every caller asked
    /// DebugSettingsManager.settings.localServerTestMode on its own, so it was
    /// not obvious from a call site whether a request went to the backend or
    /// to a local stub. Going through this gate keeps the distinction visible
    /// and gives one place to assert on it.
    /// </para>
    /// </summary>
    public static class NetworkAuthorityGate
    {
        /// <summary>
        /// True when the client should use the in-process local simulation.
        /// </summary>
        public static bool IsLocalSimulation()
        {
            DebugSettingsManager.EnsureLoaded();
            var settings = DebugSettingsManager.settings;
            return settings != null && settings.localServerTestMode;
        }

        /// <summary>
        /// True when the client should use the authoritative server.
        /// </summary>
        public static bool IsAuthoritative() => !IsLocalSimulation();

        /// <summary>
        /// Local TCP port to bind or connect to, or 0 when there is no usable
        /// local configuration.
        /// </summary>
        public static int LocalTcpPort()
        {
            DebugSettingsManager.EnsureLoaded();
            var settings = DebugSettingsManager.settings;
            if (settings == null || !settings.localServerTestMode)
            {
                return 0;
            }

            return settings.localTCPPort;
        }

        /// <summary>
        /// Local UDP port, or 0 when there is no usable local configuration.
        /// </summary>
        public static int LocalUdpPort()
        {
            DebugSettingsManager.EnsureLoaded();
            var settings = DebugSettingsManager.settings;
            if (settings == null || !settings.localServerTestMode)
            {
                return 0;
            }

            return settings.localUDPPort;
        }
    }
}