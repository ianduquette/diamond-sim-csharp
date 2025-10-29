namespace DiamondSim;

/// <summary>
/// Centralized repository of fixed play-by-play phrases per PRD Section 6.
/// All phrases are locked to prevent drift and ensure deterministic output.
/// </summary>
public static class PlayByPlayPhrases {
    // Constants for parameterless phrases
    public const string Walk = "Walk";
    public const string HitByPitch = "HBP";
    public const string WalkoffPrefix = "Walk-off: ";

    /// <summary>
    /// Returns the strikeout phrase based on whether it was looking or swinging.
    /// </summary>
    /// <param name="looking">True if the batter was called out looking, false if swinging.</param>
    /// <returns>"K looking" or "K swinging"</returns>
    public static string Strikeout(bool looking) => looking ? "K looking" : "K swinging";

    /// <summary>
    /// Returns the single phrase with the specified field.
    /// </summary>
    /// <param name="field">The field where the ball landed (LF, CF, or RF).</param>
    /// <returns>e.g., "Single to RF"</returns>
    public static string Single(string field) => $"Single to {field}";

    /// <summary>
    /// Returns the double phrase with the specified field.
    /// </summary>
    /// <param name="field">The field where the ball landed (LF, CF, or RF).</param>
    /// <returns>e.g., "Double to CF"</returns>
    public static string Double(string field) => $"Double to {field}";

    /// <summary>
    /// Returns the triple phrase with the specified field.
    /// </summary>
    /// <param name="field">The field where the ball landed (LF, CF, or RF).</param>
    /// <returns>e.g., "Triple to LF"</returns>
    public static string Triple(string field) => $"Triple to {field}";

    /// <summary>
    /// Returns the home run phrase with the specified field.
    /// </summary>
    /// <param name="field">The field where the ball landed (LF, CF, or RF).</param>
    /// <returns>e.g., "Home run to CF"</returns>
    public static string HomeRun(string field) => $"Home run to {field}";

    /// <summary>
    /// Returns the groundout phrase with the specified fielding positions.
    /// </summary>
    /// <param name="positions">The fielding positions involved (e.g., "6-3", "4-3").</param>
    /// <returns>e.g., "Groundout 6-3"</returns>
    public static string Groundout(string positions) => $"Groundout {positions}";

    /// <summary>
    /// Returns the flyout phrase with the specified field.
    /// </summary>
    /// <param name="field">The field where the ball was caught (LF, CF, or RF).</param>
    /// <returns>e.g., "Flyout to RF"</returns>
    public static string Flyout(string field) => $"Flyout to {field}";

    /// <summary>
    /// Returns the lineout phrase with the specified position.
    /// </summary>
    /// <param name="position">The fielding position (e.g., "SS", "3B").</param>
    /// <returns>e.g., "Lineout to SS"</returns>
    public static string Lineout(string position) => $"Lineout to {position}";

    /// <summary>
    /// Returns the reach on error phrase with the specified fielder position.
    /// </summary>
    /// <param name="position">The fielder position number (1-9).</param>
    /// <returns>e.g., "Reaches on E6"</returns>
    public static string ReachOnError(int position) => $"Reaches on E{position}";

    /// <summary>
    /// Returns the sacrifice fly phrase with the specified field.
    /// </summary>
    /// <param name="field">The field where the ball was caught (LF, CF, or RF).</param>
    /// <returns>e.g., "Sacrifice fly to RF"</returns>
    public static string SacrificeFly(string field) => $"Sacrifice fly to {field}";

    /// <summary>
    /// Returns the grounds into double play phrase with the specified positions.
    /// </summary>
    /// <param name="positions">The fielding positions involved (e.g., "6-4-3", "4-6-3").</param>
    /// <returns>e.g., "Grounds into DP 6-4-3"</returns>
    public static string GroundsIntoDP(string positions) => $"Grounds into DP {positions}";

    /// <summary>
    /// Returns the outs phrase for the specified number of outs.
    /// </summary>
    /// <param name="outs">The number of outs (1-3).</param>
    /// <returns>"1 out." or "2 outs." or "3 outs."</returns>
    public static string OutsPhrase(int outs) => outs == 1 ? "1 out." : $"{outs} outs.";
}
