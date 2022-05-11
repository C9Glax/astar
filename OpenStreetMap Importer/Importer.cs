using Logging;
using System.Xml;
using Graph;

namespace OpenStreetMap_Importer
{
    public class Importer
    {

        public static Dictionary<ulong, Node> Import(string filePath = "", Logger ?logger = null)
        {
            List<Way> ways = new();
            Dictionary<ulong, Node> nodes = new();
            Stream mapData;
            XmlReaderSettings readerSettings = new()
            {
                IgnoreWhitespace = true,
                IgnoreComments = true
            };

            bool wayTag = false;
            Way currentWay = new();
            Dictionary<ulong, ushort> count = new();

            /*
             * First iteration
             * Import "ways" with a tag "highway"
             * Count occurances of "nodes" to find junctions
             */
            
            if (!File.Exists(filePath))
            {
                mapData = new MemoryStream(OSM_Data.map);
                logger?.Log(LogLevel.INFO, "Filepath '{0}' does not exist. Using standard file.", filePath);
            }
            else
            {
                mapData = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                logger?.Log(LogLevel.INFO, "Using file '{0}'", filePath);
            }
            XmlReader reader = XmlReader.Create(mapData, readerSettings);
            reader.MoveToContent();
            while (reader.Read())
            {
                if (reader.Name == "way" && reader.IsStartElement())
                {
                    logger?.Log(LogLevel.VERBOSE, "WAY {0} nodes {1}", currentWay.highway.ToString(), currentWay.nodeIds.Count);
                    if (currentWay.highway != Way.highwayType.NONE)
                    {
                        ways.Add(currentWay);
                        foreach (ulong id in currentWay.nodeIds)
                            if(count.ContainsKey(id))
                                count[id]++;
                            else
                                count.TryAdd(id, 1);
                    }
                    wayTag = true;
                    currentWay = new Way();
                }
                else if (reader.Name == "tag" && wayTag)
                {
#pragma warning disable CS8600 //tags will always have a value and key
#pragma warning disable CS8604
                    string value = reader.GetAttribute("v");
                    string key = reader.GetAttribute("k");
                    logger?.Log(LogLevel.VERBOSE, "TAG {0} {1}", key, value);
#pragma warning restore CS8600
                    switch (key)
                    {
                        case "highway":
                            currentWay.SetHighwayType(value);
                            break;
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
#pragma warning restore CS8604
                }
                else if(reader.Name == "nd" && wayTag)
                {
                    ulong id = Convert.ToUInt64(reader.GetAttribute("ref"));
                    currentWay.nodeIds.Add(id);
                    logger?.Log(LogLevel.VERBOSE, "nd: {0}", id);
                }
                else if(reader.Name == "node")
                {
                    wayTag = false;
                }
            }

            logger?.Log(LogLevel.DEBUG, "Ways: {0} Nodes: {1}", ways.Count, nodes.Count);

            reader.Close();
            if (File.Exists(filePath))
            {
                mapData.Close();
                mapData = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            }
            reader = XmlReader.Create(mapData, readerSettings);
            reader.MoveToContent();

            /*
             * Second iteration
             * Import nodes that are needed by the "ways"
             */
            while (reader.Read())
            {
                if (reader.Name == "node")
                {
                    ulong id = Convert.ToUInt64(reader.GetAttribute("id"));
                    if (count.ContainsKey(id))
                    {
#pragma warning disable CS8602 //node will always have a lat and lon
                        float lat = Convert.ToSingle(reader.GetAttribute("lat").Replace('.', ','));
                        float lon = Convert.ToSingle(reader.GetAttribute("lon").Replace('.', ','));
#pragma warning restore CS8602
                        nodes.TryAdd(id, new Node(lat, lon));
                        logger?.Log(LogLevel.VERBOSE, "NODE {0} {1} {2} {3}", id, lat, lon, count[id]);
                    }
                }
            }

            logger?.Log(LogLevel.INFO, "Import finished. Calculating distances.");

            /*
             * Add connections between nodes (only junctions, e.g. nodes are referenced more than once)
             * Remove non-junction nodes
             */
            ulong edges = 0;
            foreach(Way way in ways)
            {
                Node junction1 = nodes[way.nodeIds[0]];
                Node junction2;
                double weight = 0;
                //Iterate Node-ids in current way forwards or backwards (depending on way.direction)
                if (way.direction == Way.wayDirection.forward)
                {
                    for (int index = 0; index < way.nodeIds.Count - 1; index++)
                    {
                        Node currentNode = nodes[way.nodeIds[index]];
                        Node nextNode = nodes[way.nodeIds[index + 1]];
                        weight += Utils.DistanceBetweenNodes(currentNode, nextNode);
                        if (count[way.nodeIds[index + 1]] > 1 || index == way.nodeIds.Count - 2)
                        {
                            /*
                             * If Node is referenced more than once => Junction
                             * If Node is last node of way => Junction
                             * Add an edge between two junctions
                             */
                            junction2 = nodes[way.nodeIds[index + 1]];
                            junction1.edges.Add(new Edge(junction2, weight));
                            logger?.Log(LogLevel.VERBOSE, "EDGE {0} -- {1} --> {2}", way.nodeIds[index], weight, way.nodeIds[index + 1]);
                            edges++;

                            if (!way.oneway)
                            {
                                junction2.edges.Add(new Edge(junction1, weight));
                                logger?.Log(LogLevel.VERBOSE, "EDGE {0} -- {1} --> {2}", way.nodeIds[index + 1], weight, way.nodeIds[index]);
                                edges++;
                            }

                            junction1 = junction2;
                            weight = 0;
                        }
                    }
                }
                else
                {
                    for (int index = way.nodeIds.Count - 2; index > 0; index--)
                    {
                        Node currentNode = nodes[way.nodeIds[index]];
                        Node nextNode = nodes[way.nodeIds[index - 1]];
                        weight += Utils.DistanceBetweenNodes(currentNode, nextNode);
                        if (count[way.nodeIds[index - 1]] > 1 || index == 1)
                        {
                            /*
                             * If Node is referenced more than once => Junction
                             * If Node is last node of way => Junction
                             * Add an edge between two junctions
                             */
                            junction2 = nodes[way.nodeIds[index - 1]];
                            junction1.edges.Add(new Edge(junction2, weight));
                            logger?.Log(LogLevel.VERBOSE, "EDGE {0} -- {1} --> {2}", way.nodeIds[index], weight, way.nodeIds[index - 1]);
                            edges++;

                            if (!way.oneway)
                            {
                                junction2.edges.Add(new Edge(junction1, weight));
                                logger?.Log(LogLevel.VERBOSE, "EDGE {0} -- {1} --> {2}", way.nodeIds[index - 1], weight, way.nodeIds[index]);
                                edges++;
                            }

                            junction1 = junction2;
                            weight = 0;
                        }
                    }
                }
            }
            reader.Close();

            

            logger?.Log(LogLevel.DEBUG, "Edges: {0}", edges);
            return nodes.Where(node => count[node.Key] > 1).ToDictionary(node => node.Key, node => node.Value);
        }

        internal struct Way
        {
            public List<ulong> nodeIds;
            public bool oneway;
            public wayDirection direction;
            public highwayType highway;
            public enum wayDirection { forward, backward }
            public enum highwayType : uint
            {
                NONE = 1,
                motorway = 130,
                trunk = 125,
                primary = 110,
                secondary = 100,
                tertiary = 80,
                unclassified = 40,
                residential = 23,
                motorway_link = 55,
                trunk_link = 50,
                primary_link = 30,
                secondary_link = 25,
                tertiary_link = 24,
                living_street = 20,
                service = 14,
                pedestrian = 12,
                track = 6,
                bus_guideway = 15,
                escape = 3,
                raceway = 4,
                road = 28,
                busway = 13,
                footway = 8,
                bridleway = 7,
                steps = 5,
                corridor = 9,
                path = 10,
                cycleway = 11,
                construction = 2
            }


            public Way()
            {
                this.nodeIds = new List<ulong>();
                this.oneway = false;
                this.direction = wayDirection.forward;
                this.highway = highwayType.NONE;
            }

            public void SetHighwayType(string waytype)
            {
                try
                {
                    this.highway = (highwayType)Enum.Parse(typeof(highwayType), waytype, true);
                }catch(Exception)
                {
                    this.highway = highwayType.NONE;
                }
            }
        }
    }
}
