using Logging;
using System.Xml;

namespace astar
{
    public class Importer
    {
        public static Dictionary<UInt64, Node> Import(Logger logger)
        {
            XmlReader reader = XmlReader.Create(new MemoryStream(osm_data.map));
            reader.MoveToContent();
            Dictionary<UInt64, Node> nodes = new Dictionary<UInt64, Node>();

            nodeType currentNodeType = nodeType.NULL;
            Node nullNode = new Node();
            Node currentNode = nullNode;
            Way currentWay = new Way();

            while (reader.Read())
            {
                if (reader.Name == "node" && reader.IsStartElement())
                {
                    currentNodeType = nodeType.NODE;
                    nodes.Add(
                        Convert.ToUInt64(reader.GetAttribute("id")),
                        new Node(
                            Convert.ToSingle(reader.GetAttribute("lat")),
                            Convert.ToSingle(reader.GetAttribute("lon"))
                            )
                        );
                }
                else if (reader.Name == "way")
                {
                    if(currentNodeType == nodeType.WAY)
                    {
                        ImportWay(currentWay);
                        logger.log(loglevel.INFO, "Way nodes: {0}", currentWay.nodes.Count);
                    }
                    currentNodeType = nodeType.WAY;
                    currentNode = nullNode;
                    currentWay = new Way();
                    reader.GetAttribute("id");
                }else if (reader.Name == "nd" && currentNodeType == nodeType.WAY){
                    UInt64 id = Convert.ToUInt64(reader.GetAttribute("ref"));
                    if (!nodes.TryGetValue(id, out currentNode))
                    {
                        logger.log(loglevel.DEBUG, "Node with id {0} not imported.", id);
                    }
                    else
                    {
                        currentWay.nodes.Add(currentNode);
                    }
                }else if (reader.Name == "tag")
                {
                    if(currentNodeType == nodeType.WAY)
                    {
                        string value = reader.GetAttribute("v");
                        switch (reader.GetAttribute("k"))
                        {
                            /*case "highway":

                                break;*/
                            case "oneway":
                                switch (value)
                                {
                                    case "yes":
                                        currentWay.oneway = true;
                                        break;
                                    /*case "no":
                                        currentWay.oneway = false;
                                        break;*/
                                    case "-1":
                                        currentWay.oneway = true;
                                        currentWay.direction = Way.wayDirection.backward;
                                        break;
                                }
                                break;
                            /*case "name":

                                break;*/
                        }
                    }
                }
            }

            logger.log(loglevel.INFO, "Loaded. Nodes: {0}", nodes.Count);
            return nodes;
        }

        internal static void ImportWay(Way way)
        {

            if (way.direction == Way.wayDirection.forward)
            {
                for(int index = 0; index < way.nodes.Count - 1; index++)
                {
                    way.nodes[index].edges.Add(new Edge(way.nodes[index + 1]));
                    if (!way.oneway)
                        way.nodes[index+1].edges.Add(new Edge(way.nodes[index]));
                }
            }
            else
            {
                for (int index = way.nodes.Count-1; index > 1; index--)
                {
                    way.nodes[index].edges.Add(new Edge(way.nodes[index - 1]));
                    if (!way.oneway)
                        way.nodes[index - 1].edges.Add(new Edge(way.nodes[index]));
                }
            }
        }


        internal enum nodeType { NODE, WAY, NULL }

        internal struct Way
        {
            public List<Node> nodes;
            public bool oneway;
            public wayDirection direction;
            public enum wayDirection { forward, backward }
            public Way()
            {
                this.nodes = new List<Node>();
                this.oneway = false;
                this.direction = wayDirection.forward;
            }
        }
    }
}
