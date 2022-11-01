namespace Graph
{
    public struct Edge
    {
        public ulong id { get; }
        public Node neighbor { get; }
        public float weight { get; }
        public Edge(Node neighbor, float weight)
        {
            this.neighbor = neighbor;
            this.weight = weight;
            this.id = 0;
        }

        public Edge(Node neighbor, float weight, ulong id)
        {
            this.neighbor = neighbor;
            this.weight = weight;
            this.id = id;
        }
    }
}
