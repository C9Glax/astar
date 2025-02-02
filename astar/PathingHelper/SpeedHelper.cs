﻿using OSM_Graph.Enums;

namespace astar.PathingHelper;

internal static class SpeedHelper
{
    public static byte GetSpeed(OSM_Graph.Way way, bool car = true)
    {
        byte maxspeed = way.GetMaxSpeed();
        if (maxspeed != 0)
            return (byte)(maxspeed * 0.85);
        HighwayType highwayType = way.GetHighwayType();
        return car ? SpeedCar[highwayType] : SpeedPedestrian[highwayType];
    }

    public static byte GetTheoreticalMaxSpeed(bool car = true)
    {
        return car ? SpeedCar.MaxBy(s => s.Value).Value : SpeedPedestrian.MaxBy(s => s.Value).Value;
    }

    private static readonly Dictionary<HighwayType, byte> SpeedPedestrian = new() {
        { HighwayType.NONE, 0 },
        { HighwayType.motorway, 0 },
        { HighwayType.trunk, 0 },
        { HighwayType.primary, 0 },
        { HighwayType.secondary, 0 },
        { HighwayType.tertiary, 0 },
        { HighwayType.unclassified, 1 },
        { HighwayType.residential, 3 },
        { HighwayType.motorway_link, 0 },
        { HighwayType.trunk_link, 0 },
        { HighwayType.primary_link, 0 },
        { HighwayType.secondary_link, 0 },
        { HighwayType.tertiary_link, 0 },
        { HighwayType.living_street, 5 },
        { HighwayType.service, 2 },
        { HighwayType.pedestrian, 5 },
        { HighwayType.track, 0 },
        { HighwayType.bus_guideway, 0 },
        { HighwayType.escape, 0 },
        { HighwayType.raceway, 0 },
        { HighwayType.road, 3 },
        { HighwayType.busway, 0 },
        { HighwayType.footway, 4 },
        { HighwayType.bridleway, 1 },
        { HighwayType.steps, 2 },
        { HighwayType.corridor, 3 },
        { HighwayType.path, 4 },
        { HighwayType.cycleway, 2 },
        { HighwayType.construction, 0 }
    };

    private static readonly Dictionary<HighwayType, byte> SpeedCar = new() {
        { HighwayType.NONE, 0 },
        { HighwayType.motorway, 120 },
        { HighwayType.trunk, 80 },
        { HighwayType.primary, 70 },
        { HighwayType.secondary, 70 },
        { HighwayType.tertiary, 70 },
        { HighwayType.unclassified, 30 },
        { HighwayType.residential, 10 },
        { HighwayType.motorway_link, 70 },
        { HighwayType.trunk_link, 50 },
        { HighwayType.primary_link, 50 },
        { HighwayType.secondary_link, 50 },
        { HighwayType.tertiary_link, 40 },
        { HighwayType.living_street, 5 },
        { HighwayType.service, 0 },
        { HighwayType.pedestrian, 0 },
        { HighwayType.track, 0 },
        { HighwayType.bus_guideway, 0 },
        { HighwayType.escape, 0 },
        { HighwayType.raceway, 0 },
        { HighwayType.road, 30 },
        { HighwayType.busway, 0 },
        { HighwayType.footway, 0 },
        { HighwayType.bridleway, 0 },
        { HighwayType.steps, 0 },
        { HighwayType.corridor, 0 },
        { HighwayType.path, 0 },
        { HighwayType.cycleway, 0 },
        { HighwayType.construction, 0 }
    };
}