using Graph;
using Logging;
using astar;
using OSM_XML_Importer;

string[] confirmation = { "yes", "1", "true" };

Logger logger = new(LogType.CONSOLE, LogLevel.DEBUG);
string xmlPath;
bool onlyJunctions;
switch (args.Length)
{
    case 0:
        xmlPath = @"";
        onlyJunctions = true;
        break;
    case 1:
        xmlPath = args[0];
        onlyJunctions = true;
        if (!File.Exists(xmlPath))
        {
            logger.Log(LogLevel.ERROR, "File {0} does not exist.", xmlPath);
            return;
        }
        break;
    case 2:
        xmlPath = args[0];
        if (!File.Exists(xmlPath))
        {
            logger.Log(LogLevel.ERROR, "File {0} does not exist.", xmlPath);
            return;
        }
        if (confirmation.Contains(args[1].ToLower()))
            onlyJunctions = true;
        else
            onlyJunctions = false;
        break;
    default:
        logger.Log(LogLevel.ERROR, "Invalid Arguments.");
        logger.Log(LogLevel.INFO, "Arguments can be:");
        logger.Log(LogLevel.INFO, "arg0 Path to file: string");
        logger.Log(LogLevel.INFO, "arg1 onlyJunctions: 'yes', '1', 'true'");
        return;
}

Graph.Graph graph = Importer.Import(xmlPath, onlyJunctions, logger);

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
