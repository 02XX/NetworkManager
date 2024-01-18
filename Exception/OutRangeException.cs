namespace MNet
{
    public class OutRangeExceptionException : System.Exception
    {
        public OutRangeExceptionException() { }
        public OutRangeExceptionException(string message) : base(message) { }
        public OutRangeExceptionException(string message, System.Exception inner) : base(message, inner) { }

    }
}
