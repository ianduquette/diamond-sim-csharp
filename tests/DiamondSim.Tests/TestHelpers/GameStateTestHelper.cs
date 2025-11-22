namespace DiamondSim.Tests.TestHelpers;

/// <summary>
/// Shared helper methods for creating GameState objects in tests with sensible defaults.
/// </summary>
public static class GameStateTestHelper {
    // Default constants
    private const int DefaultBalls = 0;
    private const int DefaultStrikes = 0;
    private const int DefaultAwayBattingOrderIndex = 0;
    private const int DefaultHomeBattingOrderIndex = 0;

    /// <summary>
    /// Creates a GameState with sensible defaults and optional overrides.
    /// Automatically sets offense/defense based on inning half if not specified.
    /// </summary>
    public static GameState CreateGameState(
        int balls = DefaultBalls,
        int strikes = DefaultStrikes,
        int inning = 1,
        InningHalf half = InningHalf.Top,
        int outs = 0,
        bool onFirst = false,
        bool onSecond = false,
        bool onThird = false,
        int awayScore = 0,
        int homeScore = 0,
        int awayBattingOrderIndex = DefaultAwayBattingOrderIndex,
        int homeBattingOrderIndex = DefaultHomeBattingOrderIndex,
        Team? offense = null,
        Team? defense = null,
        bool isFinal = false,
        int awayEarnedRuns = 0,
        int awayUnearnedRuns = 0,
        int homeEarnedRuns = 0,
        int homeUnearnedRuns = 0
    ) {
        // Default offense/defense based on half if not specified
        var actualOffense = offense ?? (half == InningHalf.Top ? Team.Away : Team.Home);
        var actualDefense = defense ?? (half == InningHalf.Top ? Team.Home : Team.Away);

        return new GameState(
            balls: balls,
            strikes: strikes,
            inning: inning,
            half: half,
            outs: outs,
            onFirst: onFirst,
            onSecond: onSecond,
            onThird: onThird,
            awayScore: awayScore,
            homeScore: homeScore,
            awayBattingOrderIndex: awayBattingOrderIndex,
            homeBattingOrderIndex: homeBattingOrderIndex,
            offense: actualOffense,
            defense: actualDefense,
            isFinal: isFinal,
            awayEarnedRuns: awayEarnedRuns,
            awayUnearnedRuns: awayUnearnedRuns,
            homeEarnedRuns: homeEarnedRuns,
            homeUnearnedRuns: homeUnearnedRuns
        );
    }

    /// <summary>
    /// Creates an initial game state (start of game, top of 1st, 0-0).
    /// </summary>
    public static GameState CreateInitialState() {
        return CreateGameState();
    }

    /// <summary>
    /// Records innings 1-8 (or 1-N) with 0 runs for both teams, plus top of inning N+1 with 0 runs.
    /// This simulates N.5 innings (e.g., 8.5 innings = through top of 9th).
    /// Useful for setting up tests that need a clean line score before testing 9th inning scenarios.
    /// </summary>
    /// <param name="scorekeeper">The scorekeeper to record innings on</param>
    /// <param name="throughInning">The inning to record through (default 8, meaning 8.5 innings)</param>
    public static void RecordInningsThroughTopOf(InningScorekeeper scorekeeper, int throughInning = 8) {
        for (int i = 0; i < throughInning + 1; i++) {
            scorekeeper.LineScore.RecordInning(Team.Away, 0);
            if (i < throughInning) {
                scorekeeper.LineScore.RecordInning(Team.Home, 0);
            }
        }
    }
}
