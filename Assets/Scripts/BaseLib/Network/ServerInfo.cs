




namespace OpenGS
{
    public class ServerInfo
    {
        public int Port { get; }

        public string Ip { get; }


        public ServerInfo(int port, in string ip)
        {
            Port = port;

            Ip = ip;

        }

    }
}
