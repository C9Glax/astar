#pragma warning disable CS8600, CS8601, CS8602, CS8604, CS8629
using GeoGraph;
using Logging;
using astar;
using OSM_XML_Importer;
using OSM_Landmarks;

string[] confirmation = { "yes", "1", "true" };

Logger logger = new(LogType.CONSOLE, LogLevel.DEBUG);
string xmlPath;
bool onlyJunctions;
Way.speedType speedType;
switch (args.Length)
{
    case 0:
        xmlPath = @"";
        onlyJunctions = true;
        speedType = Way.speedType.car;
        break;
    case 1:
        xmlPath = args[0];
        onlyJunctions = true;
        speedType = Way.speedType.car;
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
        speedType = Way.speedType.car;
        break;
    case 3:
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
        if (args[2].Equals("car"))
            speedType = Way.speedType.car;
        else if (args[2].Equals("ped") || args[2].Equals("pedestrian"))
            speedType = Way.speedType.pedestrian;
        else
            return;
        break;
    default:
        logger.Log(LogLevel.INFO, "Invalid Arguments.");
        logger.Log(LogLevel.INFO, "Arguments can be:");
        logger.Log(LogLevel.INFO, "arg0 Path to file: string");
        logger.Log(LogLevel.INFO, "arg1 onlyJunctions: 'yes', '1', 'true'");
        logger.Log(LogLevel.INFO, "arg2 speedType: 'car', 'ped', 'pedestrian'");
        return;
}
logger.Log(LogLevel.INFO, "Loading Graph");
Graph graph = GraphImporter.Import(xmlPath, onlyJunctions, logger);
logger.Log(LogLevel.INFO, "Loading Landmarks");
Landmarks landmarks = LandmarksImporter.Import(xmlPath, logger);
logger.Log(LogLevel.INFO, "Everything loaded.");

Route _route;
Node? n1, n2;
Console.WriteLine("Press ESC to quit.");
Console.WriteLine("Press H to print this.");
Console.WriteLine("Press R for path-calculation between 2 random Nodes.");
Console.WriteLine("Press C for path-calculation between 2 input coordinates.");
Console.WriteLine("Press P for path-calculation between 2 addresses.");
Console.WriteLine("Press L to List all addresses.");
Console.WriteLine("Press N to get Information to Node-Id.");
Console.WriteLine("Press E to get Information to Edge-Id.");
Console.WriteLine("Press X to get Explore starting at Node-Id.");
Console.WriteLine("Press A to get Query Address.");
do
{
    ConsoleKey mode = Console.ReadKey().Key;
    switch (mode)
    {
        case ConsoleKey.Escape:
            return;
        case ConsoleKey.N:
            Console.WriteLine("Enter Node-ID:");
            ulong id = Convert.ToUInt64(Console.ReadLine());
            n1 = graph.GetNode(id);
            if(n1 != null)
            {
                Console.WriteLine("{0}: {1}", id, n1.ToString());
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
                _route = new Astar().FindPath(n1, n2, speedType, logger);
            } while (!_route.routeFound);
            break;
        case ConsoleKey.C:
            Console.WriteLine("Enter Coordinates for Node 1:");
            float lat1 = Convert.ToSingle(Console.ReadLine());
            float lon1 = Convert.ToSingle(Console.ReadLine());
            Console.WriteLine("Enter Coordinates for Node 2:");
            float lat2 = Convert.ToSingle(Console.ReadLine());
            float lon2 = Convert.ToSingle(Console.ReadLine());
            n1 = graph.ClosestNodeToCoordinates(lat1, lon1);
            n2 = graph.ClosestNodeToCoordinates(lat2, lon2);
            _route = new Astar().FindPath(n1, n2, speedType, logger);
            break;
        case ConsoleKey.P:
            Console.WriteLine("Enter Address 1:");
            List<Address> a1list = landmarks.GetAddressesForQuery(Console.ReadLine());
            Console.WriteLine("Select Address 1:");
            for (int i = 0; i < a1list.Count; i++)
            {
                Console.WriteLine("{0}: {1}", i, a1list[i].ToString());
            }
            Address a1 = a1list[Convert.ToInt32(Console.ReadLine())];
            if (graph.ContainsNode((ulong)a1.locationId))
            {
                Console.WriteLine("Address already in graph");
                n1 = graph.GetNode((ulong)a1.locationId);
            }
            else
            {
                n1 = graph.ClosestNodeToCoordinates((float)a1.lat, (float)a1.lon);
                Console.WriteLine("Closest Node {0}\n{1}", graph.GetNodeId(n1), n1);
            }

            Console.WriteLine("Enter Address 2:");
            List<Address> a2list = landmarks.GetAddressesForQuery(Console.ReadLine());
            Console.WriteLine("Select Address 2:");
            for (int i = 0; i < a2list.Count; i++)
            {
                Console.WriteLine("{0}: {1}", i, a2list[i].ToString());
            }
            Address a2 = a2list[Convert.ToInt32(Console.ReadLine())];


            if (graph.ContainsNode((ulong)a2.locationId))
            {
                Console.WriteLine("Address already in graph");
                n2 = graph.GetNode((ulong)a2.locationId);
            }
            else
            {
                n2 = graph.ClosestNodeToCoordinates((float)a2.lat, (float)a2.lon);
                Console.WriteLine("Closest Node {0}\n{1}", graph.GetNodeId(n2), n2);
            }


            _route = new Astar().FindPath(n1, n2, speedType, logger);
            break;
        case ConsoleKey.L:
            foreach (Address ad in landmarks.addresses)
                Console.WriteLine(ad.ToString());
            logger.Log(LogLevel.INFO, "{0} total Addresses", landmarks.addresses.Count);
            break;
        case ConsoleKey.E:

            break;
        case ConsoleKey.X:
            Console.WriteLine("Enter quit to quit.");
            Console.WriteLine("Enter Node-ID:");
            n1 = graph.GetNode(Convert.ToUInt64(Console.ReadLine()));
            while (true)
            {
                Console.Clear();
                if (n1 != null)
                {
                    Console.WriteLine("{0}", n1.ToString());
                }
                Console.WriteLine("Select Edge:");
                string? input = Console.ReadLine();
                if (input == null || input == "quit")
                    break;
                int selectedEdge = Convert.ToInt32(input);
                n1 = n1.edges.ToArray()[selectedEdge].neighbor;
            }
            
            break;
        case ConsoleKey.A:
            Console.WriteLine("Enter Address:");
            List<Address> alist = landmarks.GetAddressesForQuery(Console.ReadLine());
            Console.WriteLine("Select Address:");
            for (int i = 0; i < alist.Count; i++)
            {
                Console.WriteLine("{0}: {1}", i, alist[i].ToString());
            }
            Address a = alist[Convert.ToInt32(Console.ReadLine())];
            if (graph.ContainsNode((ulong)a.locationId))
            {
                Console.WriteLine("Address already in graph");
                n1 = graph.GetNode((ulong)a.locationId);
            }
            else
            {
                n1 = graph.ClosestNodeToCoordinates((float)a.lat, (float)a.lon);
                Console.WriteLine("Closest Node {0} Distance: {1}\n{2}", graph.GetNodeId(n1), Utils.DistanceBetween(n1, (float)a.lat, (float)a.lon), n1);
            }
            break;
        case ConsoleKey.H:
        default:
            Console.WriteLine("Press ESC to quit.");
            Console.WriteLine("Press H to print this.");
            Console.WriteLine("Press R for path-calculation between 2 random Nodes.");
            Console.WriteLine("Press C for path-calculation between 2 input coordinates.");
            Console.WriteLine("Press P for path-calculation between 2 addresses.");
            Console.WriteLine("Press L to List all addresses.");
            Console.WriteLine("Press N to get Information to Node-Id.");
            Console.WriteLine("Press E to get Information to Edge-Id.");
            Console.WriteLine("Press X to get Explore starting at Node-Id.");
            Console.WriteLine("Press A to get Query Address.");
            break;
    }
} while (true);