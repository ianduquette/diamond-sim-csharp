namespace DiamondSim;

/// <summary>
/// Represents the outcome of a single pitch in an at-bat.
/// </summary>
public enum PitchOutcome {
    /// <summary>
    /// Pitch was out of the strike zone and the batter did not swing.
    /// </summary>
    Ball,

    /// <summary>
    /// Pitch was in the strike zone and the batter did not swing.
    /// </summary>
    CalledStrike,

    /// <summary>
    /// Batter swung at the pitch but did not make contact.
    /// </summary>
    SwingAndMiss,

    /// <summary>
    /// Batter made contact with the pitch, resulting in a foul ball.
    /// </summary>
    Foul,

    /// <summary>
    /// Batter made contact with the pitch, putting the ball in play.
    /// </summary>
    InPlay
}

/// <summary>
/// Represents the terminal outcome of a complete at-bat.
/// </summary>
public enum AtBatTerminal {
    /// <summary>
    /// At-bat ended with three strikes (strikeout).
    /// </summary>
    Strikeout,

    /// <summary>
    /// At-bat ended with four balls (walk).
    /// </summary>
    Walk,

    /// <summary>
    /// At-bat ended with the ball being put in play.
    /// </summary>
    BallInPlay
}

/// <summary>
/// Represents the complete result of a simulated at-bat.
/// </summary>
/// <param name="Terminal">The terminal outcome (Strikeout, Walk, or BallInPlay).</param>
/// <param name="FinalCount">The final count when the at-bat ended (e.g., "3-2").</param>
/// <param name="PitchCount">The total number of pitches thrown in the at-bat.</param>
public sealed record AtBatResult(
    AtBatTerminal Terminal,
    string FinalCount,
    int PitchCount
);
