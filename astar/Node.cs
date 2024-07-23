﻿namespace astar;

public class Node(float lat, float lon, Dictionary<ulong, ulong>? neighbors = null) : global::Graph.Node(lat, lon, neighbors)
{
    public ulong? PreviousNodeId = null;
    public float? Distance = null;
    public bool? PreviousIsFromStart = null;

    public static Node FromGraphNode(global::Graph.Node node) => new (node.Lat, node.Lon, node.Neighbors);

    public override string ToString()
    {
        return $"{Lat:00.000000} {Lon:000.000000} Previous {PreviousNodeId} {Distance} {(PreviousIsFromStart is not null ? PreviousIsFromStart.Value ?"Start":"End" : null)}";
    }
}