using System.Net.Sockets;
using Newtonsoft.Json;

namespace MNet
{
    public class ServerMessageHandler
    {
        private Server server;
        #region 心跳

        #endregion //心跳
        public ServerMessageHandler(Server server)
        {
            this.server = server;
        }
    }
}