using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
namespace MNet
{
    class Client : NetworkBase
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
        public void Connect()
        {
            socket.BeginConnect(IPAddress.Parse(IP), Port, CallConnect, socket);
        }
        public void Disconnected()
        {
            if(IsConnected)
                socket.Close();
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
        public void UpdateMessage()
        {
            try
            {
                Package package = ReceiveMessage(socket);
                logger.Receive(IP, Port, JsonConvert.SerializeObject(package.message));
            }
            catch (SocketException)
            {
                //服务器掉线
                logger.Log(LogType.INFO, "服务器断开");
            }
        }
    }
}