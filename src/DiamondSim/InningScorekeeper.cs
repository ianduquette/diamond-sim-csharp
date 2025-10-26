namespace DiamondSim;

/// <summary>
/// Manages deterministic state transitions for applying plate appearance results to game state.
/// This class handles runs, outs, bases, half-inning transitions, walk-offs, and extra innings.
/// Also tracks line score (runs per inning) and LOB (left on base) statistics.
/// </summary>
/// <remarks>
/// CRITICAL: This class performs purely deterministic bookkeeping with NO randomness.
/// All random outcomes must occur upstream in the at-bat and ball-in-play resolution components.
/// </remarks>
public class InningScorekeeper {
    /// <summary>
    /// Tracks runs scored per inning for both teams.
    /// </summary>
    public LineScore LineScore { get; } = new LineScore();

    /// <summary>
    /// Tracks player statistics (batting and pitching) for both teams.
    /// </summary>
    public BoxScore BoxScore { get; } = new BoxScore();

    /// <summary>
    /// Tracks left on base (LOB) per half-inning for the away team.
    /// </summary>
    public List<int> AwayLOB { get; } = new List<int>();

    /// <summary>
    /// Tracks left on base (LOB) per half-inning for the home team.
    /// </summary>
    public List<int> HomeLOB { get; } = new List<int>();

    /// <summary>
    /// Total LOB for the away team (sum of all half-innings).
    /// </summary>
    public int AwayTotalLOB => AwayLOB.Sum();

    /// <summary>
    /// Total LOB for the home team (sum of all half-innings).
    /// </summary>
    public int HomeTotalLOB => HomeLOB.Sum();

    /// <summary>
    /// Running total of runs scored in the current half-inning.
    /// Reset to 0 on half-inning transition.
    /// </summary>
    private int _currentHalfInningRuns = 0;
    /// <summary>
    /// Applies a plate appearance resolution to the current game state, returning an updated state.
    /// </summary>
    /// <param name="state">The current game state before the plate appearance.</param>
    /// <param name="resolution">The resolved outcome of the plate appearance.</param>
    /// <returns>A new GameState instance with the plate appearance result applied.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the inning exceeds 99 (safety guard against infinite loops).
    /// </exception>
    /// <remarks>
    /// State transitions occur in this critical order:
    /// 1. Apply runs to batting team's score
    /// 2. Apply outs (increment out count)
    /// 3. Apply bases (update base state from resolution)
    /// 4. Advance lineup (increment batting order index, wrap at 9)
    /// 5. Check walk-off (bottom half, inning ≥ 9, home leads → set IsFinal=true)
    /// 6. Check half close (outs ≥ 3 → perform half-inning transition)
    /// </remarks>
    public GameState ApplyPlateAppearance(GameState state, PaResolution resolution) {
        // Safety guard: prevent infinite loops in extra innings
        if (state.Inning > 99) {
            throw new InvalidOperationException(
                $"Game exceeded maximum inning limit (99). Current inning: {state.Inning}. " +
                "This indicates a potential infinite loop in the game simulation.");
        }

        // Get current batter's lineup position before advancing
        int batterLineupPosition = state.GetBattingOrderIndex();

        // Create new state instance for immutable updates
        var newState = new GameState(
            balls: 0,  // Reset count for next PA
            strikes: 0,
            inning: state.Inning,
            half: state.Half,
            outs: state.Outs,
            onFirst: state.OnFirst,
            onSecond: state.OnSecond,
            onThird: state.OnThird,
            awayScore: state.AwayScore,
            homeScore: state.HomeScore,
            awayBattingOrderIndex: state.AwayBattingOrderIndex,
            homeBattingOrderIndex: state.HomeBattingOrderIndex,
            offense: state.Offense,
            defense: state.Defense,
            isFinal: state.IsFinal
        );

        // 1. Apply runs to batting team's score
        if (newState.Offense == Team.Away) {
            newState.AwayScore += resolution.RunsScored;
        }
        else {
            newState.HomeScore += resolution.RunsScored;
        }

        // Track runs for current half-inning (for line score)
        _currentHalfInningRuns += resolution.RunsScored;

        // 2. Apply outs
        newState.Outs += resolution.OutsAdded;

        // 3. Apply bases
        newState.OnFirst = resolution.NewBases.OnFirst;
        newState.OnSecond = resolution.NewBases.OnSecond;
        newState.OnThird = resolution.NewBases.OnThird;

        // Track box score statistics
        // Determine if batter scored (true for HR, false otherwise in v0.2 simplification)
        bool batterScored = resolution.Type == PaType.HomeRun;

        // Increment batter stats
        BoxScore.IncrementBatterStats(
            team: state.Offense,
            lineupPosition: batterLineupPosition,
            paType: resolution.Type,
            runsScored: resolution.RunsScored,
            batterScored: batterScored
        );

        // Increment pitcher stats (using pitcher ID 0 for v0.2 - single pitcher per team)
        BoxScore.IncrementPitcherStats(
            team: state.Defense,
            pitcherId: 0,
            paType: resolution.Type,
            outsAdded: resolution.OutsAdded,
            runsScored: resolution.RunsScored
        );

        // 4. Advance lineup
        if (newState.Offense == Team.Away) {
            newState.AwayBattingOrderIndex = (newState.AwayBattingOrderIndex + 1) % 9;
        }
        else {
            newState.HomeBattingOrderIndex = (newState.HomeBattingOrderIndex + 1) % 9;
        }

        // 5. Check walk-off (bottom half, inning ≥ 9, home leads)
        if (newState.Half == InningHalf.Bottom &&
            newState.Inning >= 9 &&
            newState.HomeScore > newState.AwayScore) {
            // Record partial inning runs to line score (actual runs scored, not 'X')
            LineScore.RecordInning(Team.Home, _currentHalfInningRuns);
            _currentHalfInningRuns = 0;

            // Walk-off: LOB = 0 for partial inning (no 3rd out)
            HomeLOB.Add(0);

            newState.IsFinal = true;
            return newState; // Game over - return immediately
        }

        // 6. Check half close (3 outs)
        if (newState.Outs >= 3) {
            return PerformHalfInningTransition(newState);
        }

        return newState;
    }

    /// <summary>
    /// Performs the half-inning transition when 3 outs are recorded.
    /// </summary>
    /// <param name="state">The current game state with 3 or more outs.</param>
    /// <returns>A new GameState instance with the half-inning transition applied.</returns>
    /// <remarks>
    /// Transition sequence:
    /// 1. Count LOB (left on base) - occupied bases at moment of 3rd out
    /// 2. Record runs to line score for completed half-inning
    /// 3. Record LOB for completed half-inning
    /// 4. Check "skip bottom 9th" rule (if inning==9, half==Top, HomeScore > AwayScore)
    /// 5. Reset outs to 0
    /// 6. Clear all bases
    /// 7. If Half==Top: switch to Bottom, swap Offense/Defense
    /// 8. If Half==Bottom: switch to Top, increment Inning, swap Offense/Defense
    /// </remarks>
    private GameState PerformHalfInningTransition(GameState state) {
        // 1. Count LOB (left on base) at moment of 3rd out
        int lob = (state.OnFirst ? 1 : 0) +
                  (state.OnSecond ? 1 : 0) +
                  (state.OnThird ? 1 : 0);

        // 2. Record runs to line score for completed half-inning
        LineScore.RecordInning(state.Offense, _currentHalfInningRuns);
        _currentHalfInningRuns = 0; // Reset for next half

        // 3. Record LOB for completed half-inning
        if (state.Offense == Team.Away) {
            AwayLOB.Add(lob);
        }
        else {
            HomeLOB.Add(lob);
        }

        // 4. Check "skip bottom 9th" rule: if top 9th just ended and home is leading
        if (state.Inning == 9 &&
            state.Half == InningHalf.Top &&
            state.HomeScore > state.AwayScore) {
            // Game over - home team doesn't bat in bottom 9th when already leading
            // Record 'X' for home team's skipped 9th inning
            LineScore.RecordSkippedInning(Team.Home);

            var finalState = new GameState(
                balls: 0,
                strikes: 0,
                inning: state.Inning,
                half: state.Half,  // Keep as Top
                outs: 0,  // Reset outs (game is over, doesn't matter)
                onFirst: false,  // Clear bases (game is over)
                onSecond: false,
                onThird: false,
                awayScore: state.AwayScore,
                homeScore: state.HomeScore,
                awayBattingOrderIndex: state.AwayBattingOrderIndex,
                homeBattingOrderIndex: state.HomeBattingOrderIndex,
                offense: state.Offense,
                defense: state.Defense,
                isFinal: true
            );
            return finalState;
        }

        // 5-8. Reset outs and clear bases for next half, then transition
        int newInning = state.Inning;
        InningHalf newHalf;
        Team newOffense;
        Team newDefense;

        if (state.Half == InningHalf.Top) {
            // Top → Bottom: switch to bottom half, swap sides
            newHalf = InningHalf.Bottom;
            newOffense = state.Defense;
            newDefense = state.Offense;
        }
        else {
            // Bottom → Top: switch to top half, increment inning, swap sides
            newHalf = InningHalf.Top;
            newInning++;
            newOffense = state.Defense;
            newDefense = state.Offense;

            // Check if game should end after completed inning in extras
            // If inning >= 9 and away leads after bottom half, game is over
            if (state.Inning >= 9 && state.AwayScore > state.HomeScore) {
                var finalState = new GameState(
                    balls: 0,
                    strikes: 0,
                    inning: newInning,
                    half: newHalf,
                    outs: 0,
                    onFirst: false,
                    onSecond: false,
                    onThird: false,
                    awayScore: state.AwayScore,
                    homeScore: state.HomeScore,
                    awayBattingOrderIndex: state.AwayBattingOrderIndex,
                    homeBattingOrderIndex: state.HomeBattingOrderIndex,
                    offense: newOffense,
                    defense: newDefense,
                    isFinal: true
                );
                return finalState;
            }
        }

        var transitionedState = new GameState(
            balls: 0,
            strikes: 0,
            inning: newInning,
            half: newHalf,
            outs: 0,  // Reset outs
            onFirst: false,  // Clear bases
            onSecond: false,
            onThird: false,
            awayScore: state.AwayScore,
            homeScore: state.HomeScore,
            awayBattingOrderIndex: state.AwayBattingOrderIndex,
            homeBattingOrderIndex: state.HomeBattingOrderIndex,
            offense: newOffense,
            defense: newDefense,
            isFinal: false
        );

        return transitionedState;
    }
}
