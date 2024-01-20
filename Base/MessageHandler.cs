using System.Net.Sockets;
using Newtonsoft.Json;

namespace MNet
{
    public class MessageHandler
    {
        private NetworkBase networkBase;
        #region 心跳

        #endregion //心跳
        public MessageHandler(NetworkBase networkBase)
        {
            this.networkBase = networkBase;
        }
    }
}