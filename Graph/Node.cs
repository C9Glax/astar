namespace Graph
{
    public class Node
    {
        public float lat { get; }
        public float lon { get; }
        public List<Edge> edges { get; }

        public Node previousNode { get; set; }
        public double goalDistance { get; set; }

        public Node(float lat, float lon)
        {
            this.lat = lat;
            this.lon = lon;
            this.edges = new List<Edge>();
            this.previousNode = nullnode;
            this.goalDistance = uint.MaxValue;
        }
        public static Node nullnode = new Node(float.NaN, float.NaN);
    }
}
