﻿using Logging;
using GeoGraph;
using GeoGraph.Utils;

namespace astar
{
    public class Astar
    {
        private Dictionary<Node, float> timeRequired = new();
        private Dictionary<Node, float> goalDistance = new();
        private Dictionary<Node, Node> previousNode = new();

        public Route FindPath(Graph graph, Node start, Node goal, Logger? logger)
        {
            logger?.Log(LogLevel.INFO, "From {0:000.00000}#{1:000.00000} to {2:000.00000}#{3:000.00000} Great-Circle {4:00000.00}km", start.lat, start.lon, goal.lat, goal.lon, Utils.DistanceBetweenNodes(start, goal)/1000);
            List<Node> toVisit = new();
            toVisit.Add(start);
            Node currentNode = start;
            SetTimeRequiredToReach(start, 0);
            SetDistanceToGoal(start, Convert.ToSingle(Utils.DistanceBetweenNodes(start, goal)));
            while (toVisit.Count > 0 && GetTimeRequiredToReach(toVisit[0]) < GetTimeRequiredToReach(goal))
            {
                currentNode = toVisit.First();
                logger?.Log(LogLevel.VERBOSE, "toVisit-length: {0} path-length: {1} goal-distance: {2}", toVisit.Count, timeRequired[currentNode], goalDistance[currentNode]);
                //Check all neighbors of current node
                foreach (Edge e in currentNode.edges)
                {
                    if (GetTimeRequiredToReach(e.neighbor) > GetTimeRequiredToReach(currentNode) + e.time)
                    {
                        SetDistanceToGoal(e.neighbor, Convert.ToSingle(Utils.DistanceBetweenNodes(e.neighbor, goal)));
                        SetTimeRequiredToReach(e.neighbor, GetTimeRequiredToReach(currentNode) + e.time);
                        SetPreviousNodeOf(e.neighbor, currentNode);
                        if (!toVisit.Contains(e.neighbor))
                            toVisit.Add(e.neighbor);
                    }
                }

                toVisit.Remove(currentNode); //"Mark" as visited
                toVisit.Sort(CompareDistance);
            }

            if(GetPreviousNodeOf(goal) != null)
            {
                logger?.Log(LogLevel.INFO, "Way found, shortest option.");
                currentNode = goal;
            }
            else
            {
                logger?.Log(LogLevel.INFO, "No path between {0:000.00000}#{1:000.00000} and {2:000.00000}#{3:000.00000}", start.lat, start.lon, goal.lat, goal.lon);
                return new Route(new List<Step>(), false, float.MaxValue, float.MaxValue);
            }

#pragma warning disable CS8604, CS8600 // Route was found, so has to have a previous node and edges
            List<Node> tempNodes = new();
            tempNodes.Add(goal);
            while(currentNode != start)
            {
                tempNodes.Add(GetPreviousNodeOf(currentNode));
                currentNode = GetPreviousNodeOf(currentNode);
            }
            tempNodes.Reverse();

            List<Step> steps = new();
            float totalDistance = 0;

            for(int i = 0; i < tempNodes.Count - 1; i++)
            {
                Edge e = tempNodes[i].GetEdgeToNode(tempNodes[i + 1]);
                steps.Add(new Step(tempNodes[i], e, GetTimeRequiredToReach(tempNodes[i]), GetDistanceToGoal(tempNodes[i])));
                totalDistance += e.distance;
            }

            Route _route = new Route(steps, true, totalDistance, GetTimeRequiredToReach(goal));

            logger?.Log(LogLevel.INFO, "Path found");

            if(logger?.level > LogLevel.INFO)
            {

                float time = 0;
                float distance = 0;

                logger?.Log(LogLevel.DEBUG, "Route Distance: {0:00000.00km} Time: {1}", _route.distance/1000, TimeSpan.FromSeconds(_route.time));
                for(int i = 0; i < _route.steps.Count; i++)
                {
                    Step s = _route.steps[i];
                    time += s.edge.time;
                    distance += s.edge.distance;
                    logger?.Log(LogLevel.DEBUG, "Step {0:000} From {1:000.00000}#{2:000.00000} To {3:000.00000}#{4:000.00000} along {5:0000000000} after {6} and {7:0000.00}km", i, s.start.lat, s.start.lon, s.edge.neighbor.lat, s.edge.neighbor.lon, s.edge.id, TimeSpan.FromSeconds(timeRequired[s.start]), distance/1000);
                }
            }

            return _route;
#pragma warning restore CS8604, CS8600
        }

        /*
         * Compares two nodes and returns the node closer to the goal
         * -1 => n1 smaller n2
         *  0 => n1 equal n2
         *  1 => n1 larger n2
         */
        private int CompareDistance(Node n1, Node n2)
        {
            if (n1 == null || n2 == null)
                return 0;
            else
            {
                if (GetDistanceToGoal(n1) < GetDistanceToGoal(n2))
                    return -1;
                else if (GetDistanceToGoal(n1) > GetDistanceToGoal(n2))
                    return 1;
                else return 0;
            }
        }

        /*
         * Compares two nodes and returns the node with the shorter path
         * -1 => n1 smaller n2
         *  0 => n1 equal n2
         *  1 => n1 larger n2
         */
        private int ComparePathLength(Node n1, Node n2)
        {
            if (n1 == null || n2 == null)
                return 0;
            else
            {
                if (GetTimeRequiredToReach(n1) < GetTimeRequiredToReach(n2))
                    return -1;
                else if (GetTimeRequiredToReach(n1) > GetTimeRequiredToReach(n2))
                    return 1;
                else return 0;
            }
        }


        private float GetTimeRequiredToReach(Node n)
        {
            if (timeRequired.TryGetValue(n, out float t))
            {
                return t;
            }
            else
            {
                return float.MaxValue;
            }
        }

        private void SetTimeRequiredToReach(Node n, float t)
        {
            if (!timeRequired.TryAdd(n, t))
                timeRequired[n] = t;
        }

        private float GetDistanceToGoal(Node n)
        {
            if (goalDistance.TryGetValue(n, out float t))
            {
                return t;
            }
            else
            {
                return float.MaxValue;
            }
        }

        private void SetDistanceToGoal(Node n, float d)
        {
            if (!goalDistance.TryAdd(n, d))
                goalDistance[n] = d;
        }

        private Node? GetPreviousNodeOf(Node n)
        {
            if(previousNode.TryGetValue(n, out Node? t))
            {
                return t;
            }else
            {
                return null;
            }
        }

        private void SetPreviousNodeOf(Node n, Node p)
        {
            if (!previousNode.TryAdd(n, p))
                previousNode[n] = p;
        }
    }
}