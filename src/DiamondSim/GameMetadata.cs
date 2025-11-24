namespace DiamondSim;

/// <summary>
/// Immutable metadata about a simulated game.
/// Contains basic game setup information (teams, seed, timestamp).
/// </summary>
public sealed class GameMetadata {
    public string HomeTeamName { get; init; }
    public string AwayTeamName { get; init; }
    public int Seed { get; init; }
    public DateTime Timestamp { get; init; }

    public GameMetadata(string homeTeamName, string awayTeamName, int seed, DateTime timestamp) {
        HomeTeamName = homeTeamName ?? throw new ArgumentNullException(nameof(homeTeamName));
        AwayTeamName = awayTeamName ?? throw new ArgumentNullException(nameof(awayTeamName));
        Seed = seed;
        Timestamp = timestamp;
    }
}
