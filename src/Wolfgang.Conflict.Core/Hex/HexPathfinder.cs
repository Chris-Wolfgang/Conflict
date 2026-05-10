namespace Wolfgang.Conflict.Core.Hex;

/// <summary>
/// A* pathfinder over a hex grid. Generic over the neighbor-and-cost
/// function so the same algorithm serves both unweighted distance
/// queries and movement on a fully terrain-costed map.
/// </summary>
public static class HexPathfinder
{
    /// <summary>
    /// Finds the cheapest path from <paramref name="start"/> to <paramref name="goal"/>
    /// using A* with the hex distance heuristic.
    /// </summary>
    /// <param name="start">The starting hex.</param>
    /// <param name="goal">The destination hex.</param>
    /// <param name="getNeighbors">
    /// Function returning the passable neighbors of a hex along with the
    /// cost of moving from that hex to each neighbor. Costs must be
    /// non-negative integers (typically &gt;= 1) for the heuristic to remain admissible.
    /// </param>
    /// <param name="maxCost">
    /// Optional upper bound on total path cost. If the cheapest path
    /// would exceed this, the search returns <see langword="null"/>.
    /// </param>
    /// <returns>
    /// The cheapest path as a <see cref="HexPath"/>, or <see langword="null"/>
    /// if no path exists within <paramref name="maxCost"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="getNeighbors"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="getNeighbors"/> returned a negative edge cost.
    /// </exception>
    public static HexPath? FindPath
    (
        HexCoord start,
        HexCoord goal,
        Func<HexCoord, IEnumerable<(HexCoord Neighbor, int Cost)>> getNeighbors,
        int? maxCost = null
    )
    {
        ArgumentNullException.ThrowIfNull(getNeighbors);

        if (start == goal)
        {
            return new HexPath([start], 0);
        }

        var cameFrom = new Dictionary<HexCoord, HexCoord>();
        var costSoFar = new Dictionary<HexCoord, int> { [start] = 0 };
        var frontier = new PriorityQueue<HexCoord, int>();
        frontier.Enqueue(start, start.DistanceTo(goal));

        while (frontier.TryDequeue(out var current, out _))
        {
            if (current == goal)
            {
                return Reconstruct(cameFrom, costSoFar, start, goal);
            }

            var currentCost = costSoFar[current];

            foreach (var (next, stepCost) in getNeighbors(current))
            {
                if (stepCost < 0)
                {
                    throw new InvalidOperationException
                    (
                        $"Negative edge cost from {current} to {next}: {stepCost}."
                    );
                }

                var newCost = currentCost + stepCost;

                if (maxCost is { } cap && newCost > cap)
                {
                    continue;
                }

                if (costSoFar.TryGetValue(next, out var existing) && newCost >= existing)
                {
                    continue;
                }

                costSoFar[next] = newCost;
                cameFrom[next] = current;
                var priority = newCost + next.DistanceTo(goal);
                frontier.Enqueue(next, priority);
            }
        }

        return null;
    }


    private static HexPath Reconstruct
    (
        Dictionary<HexCoord, HexCoord> cameFrom,
        Dictionary<HexCoord, int> costSoFar,
        HexCoord start,
        HexCoord goal
    )
    {
        var hexes = new List<HexCoord>();
        var node = goal;

        while (node != start)
        {
            hexes.Add(node);
            node = cameFrom[node];
        }

        hexes.Add(start);
        hexes.Reverse();

        return new HexPath(hexes, costSoFar[goal]);
    }
}
