using Graph;
using Logging;
using astar;

Logger logger = new (LogType.CONSOLE, LogLevel.DEBUG);
Dictionary<ulong, Node> graph = OpenStreetMap_Importer.Importer.Import(@"", true, logger);
logger.level = LogLevel.DEBUG;

Random r = new();
Route _route;
Node n1, n2;
do
{
    do
    {
        n1 = graph[graph.Keys.ElementAt(r.Next(0, graph.Count - 1))];
        n2 = graph[graph.Keys.ElementAt(r.Next(0, graph.Count - 1))];
        _route = Astar.FindPath(ref graph, n1, n2, logger);
    } while (!_route.routeFound);
    logger.Log(LogLevel.INFO, "Press Enter to find new path.");
} while (Console.ReadKey().Key.Equals(ConsoleKey.Enter));