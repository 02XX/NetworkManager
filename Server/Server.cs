using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace MNet
{
    class Server : NetworkBase
    {
        private const string ip = "0.0.0.0";
        private int port = 12345;
        private MessageHandler messageHandler;
        private Dictionary<Socket, SocketClient> socketClients;
        public Server(int port, MessageHandler messageHandler)
        {
            this.port = port;
            this.messageHandler = messageHandler;
            this.socketClients = new Dictionary<Socket, SocketClient>();
        }
        public void Start()
        {
            socket.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
            socket.Listen(10);
            logger.Log(LogType.INFO, $"服务器开始监听，{ip}:{port}");
            List<Socket> checkRead = new List<Socket>();
            //多路复用
            while(true)
            {
                checkRead.Clear();
                checkRead.Add(socket);
                foreach(KeyValuePair<Socket, SocketClient> item in socketClients)
                {
                    checkRead.Add(item.Value.socket);
                }
                Socket.Select(checkRead, null, null, 1000); //1秒超时时间
                foreach(Socket socket in checkRead)
                {
                    if(socket == this.socket) // 处理新连接
                    {
                        //有新客户端连接
                        Socket clientSocket = this.socket.Accept();
                        IPEndPoint? iPEndPoint = clientSocket.RemoteEndPoint as IPEndPoint;
                        if(iPEndPoint == null)
                        {
                            //客户端立即断开了
                            logger.Flash();
                        }
                        else
                        {
                            string clientIP = iPEndPoint.Address.ToString();
                            int clientPort = iPEndPoint.Port;
                            SocketClient Client = new SocketClient(clientSocket, clientIP, clientPort);
                            socketClients.Add(clientSocket, Client);
                            logger.Join(clientIP, clientPort);
                        }
                        
                    }
                    else //处理消息
                    {
                        SocketClient socketClient = socketClients[socket];
                        try
                        {
                            Package package = ReceiveMessage(socket);
                            logger.Receive(socketClient.IP, socketClient.Port, JsonConvert.SerializeObject(package.message));
                        }
                        catch(SocketException)
                        {
                            //掉线
                            socketClients.Remove(socket);
                            logger.Leave(socketClient.IP, socketClient.Port);
                        }
                    }
                }
            }
        }
        public void Stop()
        {
            socket.Close();
        }
        public void InitialMessage()
        {

        }

    }
}