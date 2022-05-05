﻿using Logging;
using System.Xml;
using Graph;

namespace OpenStreetMap_Importer
{
    public class Importer
    {

        public static Dictionary<ulong, Node> Import(ref Logger logger)
        {
            List<Way> ways = new List<Way>();
            Dictionary<ulong, Node> nodes = new Dictionary<ulong, Node>();

            bool wayTag = false;
            Way currentWay = new Way();

            /*
             * First iteration
             * Import "ways" with a tag "highway" and add the node-ids to the list of nodes
             */
            XmlReaderSettings readerSettings = new XmlReaderSettings()
            {
                IgnoreWhitespace = true,
                IgnoreComments = true
            };
            XmlReader reader = XmlReader.Create(new MemoryStream(OSM_Data.map), readerSettings);
            reader.MoveToContent();
            while (reader.Read())
            {
                if (reader.Name == "way" && reader.IsStartElement())
                {
                    logger.Log(loglevel.VERBOSE, "WAY {0} nodes {1}", currentWay.highway.ToString(), currentWay.nodeIds.Count);
                    if (currentWay.highway != Way.highwayType.NONE)
                    {
                        ways.Add(currentWay);
                        foreach (ulong id in currentWay.nodeIds)
                        nodes.TryAdd(id, Node.nullnode);
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
                    logger.Log(loglevel.VERBOSE, "TAG {0} {1}", key, value);
#pragma warning restore CS8600
#pragma warning restore CS8604
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
                }
                else if(reader.Name == "nd" && wayTag)
                {
                    ulong id = Convert.ToUInt64(reader.GetAttribute("ref"));
                    currentWay.nodeIds.Add(id);
                }
                else if(reader.Name == "node")
                {
                    wayTag = false;
                }
            }

            logger.Log(loglevel.DEBUG, "Ways: {0} Nodes: {1}", ways.Count, nodes.Count);

            reader.Close();
            reader = XmlReader.Create(new MemoryStream(OSM_Data.map), readerSettings);
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
                    if (nodes.ContainsKey(id))
                    {
#pragma warning disable CS8602 //node will always have a lat and lon
                        float lat = Convert.ToSingle(reader.GetAttribute("lat").Replace('.', ','));
                        float lon = Convert.ToSingle(reader.GetAttribute("lon").Replace('.', ','));
#pragma warning restore CS8602
                        nodes[id] = new Node(lat, lon);
                        logger.Log(loglevel.VERBOSE, "NODE {0} {1} {2}", id, lat, lon);
                    }
                }
            }

            logger.Log(loglevel.INFO, "Import finished. Calculating distances.");

            /*
             * Add connections between nodes based on ways and calculate distance
             */
            ulong edges = 0;
            foreach(Way way in ways)
            {
                if (way.direction == Way.wayDirection.forward)
                {
                    for (int index = 0; index < way.nodeIds.Count - 1; index++)
                    {
                        Node currentNode = nodes[way.nodeIds[index]];
                        Node neighborNode = nodes[way.nodeIds[index + 1]];
                        double weight = Utils.DistanceBetweenNodes(currentNode, neighborNode);
                        currentNode.edges.Add(new Edge(neighborNode, weight));
                        logger.Log(loglevel.VERBOSE, "EDGE {0} -- {1} --> {2}", way.nodeIds[index], weight, way.nodeIds[index + 1]);
                        edges++;
                        if (!way.oneway)
                        {
                            neighborNode.edges.Add(new Edge(currentNode, weight));
                            edges++;
                        }
                    }
                }
                else
                {
                    for (int index = way.nodeIds.Count - 1; index > 1; index--)
                    {
                        Node currentNode = nodes[way.nodeIds[index]];
                        Node neighborNode = nodes[way.nodeIds[index - 1]];
                        double weight = Utils.DistanceBetweenNodes(currentNode, neighborNode);
                        currentNode.edges.Add(new Edge(neighborNode, weight));
                        logger.Log(loglevel.VERBOSE, "EDGE {0} -- {1} --> {2}", way.nodeIds[index], weight, way.nodeIds[index - 1]);
                        edges++;
                        if (!way.oneway)
                        {
                            neighborNode.edges.Add(new Edge(currentNode, weight));
                            edges++;
                        }
                    }
                }
            }
            reader.Close();

            logger.Log(loglevel.DEBUG, "Edges: {0}", edges);
            return nodes;
        }

        internal struct Way
        {
            public List<ulong> nodeIds;
            public bool oneway;
            public wayDirection direction;
            public highwayType highway;
            public enum wayDirection { forward, backward }
            public enum highwayType { NONE, motorway, trunk, primary, secondary, tertiary, unclassified, residential, motorway_link, trunk_link, primary_link, secondary_link, tertiary_link, living_street, service, pedestrian, track, bus_guideway, escape, raceway, road, busway, footway, bridleway, steps, corridor, path, cycleway, construction}
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
