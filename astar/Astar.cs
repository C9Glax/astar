using Logging;
using Graph;
using OpenStreetMap_Importer;

namespace astar
{
    public class Astar
    {
        private Logger logger;
        public Astar()
        {
            this.logger = new Logger(LogType.Console, loglevel.DEBUG);
            Dictionary<UInt64, Node> nodes = Importer.Import(logger);

        }
    }
}