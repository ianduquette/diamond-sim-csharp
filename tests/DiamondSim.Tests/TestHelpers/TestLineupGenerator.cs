namespace DiamondSim.Tests.TestHelpers;

/// <summary>
/// Test lineup generator that creates predictable lineups for testing.
/// Does NOT shuffle - batters are in order 1-9 for deterministic tests.
/// </summary>
public class TestLineupGenerator : ILineupGenerator {
    /// <summary>
    /// Generates a lineup of 9 batters in predictable order (no shuffling).
    /// </summary>
    public List<Batter> GenerateLineup(string teamName, IRandomSource rng) {
        var batters = new List<Batter>();
        for (int i = 1; i <= 9; i++) {
            batters.Add(new Batter($"{teamName} {i}", BatterRatings.Average));
        }
        // NO shuffling - keep in order for predictable tests
        return batters;
    }
}
