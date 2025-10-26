namespace DiamondSim;

/// <summary>
/// Tracks runs scored per inning for both teams, providing a complete line score view of the game.
/// </summary>
/// <remarks>
/// The line score shows runs scored in each inning. Special symbols:
/// - 'X' represents a skipped bottom 9th inning when the home team is already leading
/// - Walk-off partial innings show the actual runs scored (not 'X')
/// </remarks>
public class LineScore {
    /// <summary>
    /// Runs scored per inning for the away team.
    /// </summary>
    public List<int> AwayInnings { get; } = new List<int>();

    /// <summary>
    /// Runs scored per inning for the home team.
    /// Special value: -1 represents 'X' (skipped bottom 9th).
    /// </summary>
    public List<int> HomeInnings { get; } = new List<int>();

    /// <summary>
    /// Total runs scored by the away team (sum of all innings).
    /// </summary>
    public int AwayTotal => AwayInnings.Where(r => r >= 0).Sum();

    /// <summary>
    /// Total runs scored by the home team (sum of all innings, excluding 'X' entries).
    /// </summary>
    public int HomeTotal => HomeInnings.Where(r => r >= 0).Sum();

    /// <summary>
    /// Records runs scored in a completed half-inning for the specified team.
    /// </summary>
    /// <param name="team">The team that scored the runs.</param>
    /// <param name="runs">The number of runs scored in the half-inning.</param>
    public void RecordInning(Team team, int runs) {
        if (team == Team.Away) {
            AwayInnings.Add(runs);
        }
        else {
            HomeInnings.Add(runs);
        }
    }

    /// <summary>
    /// Records a skipped bottom 9th inning (represented as 'X' in the line score).
    /// </summary>
    /// <param name="team">The team whose inning was skipped (should be Home).</param>
    /// <remarks>
    /// This is used when the home team is leading after the top of the 9th inning
    /// and does not need to bat in the bottom half.
    /// Internally stored as -1, displayed as 'X'.
    /// </remarks>
    public void RecordSkippedInning(Team team) {
        if (team == Team.Home) {
            HomeInnings.Add(-1);
        }
    }

    /// <summary>
    /// Gets the runs scored in a specific inning for the specified team.
    /// </summary>
    /// <param name="team">The team to query.</param>
    /// <param name="inning">The inning number (1-based).</param>
    /// <returns>The runs scored, or -1 if the inning hasn't been played or was skipped.</returns>
    public int GetInningRuns(Team team, int inning) {
        var innings = team == Team.Away ? AwayInnings : HomeInnings;
        int index = inning - 1; // Convert to 0-based index

        if (index < 0 || index >= innings.Count) {
            return -1; // Inning not yet played
        }

        return innings[index];
    }

    /// <summary>
    /// Gets the display string for a specific inning (handles 'X' for skipped innings).
    /// </summary>
    /// <param name="team">The team to query.</param>
    /// <param name="inning">The inning number (1-based).</param>
    /// <returns>A string representation: runs as number, "X" for skipped, or "-" for not played.</returns>
    public string GetInningDisplay(Team team, int inning) {
        int runs = GetInningRuns(team, inning);

        if (runs == -1) {
            // Check if this is a skipped inning (stored as -1) vs not yet played
            var innings = team == Team.Away ? AwayInnings : HomeInnings;
            int index = inning - 1;

            if (index >= 0 && index < innings.Count && innings[index] == -1) {
                return "X"; // Skipped inning
            }
            return "-"; // Not yet played
        }

        return runs.ToString();
    }

    /// <summary>
    /// Validates that the line score totals match the expected game scores.
    /// </summary>
    /// <param name="expectedAwayScore">The expected away team score from GameState.</param>
    /// <param name="expectedHomeScore">The expected home team score from GameState.</param>
    /// <returns>True if totals match, false otherwise.</returns>
    public bool Validate(int expectedAwayScore, int expectedHomeScore) {
        return AwayTotal == expectedAwayScore && HomeTotal == expectedHomeScore;
    }
}
