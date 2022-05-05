namespace Logging
{
    public class Logger
    {
        private LogType logType;
        private string logfilepath;
        public Logger(LogType type)
        {
            this.logType = type;
            this.logfilepath = "";
        }

        public Logger(LogType type, string path)
        {
            this.logType = type;
            this.logfilepath = path;
        }

        public void log(string message)
        {
            switch (this.logType)
            {
                case LogType.Console:
                    Console.WriteLine(message);
                    break;
                case LogType.Logfile:
                    File.WriteAllText(this.logfilepath, message);
                    break;
            }
        }
    }

    public enum LogType { Console, Logfile }
}
