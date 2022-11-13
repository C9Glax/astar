
namespace Graph
{
    public class Graph
    {
        private List<Node> nodes { get; }

        public Graph()
        {
            this.nodes = new();
        }

        public bool AddNode(Node n)
        {
            if (!this.ContainsNode(n.id))
            {
                this.nodes.Add(n);
                return true;
            }
            else
            {
                return false;
            }
        }

        public Node GetNodeAtIndex(int i)
        {
            return this.nodes[i];
        }

        public int GetNodeCount()
        {
            return this.nodes.Count;
        }

        public Node? GetNode(ulong id)
        {
            foreach(Node n in this.nodes)
            {
                if (n.id == id)
                    return n;
            }
            return null;
        }

        public bool ContainsNode(ulong id)
        {
            return this.GetNode(id) != null;
        }

        public bool RemoveNode(ulong id)
        {
            Node? n = this.GetNode(id);
            if(n != null)
            {
                this.nodes.Remove(n);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool RemoveNode(Node n)
        {
            if (this.RemoveNode(n.id))
            {
                this.nodes.Remove(n);
                return true;
            }
            else
            {
                return false;
            }
        }

        public Node ClosestNodeToCoordinates(float lat, float lon)
        {
            throw new NotImplementedException();
        }
    }
}
