using System.Xml;
using Logging;

namespace astar
{
    public class Astar
    {
        private Logger logger;
        public Astar()
        {
            this.logger = new Logger(LogType.Console);
            XmlReader reader = XmlReader.Create(new MemoryStream(osm_data.map));
            reader.MoveToContent();
            while (reader.Read())
                logger.log(reader.Name);
        }
    }
}