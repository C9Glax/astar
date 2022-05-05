namespace Graph
{
    public struct Edge
    {
        public Node neighbor { get; }
        public Edge(Node neighbor)
        {
            this.neighbor = neighbor;
        }
    }
}
