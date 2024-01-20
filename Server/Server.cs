using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace MNet
{
    public class Server : NetworkBase
    {
        private const string ip = "0.0.0.0";
        private int port;
        private Dictionary<Socket, SocketClient> socketClients;
        public ServerMessageHandler serverMessageHandler;
        public bool IsStarted {get; set;}
        public Server(int port = 12345)
        {
            this.port = port;
            this.socketClients = new Dictionary<Socket, SocketClient>();
            serverMessageHandler = new ServerMessageHandler(this);
        }
        /// <summary>
        /// 开启服务器
        /// </summary>
        public void Start()
        {
            socket.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
            socket.Listen(10);
            logger.Log(LogType.INFO, $"服务器开始监听,{ip}:{port}");
            IsStarted = true;
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
                        }
                        catch(SocketException e)
                        {
                            //掉线
                            logger.Leave(socketClient.IP, socketClient.Port, e.Message);
                            socketClients.Remove(socket);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 给某个socket发送消息
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="message"></param>
        public void Send(Socket socket, Message message)
        {
            if(!socketClients.ContainsKey(socket))
            {
                logger.Log(LogType.ERROR, "客户端未连接");
                return;
            }
            SocketClient socketClient = socketClients[socket];
            try
            {
                SendMessage(socket, message);
            }
            catch(SocketException e)
            {
                //掉线
                logger.Leave(socketClient.IP, socketClient.Port, e.Message);
                socketClients.Remove(socket);
            }
        }
        /// <summary>
        /// 将消息广播给所有客户端
        /// </summary>
        /// <param name="message"></param>
        public void SendAll(Message message)
        {
            foreach(KeyValuePair<Socket, SocketClient> item in socketClients)
            {
                Send(item.Key, message);
            }
        }
        /// <summary>
        /// 停止服务器
        /// </summary>
        public void Stop()
        {
            if(IsStarted)
            {
                socket.Close();
                IsStarted = false;
                logger.Log(LogType.INFO, "服务器停止");
            }
        }

    }
}