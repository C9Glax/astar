using Logging;
using Graph;

namespace astar
{
    public class Astar
    {

        /*
         * Loads the graph, chooses two nodes at random and calls a*
         */
        public Astar(Dictionary<UInt64, Node> nodes, Logger ?logger = null)
        {
            Random r = new();
            List<Node> path = new();
            while(path.Count < 1)
            {
                Node n1 = nodes[nodes.Keys.ElementAt(r.Next(0, nodes.Count - 1))];
                Node n2 = nodes[nodes.Keys.ElementAt(r.Next(0, nodes.Count - 1))];
                logger?.Log(LogLevel.INFO, "From {0} - {1} to {2} - {3} Distance {4}", n1.lat, n1.lon, n2.lat, n2.lon, Utils.DistanceBetweenNodes(n1,n2));
                path = FindPath(ref nodes, n1, n2, ref logger);
            }

            logger?.Log(LogLevel.INFO, "Path found");
            foreach (Node n in path)
                logger?.Log(LogLevel.INFO, "lat {0:000.00000} lon {1:000.00000} traveled {2:0000.00} / {3:0000.00} beeline {4:0000.00}", n.lat, n.lon, n.pathLength, path.ElementAt(path.Count-1).pathLength, n.goalDistance);
        }

        /*
         * Resets the calculated previous nodes and distance to goal
         */
        private static void Reset(ref Dictionary<ulong, Node> nodes)
        {
            foreach(Node n in nodes.Values)
            {
                n.previousNode = Node.nullnode;
                n.goalDistance = double.MaxValue;
            }
        }

        /*
         * 
         */
        private static List<Node> FindPath(ref Dictionary<ulong, Node> nodes, Node start, Node goal, ref Logger? logger)
        {
            Reset(ref nodes);
            List<Node> toVisit = new();
            toVisit.Add(start);
            Node currentNode = start;
            start.pathLength = 0;
            start.goalDistance = Utils.DistanceBetweenNodes(start, goal);
            while (currentNode != goal && toVisit.Count > 0)
            {
                currentNode = toVisit.First();
                logger?.Log(LogLevel.VERBOSE, "toVisit-length: {0} path: {1} goal: {2}", toVisit.Count, currentNode.pathLength, currentNode.goalDistance);
                //Check all neighbors of current node
                foreach (Edge e in currentNode.edges)
                {
                    if (e.neighbor.goalDistance == double.MaxValue)
                        e.neighbor.goalDistance = Utils.DistanceBetweenNodes(e.neighbor, goal);
                    if (e.neighbor.pathLength > currentNode.pathLength + e.weight)
                    {
                        e.neighbor.pathLength = currentNode.pathLength + e.weight;
                        e.neighbor.previousNode = currentNode;
                        toVisit.Add(e.neighbor);
                    }
                }
                toVisit.Remove(currentNode); //"Mark" as visited
                toVisit.Sort(CompareDistanceToGoal);
            }

            List<Node> path = new();

            if (currentNode == goal)
            {
                if(toVisit[0].pathLength < goal.pathLength)
                {
                    logger?.Log(LogLevel.INFO, "Way found, checking for shorter option.");
                    while (toVisit[0].pathLength < goal.pathLength)
                    {
                        currentNode = toVisit.First();
                        logger?.Log(LogLevel.VERBOSE, "toVisit-length: {0} path: {1} goal: {2}", toVisit.Count, currentNode.pathLength, currentNode.goalDistance);
                        //Check all neighbors of current node
                        foreach (Edge e in currentNode.edges)
                        {
                            if (e.neighbor.goalDistance == double.MaxValue)
                                e.neighbor.goalDistance = Utils.DistanceBetweenNodes(e.neighbor, goal);
                            if (e.neighbor.pathLength > currentNode.pathLength + e.weight)
                            {
                                e.neighbor.pathLength = currentNode.pathLength + e.weight;
                                e.neighbor.previousNode = currentNode;
                                toVisit.Add(e.neighbor);
                            }
                        }
                        toVisit.Remove(currentNode); //"Mark" as visited
                        toVisit.Sort(CompareDistanceToGoal);
                    }
                }
                else
                {
                    logger?.Log(LogLevel.INFO, "Way found, shortest option.");
                }
            }
            else
            {
                logger?.Log(LogLevel.INFO, "No path between {0} - {1} and {2} - {3}", start.lat, start.lon, goal.lat, goal.lon);
                return path;
            }

            path.Add(goal);
            while(currentNode != start)
            {
                path.Add(currentNode.previousNode);
                currentNode = currentNode.previousNode;
            }
            path.Reverse();
            return path;
        }
        
        /*
         * Compares two nodes and returns the node closer to the goal
         */
        private static int CompareDistanceToGoal(Node n1, Node n2)
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
    }
}