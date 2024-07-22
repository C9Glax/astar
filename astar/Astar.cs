using Microsoft.Extensions.Logging;
using Graph.Utils;
using OSM_Regions;

namespace astar
{
    public static class Astar
    {
        public static Route FindPath(float startLat, float startLon, float endLat, float endLon, float regionSize, string? importFolderPath = null,
            ILogger? logger = null)
        {
            RegionLoader rl = new(regionSize, importFolderPath, logger: logger);
            Graph graph = Spiral(rl, startLat, startLon, regionSize);
            KeyValuePair<ulong, Node> startNode = graph.ClosestNodeToCoordinates(startLat, startLon);
            startNode.Value.PreviousIsFromStart = true;
            startNode.Value.Previous = new KeyValuePair<ulong, float>(startNode.Key, 0f);
            
            Graph endRegion = Spiral(rl, endLat, endLon, regionSize);
            graph.ConcatGraph(endRegion);
            KeyValuePair<ulong, Node> endNode = graph.ClosestNodeToCoordinates(endLat, endLon);
            endNode.Value.PreviousIsFromStart = false;
            endNode.Value.Previous = new KeyValuePair<ulong, float>(endNode.Key, 0f);

            logger?.Log(LogLevel.Information,
                "From {0:00.00000}#{1:000.00000} to {2:00.00000}#{3:000.00000} Great-Circle {4:00000.00}km",
                startNode.Value.Lat, startNode.Value.Lon, endNode.Value.Lat, endNode.Value.Lon,
                NodeUtils.DistanceBetween(startNode.Value, endNode.Value) / 1000);

            PriorityQueue<ulong, double> toVisitStart = new();
            toVisitStart.Enqueue(startNode.Key, NodeUtils.DistanceBetween(startNode.Value, endNode.Value));
            PriorityQueue<ulong, double> toVisitEnd = new();
            toVisitEnd.Enqueue(endNode.Key, NodeUtils.DistanceBetween(endNode.Value, startNode.Value));

            while (toVisitStart.Count > 0 && toVisitEnd.Count > 0)
            {
                logger?.LogDebug($"Length toVisit-Start: {toVisitStart.Count} -End: {toVisitEnd.Count}");
                /*
                 * FROM START
                 */
                ulong currentNodeStartId = toVisitStart.Dequeue();
                Node? currentNodeStart;
                if (!graph.ContainsNode(currentNodeStartId))
                {
                    Graph? newRegion = Graph.FromGraph(rl.LoadRegionFromNodeId(currentNodeStartId));
                    if (newRegion is null)
                    {
                        logger?.LogError($"Could not load Region for Node {currentNodeStartId}");
                        currentNodeStart = null;
                    }
                    else
                    {
                        graph.ConcatGraph(newRegion);
                        currentNodeStart = graph.Nodes[currentNodeStartId];
                    }
                }
                else
                    currentNodeStart = graph.Nodes[currentNodeStartId];
                logger?.LogTrace($"Current Node Start: {currentNodeStartId} {currentNodeStart}");
                if (currentNodeStart is not null)
                {
                    foreach ((ulong nodeId, ulong wayId) in currentNodeStart.Neighbors)
                    {
                        //TODO checks for way-stuff
                        Node? neighbor;
                        if (!graph.ContainsNode(nodeId))
                        {
                            Graph? newRegion = Graph.FromGraph(rl.LoadRegionFromNodeId(nodeId));
                            if (newRegion is null)
                            {
                                logger?.LogError($"Could not load Region for Node {nodeId}");
                                neighbor = null;
                            }
                            else
                            {
                                graph.ConcatGraph(newRegion);
                                neighbor = Node.FromGraphNode(graph.Nodes[nodeId]);
                            }
                        }
                        else
                        {
                            neighbor = graph.Nodes[nodeId];
                        }

                        if (neighbor is not null)
                        {
                            /*
                             * IMPORTANT SHIT BELOW
                             */
                            if (neighbor.PreviousIsFromStart is false)//Check if we found the opposite End
                                return PathFound(graph, currentNodeStart, neighbor);
                            float distance = currentNodeStart.Previous!.Value.Value + (float)neighbor.DistanceTo(currentNodeStart);
                            if (neighbor.Previous is null || neighbor.Previous.Value.Value > distance)
                            {
                                neighbor.Previous = new KeyValuePair<ulong, float>(currentNodeStartId, distance);
                                neighbor.PreviousIsFromStart = true;
                                toVisitStart.Enqueue(nodeId, NodeUtils.DistanceBetween(neighbor, endNode.Value));
                            }
                            logger?.LogTrace($"Neighbor {nodeId} {neighbor}");
                        }
                    }
                }
                
                /*
                 * FROM END
                 */
                ulong currentNodeEndId = toVisitEnd.Dequeue();
                Node? currentNodeEnd;
                if (!graph.ContainsNode(currentNodeEndId))
                {
                    Graph? newRegion = Graph.FromGraph(rl.LoadRegionFromNodeId(currentNodeEndId));
                    if (newRegion is null)
                    {
                        logger?.LogError($"Could not load Region for Node {currentNodeEndId}");
                        currentNodeEnd = null;
                    }
                    else
                    {
                        graph.ConcatGraph(newRegion);
                        currentNodeEnd = graph.Nodes[currentNodeEndId];
                    }
                }
                else
                    currentNodeEnd = graph.Nodes[currentNodeEndId];
                logger?.LogTrace($"Current Node End: {currentNodeEndId} {currentNodeEnd}");

                if (currentNodeEnd is not null)
                {
                    foreach ((ulong nodeId, ulong wayId) in currentNodeEnd.Neighbors)
                    {
                        //TODO checks for way-stuff
                        Node? neighbor;
                        if (!graph.ContainsNode(nodeId))
                        {
                            Graph? newRegion = Graph.FromGraph(rl.LoadRegionFromNodeId(nodeId));
                            if (newRegion is null)
                            {
                                logger?.LogError($"Could not load Region for Node {nodeId}");
                                neighbor = null;
                            }
                            else
                            {
                                graph.ConcatGraph(newRegion);
                                neighbor = Node.FromGraphNode(graph.Nodes[nodeId]);
                            }
                        }
                        else
                        {
                            neighbor = graph.Nodes[nodeId];
                        }

                        if (neighbor is not null)
                        {
                            /*
                             * IMPORTANT SHIT BELOW
                             */
                            if (neighbor.PreviousIsFromStart is true)//Check if we found the opposite End
                                return PathFound(graph, neighbor, currentNodeEnd);
                            
                            float distance = currentNodeEnd.Previous!.Value.Value + (float)neighbor.DistanceTo(currentNodeEnd);
                            if (neighbor.Previous is null || neighbor.Previous.Value.Value > distance)
                            {
                                neighbor.Previous = new KeyValuePair<ulong, float>(currentNodeEndId, distance);
                                neighbor.PreviousIsFromStart = false;
                                toVisitEnd.Enqueue(nodeId, NodeUtils.DistanceBetween(neighbor, startNode.Value));
                            }
                            logger?.LogTrace($"Neighbor {nodeId} {neighbor}");
                        }
                    }
                }
            }
            return new Route(graph, Array.Empty<Step>().ToList(), false);
        }

        private static Route PathFound(Graph graph, Node fromStart, Node fromEnd)
        {
            List<Step> path = new();
            Node current = fromStart;
            while (current.Previous is not null && current.Previous.Value.Value == 0f)
            {
                Step step = new((float)NodeUtils.DistanceBetween(graph.Nodes[current.Previous.Value.Key], current), graph.Nodes[current.Previous.Value.Key], current);
                path.Add(step);
                current = graph.Nodes[current.Previous.Value.Key];
            }
            path.Reverse();//Since we go from the middle backwards until here
            path.Add(new Step((float)NodeUtils.DistanceBetween(fromStart, fromEnd), fromStart, fromEnd));
            current = fromEnd;
            while (current.Previous is not null && current.Previous.Value.Value == 0f)
            {
                Step step = new((float)NodeUtils.DistanceBetween(graph.Nodes[current.Previous.Value.Key], current), current, graph.Nodes[current.Previous.Value.Key]);
                path.Add(step);
                current = graph.Nodes[current.Previous.Value.Key];
            }

            return new Route(graph, path, true);
        }

        private static Graph Spiral(RegionLoader loader, float lat, float lon, float regionSize)
        {
            Graph? ret = Graph.FromGraph(loader.LoadRegionFromCoordinates(lat, lon));
            int iteration = 1;
            while (ret is null)
            {
                for (int x = -iteration; x <= iteration; x++)
                {
                    Graph? g1 = Graph.FromGraph(loader.LoadRegionFromCoordinates(lat + x * regionSize, lon - iteration * regionSize));
                    Graph? g2 = Graph.FromGraph(loader.LoadRegionFromCoordinates(lat + x * regionSize, lon + iteration * regionSize));
                    if (ret is not null)
                    {
                        ret.ConcatGraph(g1);
                        ret.ConcatGraph(g2);
                    }
                    else if (ret is null && g1 is not null)
                    {
                        ret = g1;
                        ret.ConcatGraph(g2);
                    }else if (ret is null && g2 is not null)
                        ret = g2;
                }
                for (int y = -iteration + 1; y < iteration; y++)
                {
                    Graph? g1 = Graph.FromGraph(loader.LoadRegionFromCoordinates(lat - iteration * regionSize, lon + y * regionSize));
                    Graph? g2 = Graph.FromGraph(loader.LoadRegionFromCoordinates(lat + iteration * regionSize, lon + y * regionSize));
                    if (ret is not null)
                    {
                        ret.ConcatGraph(g1);
                        ret.ConcatGraph(g2);
                    }
                    else if (ret is null && g1 is not null)
                    {
                        ret = g1;
                        ret.ConcatGraph(g2);
                    }else if (ret is null && g2 is not null)
                        ret = g2;
                }
                iteration++;
            }
            return ret;
        }
    }
}