namespace Graph
{
    public class Node
    {
        public float lat { get; }
        public float lon { get; }
        public HashSet<Edge> edges { get; }

        public Node? previousNode { get; set; }
        public float goalDistance { get; set; }

        public float timeRequired { get; set; }

        public Node(float lat, float lon)
        {
            this.lat = lat;
            this.lon = lon;
            this.edges = new();
            this.previousNode = null;
            this.goalDistance = float.MaxValue;
            this.timeRequired = float.MaxValue;
        }

        public Edge? GetEdgeToNode(Node n)
        {
            foreach (Edge e in this.edges)
                if (e.neighbor == n)
                    return e;
            return null;
        }
    }
}
