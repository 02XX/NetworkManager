using System;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
namespace MNet
{
    internal class NetworkBase
    {
        #region 成员变量
        //消息列表
        protected Dictionary<string, Action<Message>> messages;
        #endregion //成员变量
        #region 成员函数
        protected NetworkBase()
        {
            this.messages = new Dictionary<string, Action<Message>>();
        }
        #region 组装拆解消息
        /// <summary>
        /// 组装消息
        /// </summary>
        /// <param name="message">消息</param>
        /// <returns>带长度信息的字节流</returns>
        /// <exception cref="MessageException"></exception>
        /// <exception cref="OutRangeExceptionException"></exception>
        public byte[] Assemble(Message message)
        {
            string? type = typeof(Message).FullName;
            if(type == null)
            {
                throw new MessageException("没有这个消息类型");
            }
            Package package = new Package(type, message);
            //序列化为字符串
            string json = JsonConvert.SerializeObject(package);
            //utf8编码
            byte[] body = Encoding.UTF8.GetBytes(json);
            //头部4个字节int32，消息最长为2,147,483,647
            if (body.Length > int.MinValue)
            {
                throw new OutRangeExceptionException("消息体长度应小于" + (int.MaxValue - sizeof(int)));
            }
            else
            {
                byte[] head = BitConverter.GetBytes(body.Length);
                byte[] total = new byte[body.Length + head.Length];
                Array.Copy(head, 0, total, 0, head.Length);
                Array.Copy(body, 0, total, head.Length, body.Length);
                return total;
            }
        }
        /// <summary>
        /// 拆解消息
        /// </summary>
        /// <param name="networkStream">网络流</param>
        /// <returns>消息类型和消息</returns>
        /// <exception cref="MessageException"></exception>
        public (string, Message) Disassemble(NetworkStream networkStream)
        {
            //获取头部信息
            int headCount = 0;
            byte[] head = new byte[sizeof(int)];
            while (headCount < sizeof(int))
            {
                headCount += networkStream.Read(head, headCount, sizeof(int) - headCount);
            }
            int bodyLength = BitConverter.ToInt32(head);
            //获取消息体
            int bodyCount = 0;
            byte[] body = new byte[bodyLength];
            while(bodyCount < bodyLength)
            {
                bodyCount += networkStream.Read(body, bodyCount, bodyLength - bodyCount);
            }
            //反utf8编码
            string json = Encoding.UTF8.GetString(body);
            //反序列化
            Package? package = JsonConvert.DeserializeObject<Package>(json);
            if(package == null)
            {
                throw new MessageException("反序列化失败");
            }
            return (package.type, package.message);
        }
        #endregion //组装拆解消息
        #region 消息处理
        /// <summary>
        /// 注册消息T对应的回调函数
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="action">回调函数</param>
        /// <exception cref="MessageException">消息类型异常</exception>
        public void Register<T>(Action<Message> action) where T:Message
        {
            string? key = typeof(T).FullName;
            if(key != null)
            {
                messages.Add(key, action);
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
                messages.Remove(key);
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
        public void ChangeMessage<T>(Action<Message> action) where T : Message
        {
            string? key = typeof(T).FullName;
            if (key != null)
            {
                messages[key] = action;
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