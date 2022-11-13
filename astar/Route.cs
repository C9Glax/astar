using Graph;
namespace astar
{
    public class Route
    {
        public List<Step> steps { get; }
        public bool routeFound { get; }
        public float distance { get; }
        public float time { get; }


        public Route(List<Step> steps, bool routeFound, float distance, float timeRequired)
        {
            this.steps = steps;
            this.routeFound = routeFound;
            this.distance = distance;
            this.time = timeRequired;
        }
    }

    public struct Step
    {
        public Node start { get; }
        public Edge edge { get; }

        public float timeRequired { get; }
        public float goalDistance { get; }
        public Step(Node start, Edge route, float timeRequired, float goalDistance)
        {
            this.start = start;
            this.edge = route;
            this.timeRequired = timeRequired;
            this.goalDistance = goalDistance;
        }
    }
}
