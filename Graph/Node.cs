namespace Graph
{
    public class Node
    {
        public float lat { get; }
        public float lon { get; }

        public ulong id { get; }
        public HashSet<Edge> edges { get; }

        public Node(ulong id, float lat, float lon)
        {
            this.id = id;
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
