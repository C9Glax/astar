namespace astar
{
    public class Route(Graph graph, List<Step> steps, bool routeFound)
    {
        public Graph Graph { get; } = graph;
        public List<Step> Steps { get; } = steps;
        public bool RouteFound { get; } = routeFound;
        public float Distance => Steps.Sum(step => step.Distance);

        public TimeSpan Time => TimeSpan.FromHours(Steps.Sum(step => step.Distance / 1000 / step.Speed));

        public ValueTuple<float, float> MinCoordinates()
        {
            float minLat = Graph.Nodes.MinBy(node => node.Value.Lat).Value.Lat;
            float minLon = Graph.Nodes.MinBy(node => node.Value.Lon).Value.Lon;
            return new ValueTuple<float, float>(minLat, minLon);
        }

        public ValueTuple<float, float> MaxCoordinates()
        {
            float maxLat = Graph.Nodes.MaxBy(node => node.Value.Lat).Value.Lat;
            float maxLon = Graph.Nodes.MaxBy(node => node.Value.Lon).Value.Lon;
            return new ValueTuple<float, float>(maxLat, maxLon);
        }

        public override string ToString()
        {
            return $"{string.Join("\n", Steps)}\n" +
                   $"Distance: {Distance:000000.00}m\n" +
                   $"Time: {Time:hh\\:mm\\:ss}";
        }
    }

    public struct Step(Node node1, Node node2, float distance, byte speed)
    {
        public readonly Node Node1 = node1, Node2 = node2;
        public readonly float Distance = distance;
        public readonly byte Speed = speed;

        public override string ToString()
        {
            return $"{Node1.Lat:00.000000} {Node1.Lon:000.000000} --- {Distance:0000.00}m {Speed:000} ---> {Node2.Lat:00.000000} {Node2.Lon:000.000000}";
        }
    }
}
