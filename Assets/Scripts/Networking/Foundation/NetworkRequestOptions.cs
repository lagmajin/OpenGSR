namespace OpenGS
{
    public struct NetworkRequestOptions
    {
        public int TimeoutMs;
        public int RetryCount;

        public static NetworkRequestOptions Default => new NetworkRequestOptions
        {
            TimeoutMs = 3000,
            RetryCount = 0
        };
    }
}
