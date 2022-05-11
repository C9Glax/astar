Logging.Logger logger = new Logging.Logger(Logging.LogType.CONSOLE, Logging.LogLevel.DEBUG);
Dictionary<UInt64, Graph.Node> nodes = OpenStreetMap_Importer.Importer.Import(logger);
new astar.Astar(nodes, logger);