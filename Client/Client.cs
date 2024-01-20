using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
namespace MNet
{
    public class Client : NetworkBase
    {
        public string IP {get; set;}
        public int Port {get;set;}
        public bool IsConnected {get; private set;}
        public Client(string ip, int port)
        {
            this.IP = ip;
            this.Port = port;
            this.IsConnected = false;
        }
        /// <summary>
        /// 连接到服务器
        /// </summary>
        public void Connect()
        {
            if(!IsConnected)
            {
                socket.Connect(IPAddress.Parse(IP), Port);
                IsConnected = true;
            }
        }
        private void ConnectAsync()
        {
            if(!IsConnected)
            {
                socket.BeginConnect(IPAddress.Parse(IP), Port, CallConnect, socket);
            }
        }
        /// <summary>
        /// 断开与服务器的连接
        /// </summary>
        public void Disconnect()
        {
            if(IsConnected)
            {
                socket.Close();
                IsConnected = false;
            }
        }
        /// <summary>
        /// 发送消息给服务器
        /// </summary>
        /// <param name="message"></param>
        public void Send(Message message)
        {
            if(IsConnected)
            {
                base.SendMessage(socket, message);
            }
        }
        private void CallConnect(IAsyncResult asyncResult)
        {
            try
            {
                socket.EndConnect(asyncResult);
                IsConnected = true;
            }
            catch (SocketException e)
            {
                logger.Log(LogType.ERROR, "连接服务器失败: " + e.ToString());
            }
        }
        /// <summary>
        /// 消息更新，注意：需要将该函数放在循环里面，一直更新消息
        /// </summary>
        public void UpdateMessage()
        {
            if(IsConnected)
            {
                try
                {
                    Package package = ReceiveMessage(socket);
                }
                catch (SocketException)
                {
                    //服务器掉线
                    logger.Log(LogType.INFO, "服务器断开");
                    IsConnected = false;
                }
            }
        }

        // public void UpdateMessageAsync()
        // {
        //     if (IsConnected)
        //     {
        //         try
        //         {
        //             ReceiveTask(socket);
        //         }
        //         catch (SocketException)
        //         {
        //             //服务器掉线
        //             logger.Log(LogType.INFO, "服务器断开");
        //             IsConnected = false;
        //         }
        //     }
        // }
    }
}