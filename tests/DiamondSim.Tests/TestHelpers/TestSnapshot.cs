namespace DiamondSim.Tests.TestHelpers;

/// <summary>
/// Represents a snapshot of game state for testing purposes.
/// Includes all relevant game situation fields but excludes count (balls/strikes)
/// to enable accurate test assertions without false positives.
/// </summary>
/// <remarks>
/// This helper exists because GameState.Equals() only compares balls/strikes,
/// which can lead to tests passing when game state is actually incorrect.
/// </remarks>
public sealed record TestSnapshot(
    int Inning,
    InningHalf Half,
    int Outs,
    bool OnFirst,
    bool OnSecond,
    bool OnThird,
    int AwayScore,
    int HomeScore,
    Team Offense,
    Team Defense,
    bool IsFinal
);

/// <summary>
/// Extension methods for GameState testing.
/// </summary>
public static class GameStateTestExtensions {
    /// <summary>
    /// Creates a TestSnapshot from the current GameState for test assertions.
    /// </summary>
    public static TestSnapshot ToTestSnapshot(this GameState state) {
        return new TestSnapshot(
            state.Inning,
            state.Half,
            state.Outs,
            state.OnFirst,
            state.OnSecond,
            state.OnThird,
            state.AwayScore,
            state.HomeScore,
            state.Offense,
            state.Defense,
            state.IsFinal
        );
    }
}
