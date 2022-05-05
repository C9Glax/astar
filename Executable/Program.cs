Logging.Logger logger = new Logging.Logger(Logging.LogType.Console, Logging.loglevel.DEBUG);
Dictionary<UInt64, Graph.Node> nodes = OpenStreetMap_Importer.Importer.Import(ref logger);
new astar.Astar(nodes, ref logger);