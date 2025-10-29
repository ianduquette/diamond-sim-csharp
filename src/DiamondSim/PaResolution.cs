namespace DiamondSim;

/// <summary>
/// Outcome tag enum for easy switching in formatters and box score logic.
/// </summary>
public enum OutcomeTag {
    K,
    BB,
    HBP,
    Single,
    Double,
    Triple,
    HR,
    ROE,
    SF,
    DP,
    TP,
    InPlayOut
}

/// <summary>
/// Represents a single runner's movement during a plate appearance.
/// </summary>
/// <param name="FromBase">0=batter, 1=first, 2=second, 3=third (pre-play position)</param>
/// <param name="ToBase">1-4 where 4=home/scored</param>
/// <param name="Scored">True if ToBase==4</param>
/// <param name="WasForced">True if runner was forced to advance</param>
public sealed record RunnerMove(
    int FromBase,
    int ToBase,
    bool Scored,
    bool WasForced
);

/// <summary>
/// Represents the state of runners on base.
/// </summary>
/// <param name="OnFirst">Whether a runner is on first base.</param>
/// <param name="OnSecond">Whether a runner is on second base.</param>
/// <param name="OnThird">Whether a runner is on third base.</param>
public sealed record BaseState(
    bool OnFirst,
    bool OnSecond,
    bool OnThird
);

/// <summary>
/// Represents optional flags for special plate appearance outcomes.
/// </summary>
/// <param name="IsDoublePlay">Whether the plate appearance resulted in a double play.</param>
/// <param name="IsSacFly">Whether the plate appearance was a sacrifice fly.</param>
public sealed record PaFlags(
    bool IsDoublePlay,
    bool IsSacFly
);

/// <summary>
/// Represents the complete resolution of a plate appearance, including outs, runs, and base state changes.
/// </summary>
/// <param name="OutsAdded">The number of outs recorded on this plate appearance (0-3).</param>
/// <param name="RunsScored">The number of runs scored on this plate appearance.</param>
/// <param name="NewBases">The resulting base state after the plate appearance.</param>
/// <param name="Type">The type of plate appearance outcome.</param>
/// <param name="Tag">Outcome tag enum for easy switching in formatters and box score logic.</param>
/// <param name="Flags">Optional flags for special outcomes (double play, sacrifice fly, etc.).</param>
/// <param name="HadError">Whether the play involved a fielding error.</param>
/// <param name="RbiForBatter">Explicit RBI count for the batter (0-4), respecting ROE=0, BL BB/HBP=1, SF=1, HR=all rules.</param>
/// <param name="AdvanceOnError">Which runners advanced specifically due to error (null if no error). Flags correspond to the starting base the runner occupied before the play.</param>
/// <param name="BasesAtThirdOut">Snapshot of base occupancy at the instant the third out occurred (null if PA doesn't end half). This captures which runners were still on base at the moment of the third out, excluding any runners who scored or were retired before the third out. Used for accurate LOB (Left On Base) computation.</param>
/// <param name="Moves">Detailed list of runner movements for play-by-play logging.</param>
public sealed record PaResolution(
    int OutsAdded,
    int RunsScored,
    BaseState NewBases,
    PaType Type,
    OutcomeTag Tag,
    PaFlags? Flags = null,
    bool HadError = false,
    int RbiForBatter = 0,
    BaseState? AdvanceOnError = null,
    BaseState? BasesAtThirdOut = null,
    IReadOnlyList<RunnerMove>? Moves = null
);
