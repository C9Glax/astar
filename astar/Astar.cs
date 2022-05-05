
using Logging;

namespace astar
{
    public class Astar
    {
        private Logger logger;
        public Astar()
        {
            this.logger = new Logger(LogType.Console);
            Importer.Import(logger);
        }
    }
}