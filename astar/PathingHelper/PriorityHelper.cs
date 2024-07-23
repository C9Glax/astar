namespace astar.PathingHelper;

public class PriorityHelper(double totalDistance, byte maxSpeed)
{
    private readonly double _totalDistance = totalDistance;
    private readonly byte _maxSpeed = maxSpeed;
    public int CalculatePriority(Node current, Node neighbor, Node goal, byte speed)
    {
        double neighborDistanceToGoal = neighbor.DistanceTo(goal); //we want this to be small
        double currentDistanceToGoal = current.DistanceTo(goal);
        double currentDistanceToNeighbor = current.DistanceTo(neighbor);
        double angle = //we want this to be small
            double.RadiansToDegrees(
                Math.Acos((currentDistanceToGoal * currentDistanceToGoal +
                    currentDistanceToNeighbor * currentDistanceToNeighbor -
                    neighborDistanceToGoal * neighborDistanceToGoal) /
                          (2 * currentDistanceToGoal * currentDistanceToNeighbor)));

        //double distanceRating = 100 - neighborDistanceToGoal / _totalDistance * 100;
        double angleRating = 100 - (angle < 180 ? angle / 180 : (360 - angle) / 180) * 100;
        double speedRating = speed * 1.0 / _maxSpeed * 100;
        //double distanceSpeedRating =  ((totalDistance / _maxSpeed) / (neighborDistanceToGoal / speed)) * 100;

        return (int)-(speedRating + angleRating * 1.4);
    }
}