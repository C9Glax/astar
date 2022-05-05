namespace Logging
{
    public class Logger
    {
        private LogType logType;
        private string logfilepath;
        private loglevel level;
        public Logger(LogType type, loglevel level)
        {
            this.logType = type;
            this.logfilepath = "";
            this.level = level;
        }

        public Logger(LogType type, loglevel level, string path)
        {
            this.logType = type;
            this.logfilepath = path;
            this.level = level;
        }

        public void log(loglevel type, string message, params object[] ?replace)
        {
            if(type >= this.level)
            {
                string header = string.Format("{0} {1} {2}: ", DateTime.Now.ToLocalTime().ToShortDateString(), DateTime.Now.ToLocalTime().ToLongTimeString(), type.ToString());
                switch (this.logType)
                {
                    case LogType.Console:
#pragma warning disable CS8604
                        Console.WriteLine(string.Format(header + message, replace));
#pragma warning restore CS8604
                        break;
                    case LogType.Logfile:
#pragma warning disable CS8604
                        File.WriteAllText(this.logfilepath, string.Format(header + message, replace));
#pragma warning restore CS8604
                        break;
                }
            }
        }
    }

    public enum LogType { Console, Logfile }
    public enum loglevel : ushort { DEBUG = 0, INFO = 1, ERROR = 2 };
}
