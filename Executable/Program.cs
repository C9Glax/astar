//test
Logging.Logger logger = new Logging.Logger(Logging.LogType.Console, Logging.loglevel.DEBUG);
Dictionary<UInt64, Graph.Node> nodes = OpenStreetMap_Importer.Importer.Import(logger);
new astar.Astar(nodes, logger);