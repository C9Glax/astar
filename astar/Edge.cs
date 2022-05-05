namespace Graph
{
    public struct Edge
    {
        public Node neighbor { get; }
        public ushort weight { get; }
        public Edge(Node neighbor, ushort weight)
        {
            this.neighbor = neighbor;
            this.weight = weight;
        }
    }
}
