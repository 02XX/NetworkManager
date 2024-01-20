namespace MNet
{
    public class SerializerException : System.Exception
    {
        public SerializerException() { }
        public SerializerException(string message) : base(message) { }
        public SerializerException(string message, System.Exception inner) : base(message, inner) { }

    }
}