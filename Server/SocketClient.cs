using System.Net.Sockets;

namespace MNet
{
    class SocketClient
    {
        public string IP {get; set;}
        public int Port {get; set;}
        public float LastPingTime {get; set;}
        public Socket socket;
        public SocketClient(Socket socket, string ip, int port)
        {
            this.socket = socket;
            this.IP = ip;
            this.Port = port;
            LastPingTime = 0f;
        }
    }
}