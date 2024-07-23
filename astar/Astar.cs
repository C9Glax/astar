using astar.PathingHelper;
using Graph;
using Microsoft.Extensions.Logging;
using Graph.Utils;
using OSM_Regions;

namespace astar
{
    public static class Astar
    {
        public static Route FindPath(float startLat, float startLon, float endLat, float endLon, float regionSize, bool car = true, string? importFolderPath = null,
            ILogger? logger = null)
        {
            RegionLoader rl = new(regionSize, importFolderPath, logger: logger);
            Graph graph = Spiral(rl, startLat, startLon, regionSize);
            KeyValuePair<ulong, Node> startNode = graph.ClosestNodeToCoordinates(startLat, startLon, car);
            startNode.Value.PreviousIsFromStart = true;
            startNode.Value.PreviousNodeId = startNode.Key;
            startNode.Value.Distance = 0f;
            
            Graph endRegion = Spiral(rl, endLat, endLon, regionSize);
            graph.ConcatGraph(endRegion);
            KeyValuePair<ulong, Node> endNode = graph.ClosestNodeToCoordinates(endLat, endLon, car);
            endNode.Value.PreviousIsFromStart = false;
            endNode.Value.PreviousNodeId = endNode.Key;
            endNode.Value.Distance = 0f;

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
                ulong currentNodeStartId = toVisitStart.Dequeue();
                Node currentNodeStart = graph.Nodes[currentNodeStartId];
                foreach ((ulong neighborId, ulong wayId) in currentNodeStart.Neighbors)
                {
                    if (!graph.ContainsNode(neighborId))
                        graph.ConcatGraph(Graph.FromGraph(rl.LoadRegionFromNodeId(neighborId)));
                    if (!graph.ContainsWay(wayId))
                    {
                        foreach (global::Graph.Graph? g in rl.LoadRegionsFromWayId(wayId))
                            graph.ConcatGraph(Graph.FromGraph(g));
                    }

                    Way way = graph.Ways[wayId];
                    byte speed = SpeedHelper.GetSpeed(way, car);
                    if(speed < 1)
                        continue;
                    Node neighborNode = graph.Nodes[neighborId];
                    
                    if (neighborNode.PreviousIsFromStart is false)//Check if we found the opposite End
                        return PathFound(graph, currentNodeStart, neighborNode, logger);
                    
                    float distance = (currentNodeStart.Distance??float.MaxValue) + (float)currentNodeStart.DistanceTo(neighborNode);
                    if (neighborNode.PreviousNodeId is null || neighborNode.Distance > distance && currentNodeStart.PreviousNodeId != neighborId)
                    {
                        neighborNode.PreviousNodeId = currentNodeStartId;
                        neighborNode.Distance = distance;
                        neighborNode.PreviousIsFromStart = true;
                        toVisitStart.Enqueue(neighborId, NodeUtils.DistanceBetween(neighborNode, endNode.Value) / speed);
                    }
                    logger?.LogTrace($"Neighbor {neighborId} {neighborNode}");
                }
                
                ulong currentNodeEndId = toVisitEnd.Dequeue();
                Node currentNodeEnd = graph.Nodes[currentNodeEndId];
                foreach ((ulong neighborId, ulong wayId) in currentNodeEnd.Neighbors)
                {
                    if (!graph.ContainsNode(neighborId))
                        graph.ConcatGraph(Graph.FromGraph(rl.LoadRegionFromNodeId(neighborId)));
                    if (!graph.ContainsWay(wayId))
                    {
                        foreach (global::Graph.Graph? g in rl.LoadRegionsFromWayId(wayId))
                            graph.ConcatGraph(Graph.FromGraph(g));
                    }
                    
                    Way way = graph.Ways[wayId];
                    byte speed = SpeedHelper.GetSpeed(way, car);
                    if(speed < 1)
                        continue;
                    Node neighborNode = graph.Nodes[neighborId];
                    
                    if (neighborNode.PreviousIsFromStart is true)//Check if we found the opposite End
                        return PathFound(graph, neighborNode, currentNodeEnd, logger);
                    
                    float distance = (currentNodeStart.Distance??float.MaxValue) + (float)currentNodeStart.DistanceTo(neighborNode);
                    if (neighborNode.PreviousNodeId is null || neighborNode.Distance > distance)
                    {
                        neighborNode.PreviousNodeId = currentNodeEndId;
                        neighborNode.Distance = distance;
                        neighborNode.PreviousIsFromStart = false;
                        toVisitEnd.Enqueue(neighborId, NodeUtils.DistanceBetween(neighborNode, startNode.Value) / speed);
                    }
                    logger?.LogTrace($"Neighbor {neighborId} {neighborNode}");
                }
                logger?.LogDebug($"Distance {currentNodeStart.DistanceTo(currentNodeEnd):000000.00}m toVisit-Queues: {toVisitStart.Count} {toVisitEnd.Count}");

            }
            return new Route(graph, Array.Empty<Step>().ToList(), false);
        }

        private static Route PathFound(Graph graph, Node fromStart, Node fromEnd, ILogger? logger = null)
        {
            logger?.LogInformation("Path found!");
            List<Step> path = new();
            path.Add(new Step((float)NodeUtils.DistanceBetween(fromStart, fromEnd), fromStart, fromEnd));
            Node current = fromStart;
            while (current.Distance != 0f)
            {
                Step step = new((float)NodeUtils.DistanceBetween(graph.Nodes[(ulong)current.PreviousNodeId!], current), graph.Nodes[(ulong)current.PreviousNodeId!], current);
                path.Add(step);
                current = graph.Nodes[(ulong)current.PreviousNodeId!];
            }
            path.Reverse();//Since we go from the middle backwards until here
            current = fromEnd;
            while (current.Distance != 0f)
            {
                Step step = new((float)NodeUtils.DistanceBetween(graph.Nodes[(ulong)current.PreviousNodeId!], current), current, graph.Nodes[(ulong)current.PreviousNodeId!]);
                path.Add(step);
                current = graph.Nodes[(ulong)current.PreviousNodeId!];
            }

            Route r = new (graph, path, true);
            logger?.LogInformation(r.ToString());
            return r;
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