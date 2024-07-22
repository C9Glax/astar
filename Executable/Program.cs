using System.Diagnostics;
using System.Globalization;
using astar;
using GlaxArguments;
using GlaxLogger;
using Microsoft.Extensions.Logging;
using OSM_Regions;

Logger logger = new(LogLevel.Debug, consoleOut: Console.Out);

Argument pathArg = new (["-p", "--path"], 1, "Path to OSM-XML-File");
Argument regionArg = new (["-r", "--regionSize"], 1, "Region-Size");
Argument importPathArg = new (["-i", "--importPath"], 1, "Region-Directory");
Argument routeCoordinateArg = new(["-c", "--route", "--coordinates"], 4, "Start and end coordinates");

ArgumentFetcher af = new ([pathArg, regionArg, importPathArg, routeCoordinateArg]);

Dictionary<Argument, string[]> arguments = af.Fetch(args);

if (!arguments.ContainsKey(regionArg))
{
    PrintUsage();
    return;
}
if (!float.TryParse(arguments[regionArg][0], NumberFormatInfo.InvariantInfo, out float regionSize))
{
    logger.LogError($"Failed to parse region Size from input w{arguments[regionArg][0]}");
    return;
}

if (!arguments.ContainsKey(routeCoordinateArg))
{
    PrintUsage();
    return;
}
if (!float.TryParse(arguments[routeCoordinateArg][0], NumberFormatInfo.InvariantInfo, out float startLat) ||
    !float.TryParse(arguments[routeCoordinateArg][1], NumberFormatInfo.InvariantInfo, out float startLon) ||
    !float.TryParse(arguments[routeCoordinateArg][2], NumberFormatInfo.InvariantInfo, out float endLat) ||
    !float.TryParse(arguments[routeCoordinateArg][3], NumberFormatInfo.InvariantInfo, out float endLon) )
{
    logger.LogError($"Failed to parse start/end coordinates.");
    return;
}

string? importPath = null;
if (arguments.TryGetValue(importPathArg, out string[]? importPathVal))
{
    importPath = importPathVal[0];
}

if (arguments.TryGetValue(pathArg, out string[]? pathValue))
{
    if(!File.Exists(pathValue[0]))
    {
        logger.LogError($"File doesn't exist {pathValue[0]}");
        PrintUsage();
        return;
    }

    Converter converter = new (regionSize, importPath, logger: logger);
    converter.SplitOsmExportIntoRegionFiles(pathValue[0]);
}

Route route = Astar.FindPath(startLat, startLon, endLat, endLon, regionSize, importPath, logger);
if(route.RouteFound)
    Console.WriteLine($"{string.Join("\n", route.Steps)}\n" +
                      $"Distance: {route.Distance:000000.00}m");
else
    Console.WriteLine("No route found.");

Console.WriteLine($"Visited Nodes: {route.Graph.Nodes.Values.Count(node => node.Previous is not null)}");


void PrintUsage()
{
    Console.WriteLine($"Usage: {Process.GetCurrentProcess().MainModule?.FileName} <-r regionSize> <-c startLat startLon endLat endLon> <options>\n" +
                      $"Options:\n" +
                      $"\t-h onlyHighways\n" +
                      $"\t-p Path to OSM-XML file to split into regions");
}