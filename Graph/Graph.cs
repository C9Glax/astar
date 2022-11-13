
namespace Graph
{
    public class Graph
    {
        private Dictionary<ulong, Node> nodes { get; }

        public Graph()
        {
            this.nodes = new();
        }

        public bool AddNode(ulong id, Node n)
        {
            return this.nodes.TryAdd(id, n);
        }

        public Node NodeAtIndex(int i)
        {
            return this.nodes.Values.ToArray()[i];
        }

        public int GetNodeCount()
        {
            return this.nodes.Count;
        }

        public Node? GetNode(ulong id)
        {
            if (this.nodes.TryGetValue(id, out Node? n))
                return n;
            else
                return null;
        }

        public bool ContainsNode(ulong id)
        {
            return this.nodes.ContainsKey(id);
        }

        public bool ContainsNode(Node n)
        {
            return this.nodes.Values.Contains(n);
        }

        public bool RemoveNode(ulong id)
        {
            return this.nodes.Remove(id);
        }

        public bool RemoveNode(Node n)
        {
            throw new NotImplementedException();
        }

        public Node ClosestNodeToCoordinates(float lat, float lon)
        {
            throw new NotImplementedException();
        }
    }
}
