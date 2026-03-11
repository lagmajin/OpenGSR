using System;

namespace OpenGS
{
    public class NetworkRequestException : Exception
    {
        public string ErrorCode { get; }

        public NetworkRequestException(string message, string errorCode = null) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
