
using Logging;

namespace astar
{
    public class Astar
    {
        private Logger logger;
        public Astar()
        {
            this.logger = new Logger(LogType.Console, loglevel.DEBUG);
            Importer.Import(logger);
        }
    }
}