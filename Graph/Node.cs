namespace Graph
{
    public class Node
    {
        public float lat { get; }
        public float lon { get; }
        public HashSet<Edge> edges { get; }

        public Node previousNode { get; set; }
        public float goalDistance { get; set; }

        public float pathLength { get; set; }

        public Node(float lat, float lon)
        {
            this.lat = lat;
            this.lon = lon;
            this.edges = new();
            this.previousNode = nullnode;
            this.goalDistance = float.MaxValue;
            this.pathLength = float.MaxValue;
        }
        public static Node nullnode = new(float.NaN, float.NaN);
    }
}
