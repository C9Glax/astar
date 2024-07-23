using astar.PathingHelper;
using Graph;

namespace astar;

public class Graph
{
    public readonly Dictionary<ulong, Node> Nodes = new();
    public readonly Dictionary<ulong, OSM_Graph.Way> Ways = new ();

    public static Graph? FromGraph(global::Graph.Graph? graph)
    {
        if (graph is null)
            return null;
        Graph ret = new();
        foreach ((ulong id, global::Graph.Node? node) in graph.Nodes)
            ret.Nodes.Add(id, Node.FromGraphNode(node));
        foreach ((ulong id, Way? way) in graph.Ways)
            ret.Ways.Add(id, new OSM_Graph.Way(id, way.Tags, new()));
        return ret;
    }

    public void ConcatGraph(Graph? graph)
    {
        if (graph is null)
            return;
        foreach ((ulong id, Node n) in graph.Nodes)
            this.Nodes.TryAdd(id, n);
        foreach ((ulong id, OSM_Graph.Way w) in graph.Ways)
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

    public bool ContainsWay(OSM_Graph.Way way)
    {
        return Ways.ContainsValue(way);
    }

    public bool ContainsWay(ulong wayId)
    {
        return Ways.ContainsKey(wayId);
    }

    public KeyValuePair<ulong, Node> ClosestNodeToCoordinates(float lat, float lon, bool car = true)
    {
        return Nodes.Where(n => n.Value.Neighbors.Values.Any(way => SpeedHelper.GetSpeed(Ways[way.Key], car) > 0)).MinBy(n => n.Value.DistanceTo(lat, lon));
    }

    public override string ToString()
    {
        return $"Graph {Nodes.Count} Nodes {Ways.Count} Ways.";
    }
}