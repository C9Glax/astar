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
                n.timeSpent = float.MaxValue;
            }
        }

        /*
         * 
         */
        public static bool FindPath(ref Dictionary<ulong, Node> nodes, Node start, Node goal, out List<Node> path, Logger? logger)
        {
            path = new List<Node>();
            logger?.Log(LogLevel.INFO, "From {0} - {1} to {2} - {3} Distance {4}", start.lat, start.lon, goal.lat, goal.lon, Utils.DistanceBetweenNodes(start, goal));
            Reset(ref nodes);
            List<Node> toVisit = new();
            toVisit.Add(start);
            Node currentNode = start;
            start.timeSpent = 0;
            start.goalDistance = Utils.DistanceBetweenNodes(start, goal);
            while (toVisit.Count > 0 && toVisit[0].timeSpent < goal.timeSpent)
            {
                if(currentNode == goal)
                {
                    logger?.Log(LogLevel.INFO, "Way found, checking for shorter option.");
                }
                currentNode = toVisit.First();
                logger?.Log(LogLevel.VERBOSE, "toVisit-length: {0} path-length: {1} goal-distance: {2}", toVisit.Count, currentNode.timeSpent, currentNode.goalDistance);
                //Check all neighbors of current node
                foreach (Edge e in currentNode.edges)
                {
                    if (e.neighbor.timeSpent > currentNode.timeSpent + e.weight)
                    {
                        e.neighbor.goalDistance = Utils.DistanceBetweenNodes(e.neighbor, goal);
                        e.neighbor.timeSpent = currentNode.timeSpent + e.weight;
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
            }
            else
            {
                logger?.Log(LogLevel.INFO, "No path between {0} - {1} and {2} - {3}", start.lat, start.lon, goal.lat, goal.lon);
                return false;
            }

            path.Add(goal);
            while(currentNode != start)
            {
                if(currentNode.previousNode != null)
                {
                    path.Add(currentNode.previousNode);
                    currentNode = currentNode.previousNode;
                }
            }
            path.Reverse();

            logger?.Log(LogLevel.INFO, "Path found");
            float distance = 0;
            Node? prev = null;
            TimeSpan totalTime = TimeSpan.FromSeconds(path.ElementAt(path.Count - 1).timeSpent);
            
            foreach (Node n in path)
            {
                if(prev != null)
                {
                    distance += Utils.DistanceBetweenNodes(prev, n);
                }
                prev = n;
                logger?.Log(LogLevel.DEBUG, "lat {0:000.00000} lon {1:000.00000} traveled {5:0000.00}km in {2:G} / {3:G} Great-Circle to Goal {4:0000.00}", n.lat, n.lon, TimeSpan.FromSeconds(n.timeSpent), totalTime, n.goalDistance, distance);
            }


            return true;
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
                if (n1.timeSpent < n2.timeSpent)
                    return -1;
                else if (n1.timeSpent > n2.timeSpent)
                    return 1;
                else return 0;
            }
        }
    }
}