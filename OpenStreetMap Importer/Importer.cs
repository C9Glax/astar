using Logging;
using System.Xml;
using Graph;

namespace OpenStreetMap_Importer
{
    public class Importer
    {

        private static XmlReaderSettings readerSettings = new()
        {
            IgnoreWhitespace = true,
            IgnoreComments = true
        };

        public static Dictionary<ulong, Node> Import(string filePath = "", Logger ?logger = null)
        {
            Dictionary<ulong, Node> nodes;

            /*
             * First iteration
             * Import "ways" with a tag "highway"
             * Count occurances of "nodes" to find junctions
             */

            Stream mapData = File.Exists(filePath) ? new FileStream(filePath, FileMode.Open, FileAccess.Read) : new MemoryStream(OSM_Data.map);
            Dictionary<ulong, ushort> occuranceCount = new();
            List<Way> ways = GetWays(mapData, ref occuranceCount, logger);
            mapData.Close();
            logger?.Log(LogLevel.DEBUG, "Loaded Ways: {0} Required Nodes: {1}", ways.Count, occuranceCount.Count);

            /*
             * Second iteration
             * Import nodes that are needed by the "ways"
             */
            mapData = File.Exists(filePath) ? new FileStream(filePath, FileMode.Open, FileAccess.Read) : new MemoryStream(OSM_Data.map);
            nodes = ImportNodes(mapData, occuranceCount, logger);
            mapData.Close();


            /*
             * Add connections between nodes (only junctions, e.g. nodes are referenced more than once)
             * Remove non-junction nodes
             */
            logger?.Log(LogLevel.INFO, "Calculating Edges and distances...");
            ulong edges = 0;
            foreach(Way way in ways)
            {
                Node junction1 = nodes[way.nodeIds[0]];
                Node junction2;
                float weight = 0;
                //Iterate Node-ids in current way forwards or backwards (depending on way.direction)
                if (way.IsForward())
                {
                    for (int index = 0; index < way.nodeIds.Count - 1; index++)
                    {
                        Node currentNode = nodes[way.nodeIds[index]];
                        Node nextNode = nodes[way.nodeIds[index + 1]];
                        weight += Utils.DistanceBetweenNodes(currentNode, nextNode);
                        if (occuranceCount[way.nodeIds[index + 1]] > 1 || index == way.nodeIds.Count - 2)
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

                            if (!way.IsOneWay())
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
                        if (occuranceCount[way.nodeIds[index - 1]] > 1 || index == 1)
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

                            if (!way.IsOneWay())
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

            logger?.Log(LogLevel.DEBUG, "Loaded Edges: {0}", edges);
            return nodes.Where(node => occuranceCount[node.Key] > 1).ToDictionary(node => node.Key, node => node.Value);
        }

        private static List<Way> GetWays(Stream mapData, ref Dictionary<ulong, ushort> occuranceCount, Logger? logger = null)
        {
            List<Way> _ways = new();
            bool _isWay = false;
            Way _currentWay = new();

            XmlReader _reader = XmlReader.Create(mapData, readerSettings);
            _reader.MoveToContent();

            logger?.Log(LogLevel.INFO, "Importing ways and counting nodes...");
            while (_reader.Read())
            {
                if (_reader.Name == "way" && _reader.IsStartElement())
                {
                    logger?.Log(LogLevel.VERBOSE, "WAY {0} nodes {1}", _currentWay.GetHighwayType().ToString(), _currentWay.nodeIds.Count);
                    if (_currentWay.GetHighwayType() != Way.type.NONE)
                    {
                        _ways.Add(_currentWay);
                        foreach (ulong id in _currentWay.nodeIds)
                            if (occuranceCount.ContainsKey(id))
                                occuranceCount[id]++;
                            else
                                occuranceCount.TryAdd(id, 1);
                    }
                    _isWay = true;
                    _currentWay = new Way();
                }
                else if (_reader.Name == "tag" && _isWay)
                {
#pragma warning disable CS8600 //tags will always have a value and key
#pragma warning disable CS8604
                    string value = _reader.GetAttribute("v");
                    string key = _reader.GetAttribute("k");
                    logger?.Log(LogLevel.VERBOSE, "TAG {0} {1}", key, value);
#pragma warning restore CS8600
                    _currentWay.AddTag(key, value);
#pragma warning restore CS8604
                }
                else if (_reader.Name == "nd" && _isWay)
                {
                    ulong id = Convert.ToUInt64(_reader.GetAttribute("ref"));
                    _currentWay.nodeIds.Add(id);
                    logger?.Log(LogLevel.VERBOSE, "nd: {0}", id);
                }
                else if (_reader.Name == "node")
                {
                    _isWay = false;
                }
            }

            _reader.Close();
            GC.Collect();
            return _ways;
        }

        private static Dictionary<ulong, Node> ImportNodes(Stream mapData, Dictionary<ulong, ushort> occuranceCount, Logger? logger = null)
        {
            Dictionary<ulong, Node> nodes = new();
            XmlReader reader = XmlReader.Create(mapData, readerSettings);
            reader.MoveToContent();

            logger?.Log(LogLevel.INFO, "Importing nodes...");
            while (reader.Read())
            {
                if (reader.Name == "node")
                {
                    ulong id = Convert.ToUInt64(reader.GetAttribute("id"));
                    if (occuranceCount.ContainsKey(id))
                    {
#pragma warning disable CS8602 //node will always have a lat and lon
                        float lat = Convert.ToSingle(reader.GetAttribute("lat").Replace('.', ','));
                        float lon = Convert.ToSingle(reader.GetAttribute("lon").Replace('.', ','));
#pragma warning restore CS8602
                        nodes.TryAdd(id, new Node(lat, lon));
                        logger?.Log(LogLevel.VERBOSE, "NODE {0} {1} {2} {3}", id, lat, lon, occuranceCount[id]);
                    }
                }
            }
            reader.Close();
            GC.Collect();
            return nodes;
        }

        internal struct Way
        {
            public List<ulong> nodeIds;
            private Dictionary<string, object> tags;


            public Dictionary<type, int> speed = new() {
                { type.NONE, 1 },
                { type.motorway, 130 },
                { type.trunk, 125 },
                { type.primary, 110 },
                { type.secondary, 100 },
                { type.tertiary, 90 },
                { type.unclassified, 40 },
                { type.residential, 20 },
                { type.motorway_link, 50 },
                { type.trunk_link, 50 },
                { type.primary_link, 30 },
                { type.secondary_link, 25 },
                { type.tertiary_link, 25 },
                { type.living_street, 20 },
                { type.service, 10 },
                { type.pedestrian, 10 },
                { type.track, 1 },
                { type.bus_guideway, 5 },
                { type.escape, 1 },
                { type.raceway, 1 },
                { type.road, 25 },
                { type.busway, 5 },
                { type.footway, 1 },
                { type.bridleway, 1 },
                { type.steps, 1 },
                { type.corridor, 1 },
                { type.path, 10 },
                { type.cycleway, 5 },
                { type.construction, 1 }
            };
            public enum type { NONE, motorway, trunk, primary, secondary, tertiary, unclassified, residential, motorway_link, trunk_link, primary_link, secondary_link, tertiary_link, living_street, service, pedestrian, track, bus_guideway, escape, raceway, road, busway, footway, bridleway, steps, corridor, path, cycleway, construction }


            public Way()
            {
                this.nodeIds = new List<ulong>();
                this.tags = new();
            }
            public void AddTag(string key, string value, Logger? logger = null)
            {
                switch (key)
                {
                    case "highway":
                        try
                        {
                            this.tags.Add(key, (type)Enum.Parse(typeof(type), value, true));
                            if (this.GetMaxSpeed().Equals((int)type.NONE))
                            {
                                this.tags["maxspeed"] = (int)this.GetHighwayType();
                            }
                        }
                        catch (ArgumentException)
                        {
                            this.tags.Add(key, type.NONE);
                        }
                        break;
                    case "maxspeed":
                        try
                        {
                            if (this.tags.ContainsKey("maxspeed"))
                                this.tags["maxspeed"] = Convert.ToInt32(value);
                            else
                                this.tags.Add(key, Convert.ToInt32(value));
                        }
                        catch (FormatException)
                        {
                            this.tags.Add(key, (int)this.GetHighwayType());
                        }
                        break;
                    case "oneway":
                        switch (value)
                        {
                            case "yes":
                                this.tags.Add(key, true);
                                break;
                            case "-1":
                                this.tags.Add("forward", false);
                                break;
                            case "no":
                                this.tags.Add(key, false);
                                break;
                        }
                        break;
                    default:
                        logger?.Log(LogLevel.VERBOSE, "Tag {0} - {1} was not added.", key, value);
                        break;
                }
            }

            public type GetHighwayType()
            {
                return this.tags.ContainsKey("highway") ? (type)this.tags["highway"] : type.NONE;
            }

            public bool IsOneWay()
            {
                return this.tags.ContainsKey("oneway") ? (bool)this.tags["oneway"] : false;
            }

            public int GetMaxSpeed()
            {
                return this.tags.ContainsKey("maxspeed") ? (int)this.tags["maxspeed"] : (int)this.GetHighwayType();

            }

            public bool IsForward()
            {
                return this.tags.ContainsKey("forward") ? (bool)this.tags["forward"] : true;
            }
        }
    }
}
