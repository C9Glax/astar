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

        public static Dictionary<ulong, Node> Import(string filePath = "", bool onlyJunctions = true, Logger? logger = null)
        {
            /*
             * Count Node occurances when tag is "highway"
             */
            logger?.Log(LogLevel.INFO, "Counting Node-Occurances...");
            Stream mapData = File.Exists(filePath) ? new FileStream(filePath, FileMode.Open, FileAccess.Read) : new MemoryStream(OSM_Data.map);
            Dictionary<ulong, ushort> occuranceCount = CountNodeOccurances(mapData, logger);
            mapData.Close();
            logger?.Log(LogLevel.DEBUG, "Way Nodes: {0}", occuranceCount.Count);

            /*
             * Import Nodes and Edges
             */
            logger?.Log(LogLevel.INFO, "Importing Graph...");
            mapData = File.Exists(filePath) ? new FileStream(filePath, FileMode.Open, FileAccess.Read) : new MemoryStream(OSM_Data.map);
            Dictionary<ulong, Node> graph = CreateGraph(mapData, occuranceCount, onlyJunctions, logger);
            mapData.Close();
            occuranceCount.Clear();
            GC.Collect();
            logger?.Log(LogLevel.DEBUG, "Loaded Nodes: {0}", graph.Count);

            return graph;
        }

        private static Dictionary<ulong, ushort> CountNodeOccurances(Stream mapData, Logger? logger = null)
        {
            Dictionary<ulong, ushort> _occurances = new();

            XmlReader _reader = XmlReader.Create(mapData, readerSettings);
            XmlReader _wayReader;
            _reader.MoveToContent();

            bool _isHighway;
            List<ulong> _currentIds = new();

            while (_reader.ReadToFollowing("way"))
            {
                _isHighway = false;
                _currentIds.Clear();
                _wayReader = _reader.ReadSubtree();
                while (_wayReader.Read())
                {
                    if (_reader.Name == "tag" && _reader.GetAttribute("k").Equals("highway"))
                    {
                        try
                        {
                            if (!Enum.Parse(typeof(Way.type), _reader.GetAttribute("v"), true).Equals(Way.type.NONE))
                                _isHighway = true;
                        }
                        catch (ArgumentException) { };
                    }
                    else if(_reader.Name == "nd")
                    {
                        try
                        {
                            _currentIds.Add(Convert.ToUInt64(_reader.GetAttribute("ref")));
                        }
                        catch (FormatException) { };
                    }
                }
                if (_isHighway)
                {
                    foreach(ulong _id in _currentIds)
                    {
                        if (!_occurances.TryAdd(_id, 1))
                            _occurances[_id]++;
                    }
                }
                _wayReader.Close();
            }
            _reader.Close();
            GC.Collect();

            return _occurances;
        }

        private static Dictionary<ulong, Node> CreateGraph(Stream mapData, Dictionary<ulong, ushort> occuranceCount, bool onlyJunctions, Logger? logger = null)
        {
            Dictionary<ulong, Node> _tempAll = new();
            Dictionary<ulong, Node> _graph = new();

            XmlReader _reader = XmlReader.Create(mapData, readerSettings);
            XmlReader _wayReader;
            _reader.MoveToContent();

            while (_reader.Read())
            {
                if(_reader.Name == "node")
                {
                    ulong id = Convert.ToUInt64(_reader.GetAttribute("id"));
                    if (occuranceCount.ContainsKey(id))
                    {
#pragma warning disable CS8602 //node will always have a lat and lon
                        float lat = Convert.ToSingle(_reader.GetAttribute("lat").Replace('.', ','));
                        float lon = Convert.ToSingle(_reader.GetAttribute("lon").Replace('.', ','));
#pragma warning restore CS8602
                        _tempAll.Add(id, new Node(lat, lon));
                        logger?.Log(LogLevel.VERBOSE, "NODE {0} {1} {2} {3}", id, lat, lon, occuranceCount[id]);
                    }
                }
                else if(_reader.Name == "way")
                {
                    _wayReader = _reader.ReadSubtree();
                    Way _currentWay = new();
                    while (_wayReader.Read())
                    {
                        _wayReader = _reader.ReadSubtree();
                        while (_wayReader.Read())
                        {
                            if (_reader.Name == "tag")
                            {
#pragma warning disable CS8600 //tags will always have a value and key
#pragma warning disable CS8604
                                string _value = _reader.GetAttribute("v");
                                string _key = _reader.GetAttribute("k");
                                logger?.Log(LogLevel.VERBOSE, "TAG {0} {1}", _key, _value);
                                _currentWay.AddTag(_key, _value);
#pragma warning restore CS8600
#pragma warning restore CS8604
                            }
                            else if (_reader.Name == "nd")
                            {
                                ulong _id = Convert.ToUInt64(_reader.GetAttribute("ref"));
                                _currentWay.nodeIds.Add(_id);
                                logger?.Log(LogLevel.VERBOSE, "nd: {0}", _id);
                            }
                        }
                    }
                    _wayReader.Close();

                    if (!_currentWay.GetHighwayType().Equals(Way.type.NONE))
                    {
                        logger?.Log(LogLevel.VERBOSE, "WAY Nodes: {0} Type: {1}", _currentWay.nodeIds.Count, _currentWay.GetHighwayType());
                        if (!onlyJunctions)
                        {
                            for (int _nodeIdIndex = 0; _nodeIdIndex < _currentWay.nodeIds.Count - 1; _nodeIdIndex++)
                            {
                                Node _n1 = _tempAll[_currentWay.nodeIds[_nodeIdIndex]];
                                Node _n2 = _tempAll[_currentWay.nodeIds[_nodeIdIndex + 1]];
                                float _weight = Utils.DistanceBetweenNodes(_n1, _n2) / _currentWay.GetMaxSpeed();
                                if (!_currentWay.IsOneWay())
                                {
                                    _n1.edges.Add(new Edge(_n2, _weight));
                                    _n2.edges.Add(new Edge(_n1, _weight));
                                }
                                else if (_currentWay.IsForward())
                                {
                                    _n1.edges.Add(new Edge(_n2, _weight));
                                }
                                else
                                {
                                    _n2.edges.Add(new Edge(_n1, _weight));
                                }
                            }
                            _graph = _tempAll;
                        }
                        else
                        {
                            Node _junctionStart, _nextNode;
                            if (!_tempAll.TryGetValue(_currentWay.nodeIds[0], out _junctionStart))
                            {
                                _junctionStart = _graph[_currentWay.nodeIds[0]];
                            }
                            else
                            {
                                _graph.TryAdd(_currentWay.nodeIds[0], _junctionStart);
                                _tempAll.Remove(_currentWay.nodeIds[0]);
                            }
                            Node _currentNode = _junctionStart;
                            float _distance = 0, _weight;
                            for(int _nodeIdIndex = 0; _nodeIdIndex < _currentWay.nodeIds.Count - 1; _nodeIdIndex++)
                            {
                                if (!_tempAll.TryGetValue(_currentWay.nodeIds[_nodeIdIndex + 1], out _nextNode))
                                    _nextNode = _graph[_currentWay.nodeIds[_nodeIdIndex + 1]];
                                _distance += Utils.DistanceBetweenNodes(_currentNode, _nextNode);
                                if (occuranceCount[_currentWay.nodeIds[_nodeIdIndex]] > 1 || _nodeIdIndex == _currentWay.nodeIds.Count - 2) //junction found
                                {
                                    _weight = _distance / _currentWay.GetMaxSpeed();
                                    _distance = 0;
                                    _graph.TryAdd(_currentWay.nodeIds[_nodeIdIndex + 1], _nextNode);
                                    if (!_currentWay.IsOneWay())
                                    {
                                        _junctionStart.edges.Add(new Edge(_nextNode, _weight));
                                        _nextNode.edges.Add(new Edge(_junctionStart, _weight));
                                    }
                                    else if (_currentWay.IsForward())
                                    {
                                        _junctionStart.edges.Add(new Edge(_nextNode, _weight));
                                    }
                                    else
                                    {
                                        _nextNode.edges.Add(new Edge(_junctionStart, _weight));
                                    }
                                }
                                else
                                {
                                    _tempAll.Remove(_currentWay.nodeIds[_nodeIdIndex]);
                                }
                                _currentNode = _nextNode;
                            }
                        }
                    }
                }
            }
            _reader.Close();
            GC.Collect();
            return _graph;
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
