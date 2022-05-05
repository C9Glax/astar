using Logging;
using System.Xml;
using Graph;

namespace OpenStreetMap_Importer
{
    public class Importer
    {
        public static Dictionary<UInt64, Node> Import(Logger logger)
        {
            XmlReader reader = XmlReader.Create(new MemoryStream(OSM_Data.map));
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
                    if(currentNodeType == nodeType.WAY && currentWay.nodes.Count > 1)
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
                    Node currentNode = way.nodes[index];
                    Node neighborNode = way.nodes[index + 1];
                    ushort weight = Convert.ToUInt16(DistanceBetweenNodes(currentNode, neighborNode));
                    currentNode.edges.Add(new Edge(neighborNode, weight));
                    if (!way.oneway)
                        neighborNode.edges.Add(new Edge(currentNode, weight));
                }
            }
            else
            {
                for (int index = way.nodes.Count-1; index > 1; index--)
                {
                    Node currentNode = way.nodes[index];
                    Node neighborNode = way.nodes[index - 1];
                    ushort weight = Convert.ToUInt16(DistanceBetweenNodes(currentNode, neighborNode));
                    currentNode.edges.Add(new Edge(neighborNode, weight));
                    if (!way.oneway)
                        neighborNode.edges.Add(new Edge(currentNode, weight));
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

        private static double DistanceBetweenNodes(Node n1, Node n2)
        {
            return DistanceBetweenCoordinates(n1.lat, n1.lon, n2.lat, n2.lon);
        }

        private static double DistanceBetweenCoordinates(float lat1, float lon1, float lat2, float lon2)
        {
            const int earthRadius = 6371;
            double differenceLat = DegreesToRadians(lat2 - lat1);
            double differenceLon = DegreesToRadians(lon2 - lon1);

            double lat1Rads = DegreesToRadians(lat1);
            double lat2Rads = DegreesToRadians(lat2);

            double a = Math.Sin(differenceLat / 2) * Math.Sin(differenceLat / 2) + Math.Sin(differenceLon / 2) * Math.Sin(differenceLon / 2) * Math.Cos(lat1Rads) * Math.Cos(lat2Rads);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadius * c;
        }

        private static double DegreesToRadians(double deg)
        {
            return deg * Math.PI / 180.0;
        }

        private static double RadiansToDegrees(double rad)
        {
            return rad * 180.0 / Math.PI;
        }
    }
}
