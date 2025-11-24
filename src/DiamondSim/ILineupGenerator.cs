namespace DiamondSim;

/// <summary>
/// Interface for generating team lineups.
/// Enables dependency injection for testing and future enhancements (database rosters, etc.).
/// </summary>
public interface ILineupGenerator {
    /// <summary>
    /// Generates a lineup of 9 batters for the specified team.
    /// </summary>
    /// <param name="teamName">The name of the team</param>
    /// <param name="rng">Random number generator for shuffling/randomization</param>
    /// <returns>A list of 9 batters</returns>
    List<Batter> GenerateLineup(string teamName, IRandomSource rng);
}
