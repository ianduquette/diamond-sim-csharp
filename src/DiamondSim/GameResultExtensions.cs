namespace DiamondSim;

/// <summary>
/// Extension methods for GameResult.
/// </summary>
public static class GameResultExtensions {
    /// <summary>
    /// Converts a GameResult to a formatted console report string.
    /// </summary>
    public static string ToConsoleReport(this GameResult result) {
        var formatter = new GameReportFormatter(result);
        return formatter.FormatReport();
    }
}
