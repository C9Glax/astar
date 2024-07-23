using astar.PathingHelper;
using Microsoft.Extensions.Logging;
using Graph.Utils;
using OSM_Graph.Enums;
using OSM_Regions;

namespace astar
{
    public static class Astar
    {
        public static Route FindPath(float startLat, float startLon, float endLat, float endLon, float regionSize, bool car = true, PathMeasure pathing = PathMeasure.Distance, string? importFolderPath = null,
            ILogger? logger = null)
        {
            RegionLoader rl = new(regionSize, importFolderPath, logger: logger);
            Graph graph = Spiral(rl, startLat, startLon, regionSize);
            Graph endRegion = Spiral(rl, endLat, endLon, regionSize);
            graph.ConcatGraph(endRegion);
            KeyValuePair<ulong, Node> startNode = graph.ClosestNodeToCoordinates(startLat, startLon, car);
            startNode.Value.PreviousIsFromStart = true;
            startNode.Value.PreviousNodeId = startNode.Key;
            startNode.Value.Metric = 0f;
            
            KeyValuePair<ulong, Node> endNode = graph.ClosestNodeToCoordinates(endLat, endLon, car);
            endNode.Value.PreviousIsFromStart = false;
            endNode.Value.PreviousNodeId = endNode.Key;
            endNode.Value.Metric = 0f;

            double totalDistance = NodeUtils.DistanceBetween(startNode.Value, endNode.Value);
            PriorityHelper priorityHelper = new(totalDistance, SpeedHelper.GetMaxSpeed(car));

            logger?.Log(LogLevel.Information,
                "From {0:00.00000}#{1:000.00000} to {2:00.00000}#{3:000.00000} Great-Circle {4:00000.00}km",
                startNode.Value.Lat, startNode.Value.Lon, endNode.Value.Lat, endNode.Value.Lon, totalDistance / 1000);

            PriorityQueue<ulong, int> toVisitStart = new();
            toVisitStart.Enqueue(startNode.Key, 0);
            PriorityQueue<ulong, int> toVisitEnd = new();
            toVisitEnd.Enqueue(endNode.Key, 0);

            while (toVisitStart.Count > 0 && toVisitEnd.Count > 0)
            {
                Route? route = null;
                if (toVisitStart.Count >= toVisitEnd.Count && route is null)
                {
                    for(int i = 0; i < toVisitStart.Count / 10 && route is null; i++)
                        route = ExploreSide(true, graph, toVisitStart, rl, priorityHelper, endNode.Value, car, pathing, logger);
                }
                if(route is null)
                    route = ExploreSide(true, graph, toVisitStart, rl, priorityHelper, endNode.Value, car, pathing, logger);
                
                if (toVisitEnd.Count >= toVisitStart.Count && route is null)
                {
                    for(int i = 0; i < toVisitEnd.Count / 10 && route is null; i++)
                        route = ExploreSide(false, graph, toVisitEnd, rl, priorityHelper, startNode.Value, car, pathing, logger);
                }
                if(route is null)
                    route = ExploreSide(false, graph, toVisitEnd, rl, priorityHelper, startNode.Value, car, pathing, logger);

                if (route is not null)
                    return route;
                logger?.LogDebug($"toVisit-Queues: {toVisitStart.Count} {toVisitStart.UnorderedItems.MinBy(i => i.Priority).Priority} {toVisitEnd.Count} {toVisitEnd.UnorderedItems.MinBy(i => i.Priority).Priority}");
            }
            return new Route(graph, Array.Empty<Step>().ToList(), false);
        }

        private static Route? ExploreSide(bool fromStart, Graph graph, PriorityQueue<ulong, int> toVisit, RegionLoader rl, PriorityHelper priorityHelper, Node goalNode, bool car, PathMeasure pathing = PathMeasure.Distance,  ILogger? logger = null)
        {
            ulong currentNodeId = toVisit.Dequeue();
            Node currentNode = graph.Nodes[currentNodeId];
            logger?.LogDebug($"Distance to goal {currentNode.DistanceTo(goalNode):00000.00}m");
            foreach ((ulong neighborId, KeyValuePair<ulong, bool> wayId) in currentNode.Neighbors)
            {
                if (!graph.ContainsNode(neighborId))
                    graph.ConcatGraph(Graph.FromGraph(rl.LoadRegionFromNodeId(neighborId)));
                if (!graph.ContainsWay(wayId.Key))
                {
                    foreach (global::Graph.Graph? g in rl.LoadRegionsFromWayId(wayId.Key))
                        graph.ConcatGraph(Graph.FromGraph(g));
                }
                    
                OSM_Graph.Way way = graph.Ways[wayId.Key];
                byte speed = SpeedHelper.GetSpeed(way, car);
                if(speed < 1)
                    continue;
                if(!way.AccessPermitted())
                    continue;
                
                if(wayId.Value && way.GetDirection() == (fromStart ? WayDirection.Forwards : WayDirection.Backwards) && car)
                    continue;
                if(!wayId.Value && way.GetDirection() == (fromStart ? WayDirection.Backwards : WayDirection.Forwards) && car)
                    continue;
                    
                Node neighborNode = graph.Nodes[neighborId];
                    
                if (neighborNode.PreviousIsFromStart is not null && neighborNode.PreviousIsFromStart != fromStart)//Check if we found the opposite End
                    return fromStart ? PathFound(graph, currentNode, neighborNode, car, logger) : PathFound(graph, neighborNode, currentNode, car, logger);

                float metric = (currentNode.Metric ?? float.MaxValue) + (pathing is PathMeasure.Distance
                    ? (float)currentNode.DistanceTo(neighborNode)
                    : (float)currentNode.DistanceTo(neighborNode) / speed);
                if (neighborNode.PreviousNodeId is null || neighborNode.Metric > metric)
                {
                    neighborNode.PreviousNodeId = currentNodeId;
                    neighborNode.Metric = metric;
                    neighborNode.PreviousIsFromStart = fromStart;
                    toVisit.Enqueue(neighborId, priorityHelper.CalculatePriority(currentNode, neighborNode, goalNode, speed));
                }
                logger?.LogTrace($"Neighbor {neighborId} {neighborNode}");
            }

            return null;
        }
        
        private static Route PathFound(Graph graph, Node fromStart, Node fromEnd, bool car = true, ILogger? logger = null)
        {
            logger?.LogInformation("Path found!");
            List<Step> path = new();
            OSM_Graph.Way toNeighbor = graph.Ways[fromStart.Neighbors.First(n => graph.Nodes[n.Key] == fromEnd).Value.Key];
            path.Add(new Step(fromStart, fromEnd, (float)fromStart.DistanceTo(fromEnd), SpeedHelper.GetSpeed(toNeighbor, car)));
            Node current = fromStart;
            while (current.Metric != 0f)
            {
                Node previous = graph.Nodes[(ulong)current.PreviousNodeId!];
                OSM_Graph.Way previousToCurrent = graph.Ways[previous.Neighbors.First(n => graph.Nodes[n.Key] == current).Value.Key];
                Step step = new(previous, current, (float)previous.DistanceTo(current), SpeedHelper.GetSpeed(previousToCurrent, car));
                path.Add(step);
                current = previous;
            }
            path.Reverse();//Since we go from the middle backwards until here
            current = fromEnd;
            while (current.Metric != 0f)
            {
                Node next = graph.Nodes[(ulong)current.PreviousNodeId!];
                OSM_Graph.Way currentToNext = graph.Ways[current.Neighbors.First(n => graph.Nodes[n.Key] == next).Value.Key];
                Step step = new(current, next, (float)current.DistanceTo(next), SpeedHelper.GetSpeed(currentToNext, car));
                path.Add(step);
                current = next;
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