using Wolfgang.Conflict.Core.Catalog;

namespace Wolfgang.Conflict.Core.Combat;

/// <summary>
/// Per-terrain cover values (in to-hit percentage points subtracted from the
/// attacker's chance to hit). Open ground gives no cover; woods, urban, and
/// mountains progressively harder to hit a defender on/in.
/// </summary>
public static class TerrainCover
{
    /// <summary>To-hit percentage point reduction provided by occupying <paramref name="terrain"/>.</summary>
    public static int CoverFor(Terrain terrain) => terrain switch
    {
        Terrain.Plain => 0,
        Terrain.Hills => 5,
        Terrain.Forest => 15,
        Terrain.Urban => 20,
        Terrain.Mountain => 25,
        Terrain.Water => 0,
        _ => 0
    };
}
