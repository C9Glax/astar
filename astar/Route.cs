using Graph;
namespace astar
{
    public class Route
    {
        public List<Step> steps { get; }
        public bool routeFound { get; set; }
        public float distance { get; set; }
        public float time { get; set; }

        public Route()
        {
            this.steps = new();
            this.distance = 0;
        }

        public void AddStep(Node start, Edge way)
        {
            this.steps.Add(new Step(start, way));
        }
    }

    public struct Step
    {
        public Node start { get; }
        public Edge edge { get; }
        public Step(Node start, Edge route)
        {
            this.start = start;
            this.edge = route;
        }
    }
}
