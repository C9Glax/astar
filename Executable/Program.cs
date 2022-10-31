Logging.Logger logger = new (Logging.LogType.CONSOLE, Logging.LogLevel.DEBUG);
Dictionary<UInt64, Graph.Node> nodes = OpenStreetMap_Importer.Importer.Import(@"C:\Users\glax\Downloads\oberbayern-latest.osm", logger);
logger.level = Logging.LogLevel.VERBOSE;
astar.Astar astar = new(nodes, logger);
Console.ReadKey();