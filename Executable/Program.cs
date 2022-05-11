Logging.Logger logger = new (Logging.LogType.CONSOLE, Logging.LogLevel.DEBUG);
Dictionary<UInt64, Graph.Node> nodes = OpenStreetMap_Importer.Importer.Import(@"", logger);
logger.level = Logging.LogLevel.VERBOSE;
astar.Astar astar = new(nodes, logger);