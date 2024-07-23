using astar.PathingHelper;
using Graph;

namespace astar;

public class Graph
{
    public readonly Dictionary<ulong, Node> Nodes = new();
    public readonly Dictionary<ulong, Way> Ways = new ();

    public static Graph? FromGraph(global::Graph.Graph? graph)
    {
        if (graph is null)
            return null;
        Graph ret = new();
        foreach ((ulong id, global::Graph.Node? node) in graph.Nodes)
            ret.Nodes.Add(id, Node.FromGraphNode(node));
        foreach ((ulong id, Way? way) in graph.Ways)
            ret.Ways.Add(id, way);
        return ret;
    }

    public void ConcatGraph(Graph? graph)
    {
        if (graph is null)
            return;
        foreach ((ulong id, Node n) in graph.Nodes)
            this.Nodes.TryAdd(id, n);
        foreach ((ulong id, Way w) in graph.Ways)
            this.Ways.TryAdd(id, w);
    }

    public bool ContainsNode(Node node)
    {
        return Nodes.ContainsValue(node);
    }

    public bool ContainsNode(ulong nodeId)
    {
        return Nodes.ContainsKey(nodeId);
    }

    public bool ContainsWay(Way way)
    {
        return Ways.ContainsValue(way);
    }

    public bool ContainsWay(ulong wayId)
    {
        return Ways.ContainsKey(wayId);
    }

    public KeyValuePair<ulong, Node> ClosestNodeToCoordinates(float lat, float lon, bool car = true)
    {
        return Nodes.Where(n => n.Value.Neighbors.Values.Any(wayId => SpeedHelper.GetSpeed(Ways[wayId], car) > 0)).MinBy(n => n.Value.DistanceTo(lat, lon));
    }

    public override string ToString()
    {
        return $"Graph {Nodes.Count} Nodes {Ways.Count} Ways.";
    }
}