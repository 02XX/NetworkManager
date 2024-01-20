namespace MNet
{
    public class MsgBroadcast : Message
    {
        public string message = "你好";
        public MsgBroadcast() { }
        public MsgBroadcast(string msg)
        {
            this.message = msg;
        }
    };
}