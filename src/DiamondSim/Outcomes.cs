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
/// Represents the specific outcome when a ball is put in play.
/// </summary>
public enum BipOutcome {
    /// <summary>
    /// Ball in play results in an out (fly out, ground out, line out).
    /// </summary>
    Out,

    /// <summary>
    /// Batter reaches first base safely.
    /// </summary>
    Single,

    /// <summary>
    /// Batter reaches second base safely.
    /// </summary>
    Double,

    /// <summary>
    /// Batter reaches third base safely.
    /// </summary>
    Triple,

    /// <summary>
    /// Batter circles all bases and scores.
    /// </summary>
    HomeRun
}

/// <summary>
/// Represents which half of an inning is being played.
/// </summary>
public enum InningHalf {
    /// <summary>
    /// The top half of the inning (away team batting).
    /// </summary>
    Top,

    /// <summary>
    /// The bottom half of the inning (home team batting).
    /// </summary>
    Bottom
}

/// <summary>
/// Represents a team designation in the game.
/// </summary>
public enum Team {
    /// <summary>
    /// The away (visiting) team.
    /// </summary>
    Away,

    /// <summary>
    /// The home team.
    /// </summary>
    Home
}

/// <summary>
/// Represents the type of plate appearance outcome.
/// </summary>
public enum PaType {
    /// <summary>
    /// Strikeout.
    /// </summary>
    K,

    /// <summary>
    /// Walk (base on balls).
    /// </summary>
    BB,

    /// <summary>
    /// Hit by pitch.
    /// </summary>
    HBP,

    /// <summary>
    /// Ball in play resulting in an out.
    /// </summary>
    InPlayOut,

    /// <summary>
    /// Single.
    /// </summary>
    Single,

    /// <summary>
    /// Double.
    /// </summary>
    Double,

    /// <summary>
    /// Triple.
    /// </summary>
    Triple,

    /// <summary>
    /// Home run.
    /// </summary>
    HomeRun,

    /// <summary>
    /// Reached base on error.
    /// </summary>
    ReachOnError
}

/// <summary>
/// Represents the result of resolving a ball-in-play outcome.
/// </summary>
/// <param name="Outcome">The specific hit type or out.</param>
public sealed record BipResult(
    BipOutcome Outcome
);

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
