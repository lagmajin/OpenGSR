using System;

namespace OpenGSCore
{
    public class MultipleKey
    {
        private string? ID { get; set; }
        private string? IP { get; set; }

        public MultipleKey(in string? id, in string? ip)
        {
            // These used to be dropped on the floor, which left HasValidKey
            // answering false for every key ever built and made every lookup
            // built on one of them fail.
            ID = id;
            IP = ip;
        }

        public bool HasValidKey()
        {
            return !string.IsNullOrWhiteSpace(ID) && !string.IsNullOrWhiteSpace(IP);
        }

        public string? Id() => ID;

        public string? Ip() => IP;
    }
}
