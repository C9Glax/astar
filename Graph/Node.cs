namespace Graph
{
    public class Node
    {
        public float lat { get; }
        public float lon { get; }

        public HashSet<Edge> edges { get; }

        public Node(float lat, float lon)
        {
            this.lat = lat;
            this.lon = lon;
            this.edges = new();
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
