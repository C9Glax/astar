using astar.PathingHelper;
using Microsoft.Extensions.Logging;
using Graph.Utils;
using OSM_Graph.Enums;
using OSM_Regions;

namespace astar
{
    public class Astar(ValueTuple<float, float, float, float>? priorityWeights = null, int? explorationMultiplier = null, float? nonPriorityRoadSpeedPenalty = null)
    {
        private readonly ValueTuple<float, float, float, float> DefaultPriorityWeights = priorityWeights ?? new(0.7f, 1.08f, 0, 0);
        private readonly int _explorationMultiplier = explorationMultiplier ?? 150;
        private readonly float _nonPriorityRoadSpeedPenalty = nonPriorityRoadSpeedPenalty ?? 0.9f;
        
        public Route FindPath(float startLat, float startLon, float endLat, float endLon, float regionSize, bool car = true, PathMeasure pathing = PathMeasure.Distance, string? importFolderPath = null, ILogger? logger = null)
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
            PriorityHelper priorityHelper = new(totalDistance, SpeedHelper.GetTheoreticalMaxSpeed(car));

            logger?.Log(LogLevel.Information,
                "From {0:00.00000}#{1:000.00000} to {2:00.00000}#{3:000.00000} Great-Circle {4:00000.00}km",
                startNode.Value.Lat, startNode.Value.Lon, endNode.Value.Lat, endNode.Value.Lon, totalDistance / 1000);

            PriorityQueue<ulong, int> toVisitStart = new();
            toVisitStart.Enqueue(startNode.Key, 0);
            PriorityQueue<ulong, int> toVisitEnd = new();
            toVisitEnd.Enqueue(endNode.Key, 0);

            ValueTuple<Node, Node>? meetingEnds = null;
            while (toVisitStart.Count > 0 && toVisitEnd.Count > 0)
            {
                for (int i = 0; i < Math.Min(toVisitStart.Count * 0.5, 50) && meetingEnds is null; i++)
                {
                    ulong closestEndNodeId = toVisitEnd.UnorderedItems.MinBy(node => graph.Nodes[node.Element].DistanceTo(graph.Nodes[toVisitStart.Peek()])).Element;
                    Node closestEndNode = graph.Nodes[closestEndNodeId];
                    meetingEnds = ExploreSide(true, graph, toVisitStart, rl, priorityHelper, closestEndNode, car, DefaultPriorityWeights, pathing, logger);
                }
                
                for (int i = 0; i < Math.Min(toVisitEnd.Count * 0.5, 50) && meetingEnds is null; i++)
                {
                    ulong closestStartNodeId = toVisitStart.UnorderedItems.MinBy(node => graph.Nodes[node.Element].DistanceTo(graph.Nodes[toVisitEnd.Peek()])).Element;
                    Node closestStartNode = graph.Nodes[closestStartNodeId];
                    meetingEnds = ExploreSide(false, graph, toVisitEnd, rl, priorityHelper, closestStartNode, car, DefaultPriorityWeights, pathing, logger);
                }

                if (meetingEnds is not null)
                    break;
                logger?.LogDebug($"toVisit-Queues: {toVisitStart.Count} {toVisitStart.UnorderedItems.MinBy(i => i.Priority).Priority} {toVisitEnd.Count} {toVisitEnd.UnorderedItems.MinBy(i => i.Priority).Priority}");
            }
            if(meetingEnds is null)
                return new Route(graph, Array.Empty<Step>().ToList(), false);

            Queue<ulong> routeQueue = new();
            foreach (ulong id in toVisitStart.UnorderedItems.Select(l => l.Element)
                         .Union(toVisitEnd.UnorderedItems.Select(l => l.Element)))
            {
                routeQueue.Enqueue(id);
            }
            ValueTuple<Node, Node>? newMeetingEnds = Optimize(graph, routeQueue, car, rl, pathing, logger);
            meetingEnds = newMeetingEnds ?? meetingEnds;

            return PathFound(graph, meetingEnds!.Value.Item1, meetingEnds.Value.Item2, car, logger);
        }

        private ValueTuple<Node, Node>? ExploreSide(bool fromStart, Graph graph, PriorityQueue<ulong, int> toVisit, RegionLoader rl, PriorityHelper priorityHelper, Node goalNode, bool car, ValueTuple<float,float,float,float> ratingWeights, PathMeasure pathing, ILogger? logger = null)
        {
            ulong currentNodeId = toVisit.Dequeue();
            Node currentNode = graph.Nodes[currentNodeId];
            logger?.LogDebug($"Distance to goal {currentNode.DistanceTo(goalNode):00000.00}m");
            foreach ((ulong neighborId, KeyValuePair<ulong, bool> wayId) in currentNode.Neighbors)
            {
                LoadNeighbor(graph, neighborId, wayId.Key, rl, logger);
                    
                OSM_Graph.Way way = graph.Ways[wayId.Key];
                byte speed = SpeedHelper.GetSpeed(way, car);
                if(!IsNeighborReachable(speed, wayId.Value, fromStart, way, car))
                    continue;
                if (car && !way.IsPriorityRoad())
                    speed = (byte)(speed * _nonPriorityRoadSpeedPenalty);
                
                Node neighborNode = graph.Nodes[neighborId];

                if (neighborNode.PreviousIsFromStart is not null &&
                    neighborNode.PreviousIsFromStart != fromStart) //Check if we found the opposite End
                    return fromStart ? new(currentNode, neighborNode) : new(neighborNode, currentNode);

                float metric = (currentNode.Metric ?? float.MaxValue) + (pathing is PathMeasure.Distance
                    ? (float)currentNode.DistanceTo(neighborNode)
                    : (float)currentNode.DistanceTo(neighborNode) / speed);
                if (neighborNode.PreviousNodeId is null || neighborNode.Metric > metric)
                {
                    neighborNode.PreviousNodeId = currentNodeId;
                    neighborNode.Metric = metric;
                    neighborNode.PreviousIsFromStart = fromStart;
                    toVisit.Enqueue(neighborId,
                        priorityHelper.CalculatePriority(currentNode, neighborNode, goalNode, speed, ratingWeights));
                }
                logger?.LogTrace($"Neighbor {neighborId} {neighborNode}");
            }

            return null;
        }

        private ValueTuple<Node, Node>? Optimize(Graph graph, Queue<ulong> combinedQueue, bool car, RegionLoader rl, PathMeasure pathing, ILogger? logger = null)
        {
            int currentPathLength = graph.Nodes.Values.Count(node => node.PreviousNodeId is not null);
            int optimizeAfterFound = (int)(combinedQueue.Count * _explorationMultiplier); //Check another x% of unexplored Paths.
            logger?.LogInformation($"Path found (explored {currentPathLength} Nodes). Optimizing route. (exploring {optimizeAfterFound} additional Nodes)");
            ValueTuple<Node, Node>? newMeetingEnds = null;
            while (optimizeAfterFound-- > 0 && combinedQueue.Count > 0)
            {
                ulong currentNodeId = combinedQueue.Dequeue();
                Node currentNode = graph.Nodes[currentNodeId];
                bool fromStart = (bool)currentNode.PreviousIsFromStart!;
                foreach ((ulong neighborId, KeyValuePair<ulong, bool> wayId) in currentNode.Neighbors)
                {
                    LoadNeighbor(graph, neighborId, wayId.Key, rl, logger);
                    
                    OSM_Graph.Way way = graph.Ways[wayId.Key];
                    byte speed = SpeedHelper.GetSpeed(way, car);
                    if(!IsNeighborReachable(speed, wayId.Value, fromStart, way, car))
                        continue;
                    if (car && !way.IsPriorityRoad())
                        speed = (byte)(speed * _nonPriorityRoadSpeedPenalty);
                    
                    Node neighborNode = graph.Nodes[neighborId];

                    if (neighborNode.PreviousIsFromStart is not null &&
                        neighborNode.PreviousIsFromStart != fromStart) //Check if we found the opposite End
                    {
                        newMeetingEnds = fromStart ? new(currentNode, neighborNode) : new(neighborNode, currentNode);
                    }

                    float metric = (currentNode.Metric ?? float.MaxValue) + (pathing is PathMeasure.Distance
                        ? (float)currentNode.DistanceTo(neighborNode)
                        : (float)currentNode.DistanceTo(neighborNode) / speed);
                    if (neighborNode.PreviousNodeId is null || (neighborNode.PreviousIsFromStart == fromStart && neighborNode.Metric > metric))
                    {
                        neighborNode.PreviousNodeId = currentNodeId;
                        neighborNode.Metric = metric;
                        neighborNode.PreviousIsFromStart = fromStart;
                        combinedQueue.Enqueue(neighborId);
                    }
                    logger?.LogTrace($"Neighbor {neighborId} {neighborNode}");
                    logger?.LogDebug($"Optimization Contingent: {optimizeAfterFound}/{combinedQueue.Count}");
                }
            }
            
            logger?.LogDebug($"Nodes in Queue after Optimization: {combinedQueue.Count}");

            return newMeetingEnds;
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

        private static bool IsNeighborReachable(byte speed, bool wayDirection, bool fromStart, OSM_Graph.Way way, bool car)
        {
            if(speed < 1)
                return false;
            if(!way.AccessPermitted())
                return false;
            if(wayDirection && way.GetDirection() == (fromStart ? WayDirection.Backwards : WayDirection.Forwards) && car)
                return false;
            if(!wayDirection && way.GetDirection() == (fromStart ? WayDirection.Forwards : WayDirection.Backwards) && car)
                return false;
            return true;
        }

        private static void LoadNeighbor(Graph graph, ulong neighborId, ulong wayId, RegionLoader rl, ILogger? logger = null)
        {
            
            if (!graph.ContainsNode(neighborId))
                graph.ConcatGraph(Graph.FromGraph(rl.LoadRegionFromNodeId(neighborId)));
            if (!graph.ContainsWay(wayId))
            {
                logger?.LogDebug("Loading way... This will be slow.");
                foreach (global::Graph.Graph? g in rl.LoadRegionsFromWayId(wayId))
                    graph.ConcatGraph(Graph.FromGraph(g));
            }
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