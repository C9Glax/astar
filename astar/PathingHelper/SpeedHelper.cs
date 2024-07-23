using Graph;

namespace astar.PathingHelper;

internal static class SpeedHelper
{
    public static byte GetSpeed(Way way, bool car = true)
    {
        if (!way.Tags.TryGetValue("highway", out string? highwayTypeStr))
            return 0;
        if (!Enum.TryParse(highwayTypeStr, out HighwayType highwayType))
            return 0;
        return car ? SpeedCar[highwayType] : SpeedPedestrian[highwayType];
    }

    private static Dictionary<HighwayType, byte> SpeedPedestrian = new() {
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

    private static Dictionary<HighwayType, byte> SpeedCar = new() {
        { HighwayType.NONE, 0 },
        { HighwayType.motorway, 110 },
        { HighwayType.trunk, 100 },
        { HighwayType.primary, 80 },
        { HighwayType.secondary, 80 },
        { HighwayType.tertiary, 70 },
        { HighwayType.unclassified, 20 },
        { HighwayType.residential, 10 },
        { HighwayType.motorway_link, 50 },
        { HighwayType.trunk_link, 50 },
        { HighwayType.primary_link, 30 },
        { HighwayType.secondary_link, 25 },
        { HighwayType.tertiary_link, 25 },
        { HighwayType.living_street, 10 },
        { HighwayType.service, 0 },
        { HighwayType.pedestrian, 0 },
        { HighwayType.track, 0 },
        { HighwayType.bus_guideway, 0 },
        { HighwayType.escape, 0 },
        { HighwayType.raceway, 0 },
        { HighwayType.road, 25 },
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