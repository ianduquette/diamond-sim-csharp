namespace DiamondSim;

/// <summary>
/// Immutable team lineup containing team name and 9 batters.
/// </summary>
public sealed class TeamLineup {
    public string TeamName { get; init; }
    public IReadOnlyList<Batter> Batters { get; init; }

    public TeamLineup(string teamName, IReadOnlyList<Batter> batters) {
        TeamName = teamName ?? throw new ArgumentNullException(nameof(teamName));
        Batters = batters ?? throw new ArgumentNullException(nameof(batters));

        if (batters.Count != 9) {
            throw new ArgumentException("Lineup must have exactly 9 batters", nameof(batters));
        }
    }
}
