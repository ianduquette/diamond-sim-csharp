namespace DiamondSim;

/// <summary>
/// Result of applying a plate appearance to game state.
/// </summary>
/// <param name="StateAfter">The updated game state after applying the plate appearance.</param>
/// <param name="IsWalkoff">True if this plate appearance resulted in a walk-off win.</param>
/// <param name="OutsAfter">The number of outs after applying the plate appearance (0-3).</param>
public sealed record ApplyResult(
    GameState StateAfter,
    bool IsWalkoff,
    int OutsAfter
);

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
    /// Applies walk-off run clamping if applicable.
    /// Walk-off situations occur in the bottom of the 9th inning or later when the home team
    /// is tied or trailing.
    /// CRITICAL: Home runs are dead balls - all runs count (MLB Rule 5.06(b)(4)(A)).
    /// Non-home runs: Only the minimum runs needed to win are credited.
    /// </summary>
    /// <param name="state">The current game state before the plate appearance.</param>
    /// <param name="resolution">The resolved outcome of the plate appearance.</param>
    /// <returns>A tuple containing the clamped run count and whether walk-off was applied.</returns>
    private (int clampedRuns, bool walkoffApplied) ApplyWalkoffClamping(
        GameState state,
        PaResolution resolution) {
        // Check if walk-off situation is possible
        if (state.Half != InningHalf.Bottom || state.Inning < 9 || state.Offense != Team.Home) {
            return (resolution.RunsScored, false);
        }

        int homeScore = state.HomeScore;
        int awayScore = state.AwayScore;

        // Home team already winning - no clamping needed
        if (homeScore > awayScore) {
            return (resolution.RunsScored, false);
        }

        // Calculate runs needed to win
        int runsNeededToWin = (awayScore - homeScore) + 1;

        // CRITICAL: Home runs are dead balls - all runs count (MLB Rule 5.06(b)(4)(A))
        if (resolution.Type == PaType.HomeRun) {
            // Walk-off home run: credit all runs, game ends
            if (resolution.RunsScored >= runsNeededToWin) {
                return (resolution.RunsScored, true);  // All runs count for HR
            }
            return (resolution.RunsScored, false);  // Not enough to win yet
        }

        // Non-home run: Clamp to minimum needed (game ends when winning run scores)
        if (resolution.RunsScored >= runsNeededToWin) {
            return (runsNeededToWin, true);
        }

        // Not enough runs to win yet
        return (resolution.RunsScored, false);
    }

    /// <summary>
    /// Calculates RBI (Runs Batted In) according to official baseball rules.
    /// CRITICAL: This is called AFTER walk-off clamping, using the clamped run total.
    /// </summary>
    /// <param name="resolution">The plate appearance resolution (with potentially clamped runs).</param>
    /// <param name="clampedRuns">The actual runs credited (after walk-off clamping).</param>
    /// <param name="priorState">The game state before the plate appearance.</param>
    /// <returns>The number of RBI to credit to the batter.</returns>
    private int CalculateRbi(PaResolution resolution, int clampedRuns, GameState priorState) {
        // Rule 1: ROE = 0 RBI (MLB Rule 9.06(g))
        if (resolution.Type == PaType.ReachOnError) {
            return 0;
        }

        // Rule 2: Bases-loaded walk/HBP = 1 RBI
        if ((resolution.Type == PaType.BB || resolution.Type == PaType.HBP) &&
            priorState.OnFirst && priorState.OnSecond && priorState.OnThird) {
            return 1;
        }

        // Rule 3: Sacrifice fly = 1 RBI
        if (resolution.Flags?.IsSacFly == true) {
            return 1;
        }

        // Rule 3b: GIDP exception — no RBI on a double play even if a run scores (Rule 9.04 exception)
        if (resolution.Flags?.IsDoublePlay == true) {
            return 0;
        }

        // Rule 4: Clean BIP - credit clamped runs scored
        // Note: clampedRuns already accounts for walk-off clamping
        return clampedRuns;
    }

    /// <summary>
    /// Classifies runs as earned or unearned according to official baseball rules (v1-light simplified approach).
    /// CRITICAL: This is called AFTER walk-off clamping, using the clamped run total.
    ///
    /// V1-LIGHT SIMPLIFICATION: This uses a conservative approach where any error involvement
    /// marks all runs as unearned. Full MLB Rule 9.16 reconstruction (hypothetical inning replay
    /// without errors) is deferred to a future PRD.
    /// </summary>
    /// <param name="resolution">The plate appearance resolution.</param>
    /// <param name="clampedRuns">The actual runs credited (after walk-off clamping).</param>
    /// <returns>A tuple of (earned runs, unearned runs).</returns>
    private (int earned, int unearned) ClassifyRuns(PaResolution resolution, int clampedRuns) {
        // If no runs scored, nothing to classify
        if (clampedRuns == 0) {
            return (0, 0);
        }

        // Rule 1: ROE = all runs unearned
        if (resolution.Type == PaType.ReachOnError) {
            return (0, clampedRuns);
        }

        // Rule 2: Check for error-assisted advancement (v1-light simplified)
        // In v1-light, if ANY runner advanced on error, mark ALL runs as unearned
        // Full MLB Rule 9.16 reconstruction (hypothetical replay without errors) deferred to future PRD
        if (resolution.HadError && resolution.AdvanceOnError != null) {
            bool anyAdvanceOnError =
                resolution.AdvanceOnError.OnFirst ||
                resolution.AdvanceOnError.OnSecond ||
                resolution.AdvanceOnError.OnThird;

            if (anyAdvanceOnError) {
                return (0, clampedRuns);
            }
        }

        // Rule 3: Clean play = all runs earned
        return (clampedRuns, 0);
    }

    /// <summary>
    /// Computes the number of runners left on base (LOB) for a half-inning ending plate appearance.
    /// </summary>
    /// <param name="state">The current game state before the plate appearance.</param>
    /// <param name="resolution">The resolved outcome of the plate appearance.</param>
    /// <param name="isWalkoff">Whether this is a walk-off situation.</param>
    /// <returns>The number of runners left on base (0-3).</returns>
    /// <remarks>
    /// LOB computation rules (in priority order):
    /// 1. Walk-off situations: Always return 0 (game ends mid-play, no runners stranded)
    /// 2. Third-out snapshot available: Use BasesAtThirdOut (authoritative)
    /// 3. Fallback: Use NewBases (backward compatibility until all producers updated)
    /// </remarks>
    private int ComputeLeftOnBase(
        GameState state,
        PaResolution resolution,
        bool isWalkoff) {
        // Rule 1: Walk-off always results in LOB = 0
        if (isWalkoff) {
            return 0;
        }

        // Rule 2: Use third-out snapshot if available (authoritative)
        if (resolution.BasesAtThirdOut != null) {
            return CountOccupiedBases(resolution.BasesAtThirdOut);
        }

        // Rule 3: Fallback to legacy behavior (post-play bases)
        // This maintains backward compatibility until all producers are updated
        // Note: In production, consider logging a warning here
        return CountOccupiedBases(resolution.NewBases);
    }

    /// <summary>
    /// Counts the number of occupied bases in a BaseState.
    /// </summary>
    /// <param name="bases">The base state to count.</param>
    /// <returns>The number of occupied bases (0-3).</returns>
    private int CountOccupiedBases(BaseState bases) {
        int count = 0;
        if (bases.OnFirst) count++;
        if (bases.OnSecond) count++;
        if (bases.OnThird) count++;
        return count;
    }

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
    public ApplyResult ApplyPlateAppearance(GameState state, PaResolution resolution) {
        // Safety guard: prevent infinite loops in extra innings
        if (state.Inning > 99) {
            throw new InvalidOperationException(
                $"Game exceeded maximum inning limit (99). Current inning: {state.Inning}. " +
                "This indicates a potential infinite loop in the game simulation.");
        }

        // Get current batter's lineup position before advancing
        int batterLineupPosition = state.GetBattingOrderIndex();

        // STEP 1: CLAMP RUNS (walk-off logic if applicable)
        var (clampedRuns, walkoffApplied) = ApplyWalkoffClamping(state, resolution);

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
            isFinal: state.IsFinal,
            awayEarnedRuns: state.AwayEarnedRuns,
            awayUnearnedRuns: state.AwayUnearnedRuns,
            homeEarnedRuns: state.HomeEarnedRuns,
            homeUnearnedRuns: state.HomeUnearnedRuns
        );

        // STEP 2: Apply clamped runs to batting team's score
        if (newState.Offense == Team.Away) {
            newState.AwayScore += clampedRuns;
        }
        else {
            newState.HomeScore += clampedRuns;
        }

        // Track runs for current half-inning (for line score)
        _currentHalfInningRuns += clampedRuns;

        // STEP 3: Apply outs
        newState.Outs += resolution.OutsAdded;

        // STEP 4: Apply bases (unless walk-off applied)
        if (!walkoffApplied) {
            newState.OnFirst = resolution.NewBases.OnFirst;
            newState.OnSecond = resolution.NewBases.OnSecond;
            newState.OnThird = resolution.NewBases.OnThird;
        }
        else {
            // Walk-off: clear bases (game ends mid-play)
            newState.OnFirst = false;
            newState.OnSecond = false;
            newState.OnThird = false;
        }

        // STEP 5: Use RBI from resolution (already calculated by BaseRunnerAdvancement)
        // If resolution doesn't have RbiForBatter set (e.g., in tests), calculate it
        int rbi = resolution.RbiForBatter > 0 ? resolution.RbiForBatter : CalculateRbi(resolution, clampedRuns, state);

        // STEP 6: Classify earned/unearned runs (using clamped runs)
        var (earnedRuns, unearnedRuns) = ClassifyRuns(resolution, clampedRuns);

        // Update team earned/unearned run totals
        if (newState.Offense == Team.Away) {
            newState.AwayEarnedRuns += earnedRuns;
            newState.AwayUnearnedRuns += unearnedRuns;
        }
        else {
            newState.HomeEarnedRuns += earnedRuns;
            newState.HomeUnearnedRuns += unearnedRuns;
        }

        // STEP 7: Track box score statistics (using clamped runs and calculated RBI)
        // Determine if batter scored by checking if batter (FromBase=0) reached home (ToBase=4) in Moves
        bool batterScored = resolution.Moves?.Any(m => m.FromBase == 0 && m.ToBase == 4 && m.Scored) ?? false;

        // Increment batter stats (using clamped runs and explicit RBI)
        BoxScore.IncrementBatterStats(
            team: state.Offense,
            lineupPosition: batterLineupPosition,
            paType: resolution.Type,
            runsScored: clampedRuns,
            rbiDelta: rbi,
            batterScored: batterScored
        );

        // Increment pitcher stats (using pitcher ID 0 for v0.2 - single pitcher per team)
        BoxScore.IncrementPitcherStats(
            team: state.Defense,
            pitcherId: 0,
            paType: resolution.Type,
            outsAdded: resolution.OutsAdded,
            runsScored: clampedRuns
        );

        // STEP 8: Advance lineup
        if (newState.Offense == Team.Away) {
            newState.AwayBattingOrderIndex = (newState.AwayBattingOrderIndex + 1) % 9;
        }
        else {
            newState.HomeBattingOrderIndex = (newState.HomeBattingOrderIndex + 1) % 9;
        }

        // STEP 9: Check walk-off ending (if walk-off was applied)
        if (walkoffApplied) {
            // Record partial inning runs to line score (clamped runs scored)
            LineScore.RecordInning(Team.Home, _currentHalfInningRuns);
            _currentHalfInningRuns = 0;

            // Walk-off: LOB = 0 ALWAYS (game ends mid-play, no 3rd out)
            HomeLOB.Add(0);

            newState.IsFinal = true;
            return new ApplyResult(newState, true, 3); // Walk-off! Always 3 outs (or less if walk-off before 3rd out)
        }

        // STEP 10: Check half close (3 outs)
        if (newState.Outs >= 3) {
            var transitionedState = PerformHalfInningTransition(newState, resolution, walkoffApplied);
            return new ApplyResult(transitionedState, false, 3); // Half-inning ended with 3 outs
        }

        return new ApplyResult(newState, false, newState.Outs); // Mid-inning, return current outs
    }

    /// <summary>
    /// Performs the half-inning transition when 3 outs are recorded.
    /// </summary>
    /// <param name="state">The current game state with 3 or more outs.</param>
    /// <param name="resolution">The plate appearance resolution that ended the half-inning.</param>
    /// <param name="isWalkoff">Whether this was a walk-off situation (should always be false here, as walk-offs exit early).</param>
    /// <returns>A new GameState instance with the half-inning transition applied.</returns>
    /// <remarks>
    /// Transition sequence:
    /// 1. Count LOB (left on base) using BasesAtThirdOut snapshot if available
    /// 2. Record runs to line score for completed half-inning
    /// 3. Record LOB for completed half-inning
    /// 4. Check "skip bottom 9th" rule (if inning==9, half==Top, HomeScore > AwayScore)
    /// 5. Reset outs to 0
    /// 6. Clear all bases
    /// 7. If Half==Top: switch to Bottom, swap Offense/Defense
    /// 8. If Half==Bottom: switch to Top, increment Inning, swap Offense/Defense
    /// </remarks>
    private GameState PerformHalfInningTransition(GameState state, PaResolution resolution, bool isWalkoff) {
        // 1. Count LOB (left on base) using the new ComputeLeftOnBase method
        // This uses BasesAtThirdOut snapshot if available, otherwise falls back to NewBases
        int lob = ComputeLeftOnBase(state, resolution, isWalkoff);

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
