using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
namespace MNet
{
    struct AsyncState
    {
        public byte[] head;
        public byte[] body;
        public int headCount;
        public int bodyCount;
        public int headLength;
        public int bodyLength;
        public Socket socket;
    }
    public class NetworkBase
    {
        #region 成员变量
        protected Socket socket;
        public Logger logger;
        private JsonSerializerSettings setting;
        public MessageHandler MessageHandler { get; set; }
        //消息列表
        protected Dictionary<string, Action<Socket, Message>> messages;
        #endregion //成员变量
        #region 成员函数
        protected NetworkBase()
        {
            logger = new Logger();
            setting = new JsonSerializerSettings();
            setting.Converters.Add(new MessageConverter());
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.messages = new Dictionary<string, Action<Socket, Message>>();
            this.MessageHandler = new MessageHandler(this);
        }
        #region 组装拆解消息
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="socket">需要发送消息的socket</param>
        /// <param name="message">消息</param>
        /// <exception cref="MessageException"></exception>
        /// <exception cref="OutRangeExceptionException"></exception>
        /// /// <exception cref="SocketException"></exception>
        protected void SendMessage(Socket socket, Message message)
        {
            IPEndPoint? iPEndPoint = socket.RemoteEndPoint as IPEndPoint;
            string ip = iPEndPoint?.Address.ToString() ?? "UnknownIP";
            int port = iPEndPoint?.Port ?? -1;
            string? type = message.GetType().FullName;
            if(type == null)
            {
                throw new MessageException("没有这个消息类型");
            }
            Package package = new Package(type, message);
            //序列化为字符串
            string json = JsonConvert.SerializeObject(package, setting);
            //utf8编码
            byte[] body = Encoding.UTF8.GetBytes(json);
            //头部4个字节int32，消息最长为2,147,483,647
            if (body.Length > int.MaxValue)
            {
                throw new OutRangeExceptionException("消息体长度应小于" + (int.MaxValue - sizeof(int))+"当前长度"+ body.Length);
            }
            else
            {
                byte[] head = BitConverter.GetBytes(body.Length);
                byte[] total = new byte[body.Length + head.Length];
                Array.Copy(head, 0, total, 0, head.Length);
                Array.Copy(body, 0, total, head.Length, body.Length);
                logger.Send(ip, port, json);
                socket.Send(total);
            }
        }
        /// <summary>
        /// 接受消息
        /// </summary>
        /// <param name="socket">需要接受消息的socket</param>
        /// <exception cref="MessageException"></exception>
        /// <exception cref="SocketException">socket掉线</exception>
        protected Package ReceiveMessage(Socket socket)
        {
            IPEndPoint? iPEndPoint = socket.RemoteEndPoint as IPEndPoint;
            string ip = iPEndPoint?.Address.ToString() ?? "UnknownIP";
            int port = iPEndPoint?.Port ?? -1;
            //获取头部信息
            int headCount = 0;
            byte[] head = new byte[sizeof(int)];
            while (headCount < sizeof(int))
            {
                headCount += socket.Receive(head, headCount, sizeof(int) - headCount, SocketFlags.None);
            }
            int bodyLength = BitConverter.ToInt32(head);
            //获取消息体
            int bodyCount = 0;
            byte[] body = new byte[bodyLength];
            while(bodyCount < bodyLength)
            {
                bodyCount += socket.Receive(body, bodyCount, bodyLength - bodyCount, SocketFlags.None);
            }
            //反utf8编码
            string json = Encoding.UTF8.GetString(body);
            //反序列化
            Package? package = JsonConvert.DeserializeObject<Package>(json, setting);
            if(package == null)
            {
                throw new MessageException("反序列化失败");
            }
            if(messages.ContainsKey(package.type))
            {
                logger.Receive(ip, port, json);
                messages[package.type]?.Invoke(socket, package.message);
            }
            else
            {
                logger.Log(LogType.ERROR, "不能识别的消息类型"+ package.type);
            }
            return package;
        }
        /// <summary>
        /// 非阻塞接受数据
        /// </summary>
        /// <param name="socket"></param>
        protected void ReceiveTask(Socket socket)
        {
            byte[] head = new byte[sizeof(int)];
            socket.BeginReceive(head, 0, sizeof(int) - 0, SocketFlags.None,CallReceive, new AsyncState{head = head, headCount = 0, headLength = sizeof(int),body = [], bodyCount = 0,bodyLength = -1, socket = socket});
        }
        private void CallReceive(IAsyncResult asyncResult)
        {
            if(asyncResult.AsyncState == null)
            {
                throw new MessageException("回调为空");
            }
            AsyncState state = (AsyncState)asyncResult.AsyncState;
            //判断是组装头部还是身体
            if(state.bodyLength == -1)
            {
                //组装头部
                state.headCount += state.socket.EndReceive(asyncResult);
                if(state.headCount < state.headLength)
                {
                    //继续接受
                    state.socket.BeginReceive(state.head, state.headCount, state.headLength, SocketFlags.None, CallReceive, new AsyncState { head = state.head, headCount = state.headCount, headLength = state.headLength, body = [], bodyCount = 0, bodyLength = -1, socket = state.socket});
                }
                else
                {
                    //头部接受完毕
                    state.bodyLength = BitConverter.ToInt32(state.head);
                    byte[] body = new byte[state.bodyLength];
                    //开始接受身体
                    state.socket.BeginReceive(body, 0, state.bodyLength, SocketFlags.None, CallReceive, new AsyncState {head=[], headCount = 0, headLength = -1, body = body, bodyCount = 0, bodyLength = state.bodyLength, socket = state.socket});
                }
            }
            else
            {
                //组装身体
                state.bodyCount += state.socket.EndReceive(asyncResult);
                if(state.bodyCount < state.bodyLength)
                {
                    //继续组装
                    state.socket.BeginReceive(state.body, state.bodyCount, state.bodyLength, SocketFlags.None, CallReceive, new AsyncState { head = state.head, headCount = state.headCount, headLength = state.headLength, body = state.body, bodyCount = state.bodyCount, bodyLength = state.bodyLength, socket = state.socket });
                }
                else
                {
                    //接受完毕
                    //反utf8编码
                    string json = Encoding.UTF8.GetString(state.body);
                    //反序列化
                    Package? package = JsonConvert.DeserializeObject<Package>(json);
                    if (package == null)
                    {
                        throw new MessageException("反序列化失败");
                    }
                    if (messages.ContainsKey(package.type))
                    {
                        messages[package.type]?.Invoke(socket, package.message);
                    }
                    else
                    {
                        logger.Log(LogType.ERROR, "不能识别的消息类型" + package.type);
                    }
                }
            }
        }
        
        #endregion //组装拆解消息
        #region 消息处理
        /// <summary>
        /// 注册消息T对应的回调函数
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="action">回调函数</param>
        /// <exception cref="MessageException">消息类型异常</exception>
        public void RegisterMessage<T>(Action<Socket, Message> action) where T:Message
        {
            string? key = typeof(T).FullName;
            if(key != null)
            {
                if(!messages.ContainsKey(key))
                {
                    messages.Add(key, action);
                    logger.Log(LogType.INFO, "注册消息类型" + key);
                }
                else
                {
                    logger.Log(LogType.INFO, key+"注册失败,消息类型已经注册"+messages[key].GetType().FullName);
                }
            }
            else
            {
                throw new MessageException("未定义的消息类型");
            }
        }
        /// <summary>
        /// 取消消息T对应的回调函数
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <exception cref="MessageException">消息类型异常</exception>
        public void RemoveMessage<T>() where T : Message
        {
            string? key = typeof(T).FullName;
            if (key != null)
            {
                if(messages.ContainsKey(key))
                {
                    messages.Remove(key);
                    logger.Log(LogType.INFO, "删除消息类型" + key);
                }
                else
                {
                    logger.Log(LogType.INFO, "删除失败,"+key+"未被注册");
                }
            }
            else
            {
                throw new MessageException("未定义的消息类型");
            }
        }
        /// <summary>
        /// 更改消息T对应的回调函数
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="action">回调函数</param>
        /// <exception cref="MessageException">消息类型异常</exception>
        public void ChangeMessage<T>(Action<Socket, Message> action) where T : Message
        {
            string? key = typeof(T).FullName;
            if (key != null)
            {
                if (messages.ContainsKey(key))
                {
                    messages[key] = action;
                    logger.Log(LogType.INFO, "修改消息类型" + key);
                }
                else
                {
                    logger.Log(LogType.INFO, "修改失败," + key + "未被注册");
                }
            }
            else
            {
                throw new MessageException("未定义的消息类型");
            }
        }
        #endregion //消息处理
        #endregion //成员函数
    }
}