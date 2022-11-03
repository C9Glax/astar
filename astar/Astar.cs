using Logging;
using Graph;

namespace astar
{
    public class Astar
    {

        /*
         * Resets the calculated previous nodes and distance to goal
         */
        private static void Reset(ref Dictionary<ulong, Node> nodes)
        {
            foreach(Node n in nodes.Values)
            {
                n.previousNode = null;
                n.goalDistance = float.MaxValue;
                n.timeRequired = float.MaxValue;
            }
        }


        public static Route FindPath(ref Dictionary<ulong, Node> nodes, Node start, Node goal, Logger? logger)
        {
            Route _route = new Route();
            logger?.Log(LogLevel.INFO, "From {0:000.00000}#{1:000.00000} to {2:000.00000}#{3:000.00000} Great-Circle {4:00000.00}km", start.lat, start.lon, goal.lat, goal.lon, Utils.DistanceBetweenNodes(start, goal)/1000);
            Reset(ref nodes);
            List<Node> toVisit = new();
            toVisit.Add(start);
            Node currentNode = start;
            start.timeRequired = 0;
            start.goalDistance = Utils.DistanceBetweenNodes(start, goal);
            while (toVisit.Count > 0 && toVisit[0].timeRequired < goal.timeRequired)
            {
                if(currentNode == goal)
                {
                    logger?.Log(LogLevel.INFO, "Way found, checking for shorter option.");
                }
                currentNode = toVisit.First();
                logger?.Log(LogLevel.VERBOSE, "toVisit-length: {0} path-length: {1} goal-distance: {2}", toVisit.Count, currentNode.timeRequired, currentNode.goalDistance);
                //Check all neighbors of current node
                foreach (Edge e in currentNode.edges)
                {
                    if (e.neighbor.timeRequired > currentNode.timeRequired + e.time)
                    {
                        e.neighbor.goalDistance = Utils.DistanceBetweenNodes(e.neighbor, goal);
                        e.neighbor.timeRequired = currentNode.timeRequired + e.time;
                        e.neighbor.previousNode = currentNode;
                        toVisit.Add(e.neighbor);
                    }
                }

                toVisit.Remove(currentNode); //"Mark" as visited
                toVisit.Sort(CompareDistance);
            }
            if(goal.previousNode != null)
            {
                logger?.Log(LogLevel.INFO, "Way found, shortest option.");
                currentNode = goal;
                _route.routeFound = true;
                _route.time = goal.timeRequired;
            }
            else
            {
                logger?.Log(LogLevel.INFO, "No path between {0:000.00000}#{1:000.00000} and {2:000.00000}#{3:000.00000}", start.lat, start.lon, goal.lat, goal.lon);
                _route.routeFound = false;
                return _route;
            }

            List<Node> tempNodes = new();
            tempNodes.Add(goal);
            while(currentNode != start)
            {
#pragma warning disable CS8604 // Route was found, so has to have a previous node
                tempNodes.Add(currentNode.previousNode);
#pragma warning restore CS8604
                currentNode = currentNode.previousNode;
            }
            tempNodes.Reverse();
            for(int i = 0; i < tempNodes.Count - 1; i++)
            {
#pragma warning disable CS8600, CS8604 // Route was found, so has to have an edge
                Edge e = tempNodes[i].GetEdgeToNode(tempNodes[i + 1]);
                _route.AddStep(tempNodes[i], e);
#pragma warning restore CS8600, CS8604
                _route.distance += e.distance;
            }

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
                    logger?.Log(LogLevel.DEBUG, "Step {0:000} From {1:000.00000}#{2:000.00000} To {3:000.00000}#{4:000.00000} along {5:0000000000} after {6} and {7:0000.00}km", i, s.start.lat, s.start.lon, s.edge.neighbor.lat, s.edge.neighbor.lon, s.edge.id, TimeSpan.FromSeconds(s.start.timeRequired), distance/1000);
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
        private static int CompareDistance(Node n1, Node n2)
        {
            if (n1 == null || n2 == null)
                return 0;
            else
            {
                if (n1.goalDistance < n2.goalDistance)
                    return -1;
                else if (n1.goalDistance > n2.goalDistance)
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
        private static int ComparePathLength(Node n1, Node n2)
        {
            if (n1 == null || n2 == null)
                return 0;
            else
            {
                if (n1.timeRequired < n2.timeRequired)
                    return -1;
                else if (n1.timeRequired > n2.timeRequired)
                    return 1;
                else return 0;
            }
        }


    }
}