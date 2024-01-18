using System.Net;
using System.Net.Sockets;

namespace MNet
{
    class Server : NetworkBase
    {
        private TcpListener tcpListener;
        private MessageHandler messageHandler;
        private List<TcpClient> tcpClients;
        public Server(string IP, int Port, MessageHandler messageHandler)
        {
            tcpListener = new TcpListener(IPAddress.Parse(IP), Port);
            this.messageHandler = messageHandler;
            this.tcpClients = new List<TcpClient>();
        }
        public void Start()
        {
            tcpListener.Start();
            //多路复用
            //..........
            //...........
        }
        public void Stop()
        {
            tcpListener.Stop();
        }
        public void InitialMessage()
        {

        }

    }
}