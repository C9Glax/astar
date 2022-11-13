using Logging;
using Graph;
using Graph.Utils;

namespace astar
{
    public class Astar
    {
        Dictionary<Node, float> timeRequired = new();
        Dictionary<Node, float> goalDistance = new();
        Dictionary<Node, Node> previousNode = new();

        public Route FindPath(Graph.Graph graph, Node start, Node goal, Logger? logger)
        {
            logger?.Log(LogLevel.INFO, "From {0:000.00000}#{1:000.00000} to {2:000.00000}#{3:000.00000} Great-Circle {4:00000.00}km", start.lat, start.lon, goal.lat, goal.lon, Utils.DistanceBetweenNodes(start, goal)/1000);
            List<Node> toVisit = new();
            toVisit.Add(start);
            Node currentNode = start;
            timeRequired.Add(start, 0);
            goalDistance.Add(start, Convert.ToSingle(Utils.DistanceBetweenNodes(start, goal)));
            while (toVisit.Count > 0 && timeRequired[toVisit[0]] < timeRequired[goal])
            {
                if(currentNode == goal)
                {
                    logger?.Log(LogLevel.INFO, "Way found, checking for shorter option.");
                }
                currentNode = toVisit.First();
                logger?.Log(LogLevel.VERBOSE, "toVisit-length: {0} path-length: {1} goal-distance: {2}", toVisit.Count, timeRequired[currentNode], goalDistance[currentNode]);
                //Check all neighbors of current node
                foreach (Edge e in currentNode.edges)
                {
                    if (timeRequired[e.neighbor] > timeRequired[currentNode] + e.time)
                    {
                        goalDistance[e.neighbor] = Convert.ToSingle(Utils.DistanceBetweenNodes(e.neighbor, goal));
                        timeRequired[e.neighbor] = timeRequired[currentNode] + e.time;
                        previousNode[e.neighbor] = currentNode;
                        toVisit.Add(e.neighbor);
                    }
                }

                toVisit.Remove(currentNode); //"Mark" as visited
                toVisit.Sort(CompareDistance);
            }

            if(previousNode[goal] != null)
            {
                logger?.Log(LogLevel.INFO, "Way found, shortest option.");
                currentNode = goal;
            }
            else
            {
                logger?.Log(LogLevel.INFO, "No path between {0:000.00000}#{1:000.00000} and {2:000.00000}#{3:000.00000}", start.lat, start.lon, goal.lat, goal.lon);
                return new Route(new List<Step>(), false, float.MaxValue, float.MaxValue);
            }

            List<Node> tempNodes = new();
            tempNodes.Add(goal);
            while(currentNode != start)
            {
#pragma warning disable CS8604 // Route was found, so has to have a previous node
                tempNodes.Add(previousNode[currentNode]);
#pragma warning restore CS8604
                currentNode = previousNode[currentNode];
            }
            tempNodes.Reverse();

            List<Step> steps = new();
            float totalDistance = 0;

            for(int i = 0; i < tempNodes.Count - 1; i++)
            {
#pragma warning disable CS8600, CS8604 // Route was found, so has to have an edge
                Edge e = tempNodes[i].GetEdgeToNode(tempNodes[i + 1]);
                steps.Add(new Step(tempNodes[i], e, timeRequired[tempNodes[i]], goalDistance[tempNodes[i]]));
#pragma warning restore CS8600, CS8604
                totalDistance += e.distance;
            }

            Route _route = new Route(steps, true, totalDistance, timeRequired[goal]);

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
                if (goalDistance[n1] < goalDistance[n2])
                    return -1;
                else if (goalDistance[n1] > goalDistance[n2])
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
                if (timeRequired[n1] < timeRequired[n2])
                    return -1;
                else if (timeRequired[n1] > timeRequired[n2])
                    return 1;
                else return 0;
            }
        }


    }
}