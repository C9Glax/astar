namespace Graph
{
    public struct Edge
    {
        public Node neighbor { get; }
        public double weight { get; }
        public Edge(Node neighbor, double weight)
        {
            this.neighbor = neighbor;
            this.weight = weight;
        }
    }
}
