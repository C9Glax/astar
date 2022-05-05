using Logging;
using Graph;
using OpenStreetMap_Importer;

namespace astar
{
    public class Astar
    {
        private Logger logger;
        public Astar()
        {
            this.logger = new Logger(LogType.Console, loglevel.DEBUG);
            Dictionary<UInt64, Node> nodes = Importer.Import(ref logger);
            Random r = new Random();
            List<Node> path = new List<Node>();
            while(path.Count < 1)
            {
                Node n1 = nodes[nodes.Keys.ElementAt(r.Next(0, nodes.Count - 1))];
                Node n2 = nodes[nodes.Keys.ElementAt(r.Next(0, nodes.Count - 1))];
                logger.Log(loglevel.INFO, "From {0} - {1} to {2} - {3}", n1.lat, n1.lon, n2.lat, n2.lon);
                path = FindPath(n1, n2, ref this.logger);
            }

            logger.Log(loglevel.INFO, "Path found");
            foreach (Node n in path)
                logger.Log(loglevel.INFO, "{0} {1} Distance left {2}", n.lat, n.lon, n.goalDistance);
        }

        private static void Reset(ref Dictionary<ulong, Node> nodes)
        {
            foreach(Node n in nodes.Values)
            {
                n.previousNode = Node.nullnode;
                n.goalDistance = double.MaxValue;
            }
        }

        private static List<Node> FindPath(Node start, Node goal, ref Logger logger)
        {
            List<Node> toVisit = new List<Node>();
            toVisit.Add(start);
            Node currentNode = start;
            while(currentNode != goal && toVisit.Count > 0)
            {
                logger.Log(loglevel.VERBOSE, "toVisit-length: {0} distance: {1}", toVisit.Count, currentNode.goalDistance);
                foreach (Edge e in currentNode.edges)
                {
                    if(e.neighbor.previousNode == Node.nullnode)
                    {
                        toVisit.Add(e.neighbor);
                        e.neighbor.goalDistance = Utils.DistanceBetweenNodes(e.neighbor, goal);
                        e.neighbor.previousNode = currentNode;
                    }
                }
                toVisit.Remove(currentNode);
                toVisit.Sort(CompareDistanceToGoal);
                currentNode = toVisit.First();
            }

            List<Node> path = new List<Node>();

            if (currentNode != goal)
            {
                logger.Log(loglevel.INFO, "No path between {0} - {1} and {2} - {3}", start.lat, start.lon, goal.lat, goal.lon);
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
        
        private static int CompareDistanceToGoal(Node n1, Node n2)
        {
            if (n1 == null || n2 == null)
                return 0;
            else
            {
                if (n1.goalDistance < n2.goalDistance)
                    return 1;
                else if (n1.goalDistance > n2.goalDistance)
                    return -1;
                else return 0;
            }
        }
    }
}