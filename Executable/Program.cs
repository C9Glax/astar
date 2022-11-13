using Graph;
using Logging;
using astar;
using OSM_XML_Importer;

Logger logger = new (LogType.CONSOLE, LogLevel.DEBUG);
Graph.Graph graph = Importer.Import(@"C:\Users\glax\Downloads\oberbayern-latest.osm", true, logger);
logger.level = LogLevel.DEBUG;

Random r = new();
Route _route;
Node n1, n2;
do
{
    do
    {
        n1 = graph.NodeAtIndex(r.Next(0, graph.GetNodeCount() - 1));
        n2 = graph.NodeAtIndex(r.Next(0, graph.GetNodeCount() - 1));
        _route = new Astar().FindPath(graph, n1, n2, logger);
    } while (!_route.routeFound);
    logger.Log(LogLevel.INFO, "Press Enter to find new path.");
} while (Console.ReadKey().Key.Equals(ConsoleKey.Enter));