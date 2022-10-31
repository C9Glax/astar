namespace Graph
{
    public struct Edge
    {
        public Node neighbor { get; }
        public float weight { get; }
        public Edge(Node neighbor, float weight)
        {
            this.neighbor = neighbor;
            this.weight = weight;
        }
    }
}
