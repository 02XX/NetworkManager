namespace MNet
{   
    internal enum LogType
    {
        INFO,//提示
        ERROR,//错误
        WARNING,//警告
    }
    internal class Logger
    {
        private string filePath;

        public Logger(string filePath = "./Log.log")
        {
            this.filePath = filePath;
        }
        public void Log(LogType logType ,string message)
        {
            using (StreamWriter streamWriter = new StreamWriter(this.filePath, true))
            {
                streamWriter.WriteLine($"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")} - {logType}: {message}");
            }
            switch (logType)
            {
                case LogType.ERROR:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogType.WARNING:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                default:
                    break;
            }
            Console.WriteLine($"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")} - [{logType}] {message}");
            Console.ResetColor();
        }
        public void Join(string ip, int port)
        {
            Log(LogType.INFO, $"{ip}:{port}加入");
        }
        public void Leave(string ip, int port)
        {
            Log(LogType.INFO, $"{ip}:{port}离开");
        }
        public void Flash()
        {
            Log(LogType.INFO, $"一个客户端加入后又立即离开了");
        }
        public void Receive(string ip, int port, string message)
        {
            Log(LogType.INFO, $"接收到{ip}:{port}: {message}");
        }
        public void Send(string ip, int port, string message)
        {
            Log(LogType.INFO, $"发送给{ip}:{port}: {message}");
        }
    }
}