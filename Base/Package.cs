namespace MNet
{
    internal class Package
    {
        public string type;
        public Message message;
        public Package(string type, Message message)
        {
            this.type = type;
            this.message = message;
        }
    }
}