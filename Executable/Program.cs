using GeoGraph;
using Logging;
using astar;
using OSM_XML_Importer;
using OSM_Landmarks;

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
            logger.Log(LogLevel.INFO, "File {0} does not exist.", xmlPath);
            throw new FileNotFoundException(xmlPath);
        }
        break;
    case 2:
        xmlPath = args[0];
        if (!File.Exists(xmlPath))
        {
            logger.Log(LogLevel.INFO, "File {0} does not exist.", xmlPath);
            throw new FileNotFoundException(xmlPath);
        }
        if (confirmation.Contains(args[1].ToLower()))
            onlyJunctions = true;
        else
            onlyJunctions = false;
        break;
    default:
        logger.Log(LogLevel.INFO, "Invalid Arguments.");
        logger.Log(LogLevel.INFO, "Arguments can be:");
        logger.Log(LogLevel.INFO, "arg0 Path to file: string");
        logger.Log(LogLevel.INFO, "arg1 onlyJunctions: 'yes', '1', 'true'");
        return;
}
logger.Log(LogLevel.INFO, "Loading Graph");
Graph graph = OSM_XML_Importer.Importer.Import(xmlPath, onlyJunctions, logger);
logger.Log(LogLevel.INFO, "Loading Landmarks");
Landmarks landmarks = OSM_Landmarks.Importer.Import(xmlPath, logger);
logger.Log(LogLevel.INFO, "Everything loaded.");

Route _route;
Node? n1, n2;
do
{
    logger.Log(LogLevel.INFO, "Press ESC to quit.");
    logger.Log(LogLevel.INFO, "Press R for path-calculation between 2 random Nodes.");
    logger.Log(LogLevel.INFO, "Press C for path-calculation between 2 input coordinates.");
    logger.Log(LogLevel.INFO, "Press A for path-calculation between 2 addresses.");
    logger.Log(LogLevel.INFO, "Press L to List all addresses.");
    logger.Log(LogLevel.INFO, "Press N to get Information to Node-Id.");

    ConsoleKey mode = Console.ReadKey().Key;
    switch (mode)
    {
        case ConsoleKey.Escape:
            return;
        case ConsoleKey.N:
            logger.Log(LogLevel.INFO, "Enter Node-ID:");
            ulong id = Convert.ToUInt64(Console.ReadLine());
            n1 = graph.GetNode(id);
            if(n1 != null)
            {
                logger.Log(LogLevel.INFO, "{0}: {1}", id, n1.ToString());
            }
            break;
        case ConsoleKey.R:
            do
            {
                Dictionary<ulong, Node> temp = graph.GetRandomNodes(2);
                n1 = temp.Values.ToArray()[0];
                n2 = temp.Values.ToArray()[1];
                Console.WriteLine();
                Console.WriteLine();
                _route = new Astar().FindPath(n1, n2, logger);
            } while (!_route.routeFound);
            break;
        case ConsoleKey.C:
            logger.Log(LogLevel.INFO, "Enter Coordinates for Node 1:");
            float lat1 = Convert.ToSingle(Console.ReadLine());
            float lon1 = Convert.ToSingle(Console.ReadLine());
            logger.Log(LogLevel.INFO, "Enter Coordinates for Node 2:");
            float lat2 = Convert.ToSingle(Console.ReadLine());
            float lon2 = Convert.ToSingle(Console.ReadLine());
            n1 = graph.ClosestNodeToCoordinates(lat1, lon1);
            n2 = graph.ClosestNodeToCoordinates(lat2, lon2);
            _route = new Astar().FindPath(n1, n2, logger);
            break;
        case ConsoleKey.A:
            logger.Log(LogLevel.INFO, "Enter Address 1:");
            List<Address> a1list = landmarks.GetAddressesForQuery(Console.ReadLine());
            logger.Log(LogLevel.INFO, "Select Address 1:");
            for (int i = 0; i < a1list.Count; i++)
            {
                logger.Log(LogLevel.INFO, "{0}: {1}", i, a1list[i].ToString());
            }
            Address a1 = a1list[Convert.ToInt32(Console.ReadLine())];
            if (graph.ContainsNode(a1.locationId))
            {
                logger.Log(LogLevel.INFO, "Address already in graph");
                n1 = graph.GetNode(a1.locationId);
            }
            else
            {
                n1 = graph.ClosestNodeToCoordinates(a1.lat, a1.lon);
                logger.Log(LogLevel.INFO, "Closest Node\n{0}", n1);
            }

            logger.Log(LogLevel.INFO, "Enter Address 2:");
            List<Address> a2list = landmarks.GetAddressesForQuery(Console.ReadLine());
            logger.Log(LogLevel.INFO, "Select Address 2:");
            for (int i = 0; i < a2list.Count; i++)
            {
                logger.Log(LogLevel.INFO, "{0}: {1}", i, a2list[i].ToString());
            }
            Address a2 = a2list[Convert.ToInt32(Console.ReadLine())];


            if (graph.ContainsNode(a2.locationId))
            {
                logger.Log(LogLevel.INFO, "Address already in graph");
                n2 = graph.GetNode(a2.locationId);
            }
            else
            {
                n2 = graph.ClosestNodeToCoordinates(a2.lat, a2.lon);
                logger.Log(LogLevel.INFO, "Closest Node\n{0}", n2);
            }


            _route = new Astar().FindPath(n1, n2, logger);
            break;
        case ConsoleKey.L:
            foreach (Address a in landmarks.addresses)
                logger.Log(LogLevel.INFO, a.ToString());
            break;

        default:
            Console.Clear();
            break;
    }
} while (true);