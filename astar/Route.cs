namespace astar
{
    public class Route(Graph graph, List<Step> steps, bool routeFound)
    {
        public Graph Graph { get; } = graph;
        public List<Step> Steps { get; } = steps;
        public bool RouteFound { get; } = routeFound;
        public float Distance => Steps.Sum(step => step.Distance);

        public KeyValuePair<float, float> MinCoordinates()
        {
            float minLat, minLon;
            if (RouteFound)
            {
                Step minLatStep = Steps.MinBy(step => step.Node1.Lat < step.Node2.Lat ? step.Node1.Lat : step.Node2.Lat);
                Step minLonStep = Steps.MinBy(step => step.Node1.Lon < step.Node2.Lon ? step.Node1.Lon : step.Node2.Lon);
                minLat = minLatStep.Node1.Lat < minLatStep.Node2.Lat ? minLatStep.Node1.Lat : minLatStep.Node2.Lat;
                minLon = minLonStep.Node1.Lon < minLonStep.Node2.Lon ? minLonStep.Node1.Lon : minLonStep.Node2.Lon;
            }
            else
            {
                minLat = Graph.Nodes.MinBy(node => node.Value.Lat).Value.Lat;
                minLon = Graph.Nodes.MinBy(node => node.Value.Lon).Value.Lon;
            }
            return new KeyValuePair<float, float>(minLat, minLon);
        }

        public KeyValuePair<float, float> MaxCoordinates()
        {
            float maxLat, maxLon;
            if (RouteFound)
            {
                Step maxLatStep = Steps.MaxBy(step => step.Node1.Lat > step.Node2.Lat ? step.Node1.Lat : step.Node2.Lat);
                Step maxLonStep = Steps.MaxBy(step => step.Node1.Lon > step.Node2.Lon ? step.Node1.Lon : step.Node2.Lon);
                maxLat = maxLatStep.Node1.Lat > maxLatStep.Node2.Lat ? maxLatStep.Node1.Lat : maxLatStep.Node2.Lat;
                maxLon = maxLonStep.Node1.Lon > maxLonStep.Node2.Lon ? maxLonStep.Node1.Lon : maxLonStep.Node2.Lon;
            }
            else
            {
                maxLat = Graph.Nodes.MaxBy(node => node.Value.Lat).Value.Lat;
                maxLon = Graph.Nodes.MaxBy(node => node.Value.Lon).Value.Lon;
            }
            return new KeyValuePair<float, float>(maxLat, maxLon);
        }
    }

    public struct Step(float distance, Node node1, Node node2)
    {
        public readonly Node Node1 = node1, Node2 = node2;
        public readonly float Distance = distance;

        public override string ToString()
        {
            return $"{Node1.Lat:00.000000} {Node1.Lon:000.000000} --- {Distance:0000.00}m ---> {Node2.Lat:00.000000} {Node2.Lon:000.000000}";
        }
    }
}
