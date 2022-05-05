namespace Graph
{
    public struct Node
    {
        public float lat { get; }
        public float lon { get; }
        public List<Edge> edges { get; }

        public Node(float lat, float lon)
        {
            this.lat = lat;
            this.lon = lon;
            this.edges = new List<Edge>(); 
        }
    }
}
