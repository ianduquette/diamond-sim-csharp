namespace DiamondSim;

/// <summary>
/// Pure math helper that converts at-bat outcomes and ball-in-play results into
/// complete plate appearance resolutions with runner advancement, RBI attribution,
/// and base state changes.
/// </summary>
/// <remarks>
/// This class performs NO state mutation - it only calculates and returns PaResolution.
/// All stat tracking and state updates are handled by InningScorekeeper.
/// </remarks>
public class BaseRunnerAdvancement {
    // ROE probability: small percentage of outs become errors
    private const double RoeRate = 0.05; // 5% of outs become ROE

    // DP probability when runner on first with less than 2 outs
    private const double DpRate = 0.15; // 15% of groundouts become DP

    /// <summary>
    /// Resolves a complete plate appearance into a PaResolution with runner movements,
    /// RBI attribution, and base state changes.
    /// </summary>
    /// <param name="terminal">The terminal at-bat outcome (K, BB, or BallInPlay)</param>
    /// <param name="bipOutcome">The ball-in-play outcome if terminal==BallInPlay, otherwise null</param>
    /// <param name="currentBases">Current base occupancy before the play</param>
    /// <param name="currentOuts">Current outs before the play (0-2)</param>
    /// <param name="rng">Random source for probabilistic decisions (ROE, DP)</param>
    /// <returns>Complete PaResolution with all details for state update and logging</returns>
    public PaResolution Resolve(
        AtBatTerminal terminal,
        BipOutcome? bipOutcome,
        BaseState currentBases,
        int currentOuts,
        IRandomSource rng) {

        return terminal switch {
            AtBatTerminal.Strikeout => ResolveStrikeout(currentBases, currentOuts),
            AtBatTerminal.Walk => ResolveWalk(currentBases, currentOuts),
            AtBatTerminal.HitByPitch => ResolveHitByPitch(currentBases, currentOuts),
            AtBatTerminal.BallInPlay => ResolveBallInPlay(bipOutcome!.Value, currentBases, currentOuts, rng),
            _ => throw new ArgumentException($"Unknown terminal outcome: {terminal}")
        };
    }

    private PaResolution ResolveStrikeout(BaseState currentBases, int currentOuts) {
        var moves = new List<RunnerMove>();
        var basesAtThirdOut = (currentOuts + 1 >= 3) ? currentBases : null;

        return new PaResolution(
            OutsAdded: 1,
            RunsScored: 0,
            NewBases: currentBases, // Bases unchanged on strikeout
            Type: PaType.K,
            Tag: OutcomeTag.K,
            Flags: null,
            HadError: false,
            RbiForBatter: 0,
            AdvanceOnError: null,
            BasesAtThirdOut: basesAtThirdOut,
            Moves: moves
        );
    }

    private PaResolution ResolveWalk(BaseState currentBases, int currentOuts) {
        var moves = new List<RunnerMove>();
        int runsScored = 0;
        bool newFirst = true;
        bool newSecond = currentBases.OnFirst;
        bool newThird = currentBases.OnSecond;

        // Check for bases-loaded walk (forced run)
        if (currentBases.OnFirst && currentBases.OnSecond && currentBases.OnThird) {
            runsScored = 1;
            moves.Add(new RunnerMove(3, 4, true, true)); // R3 scores (forced)
            newThird = true; // R2 forced to third
        }

        // Add runner movements for forced advances
        if (currentBases.OnFirst) {
            moves.Add(new RunnerMove(1, 2, false, true)); // R1 forced to second
        }
        if (currentBases.OnSecond && currentBases.OnFirst) {
            moves.Add(new RunnerMove(2, 3, false, true)); // R2 forced to third
        }

        var newBases = new BaseState(newFirst, newSecond, newThird);

        // RBI: bases-loaded walk = 1 RBI
        int rbi = (currentBases.OnFirst && currentBases.OnSecond && currentBases.OnThird) ? 1 : 0;

        return new PaResolution(
            OutsAdded: 0,
            RunsScored: runsScored,
            NewBases: newBases,
            Type: PaType.BB,
            Tag: OutcomeTag.BB,
            Flags: null,
            HadError: false,
            RbiForBatter: rbi,
            AdvanceOnError: null,
            BasesAtThirdOut: null, // No outs on walk
            Moves: moves
        );
    }

    private PaResolution ResolveHitByPitch(BaseState currentBases, int currentOuts) {
        var moves = new List<RunnerMove>();
        int runsScored = 0;
        bool newFirst = true;
        bool newSecond = currentBases.OnFirst;
        bool newThird = currentBases.OnSecond;

        // Check for bases-loaded HBP (forced run)
        if (currentBases.OnFirst && currentBases.OnSecond && currentBases.OnThird) {
            runsScored = 1;
            moves.Add(new RunnerMove(3, 4, true, true)); // R3 scores (forced)
            newThird = true; // R2 forced to third
        }

        // Add runner movements for forced advances
        if (currentBases.OnFirst) {
            moves.Add(new RunnerMove(1, 2, false, true)); // R1 forced to second
        }
        if (currentBases.OnSecond && currentBases.OnFirst) {
            moves.Add(new RunnerMove(2, 3, false, true)); // R2 forced to third
        }

        var newBases = new BaseState(newFirst, newSecond, newThird);

        // RBI: bases-loaded HBP = 1 RBI
        int rbi = (currentBases.OnFirst && currentBases.OnSecond && currentBases.OnThird) ? 1 : 0;

        return new PaResolution(
            OutsAdded: 0,
            RunsScored: runsScored,
            NewBases: newBases,
            Type: PaType.HBP,
            Tag: OutcomeTag.HBP,
            Flags: null,
            HadError: false,
            RbiForBatter: rbi,
            AdvanceOnError: null,
            BasesAtThirdOut: null, // No outs on HBP
            Moves: moves
        );
    }

    private PaResolution ResolveBallInPlay(
        BipOutcome bipOutcome,
        BaseState currentBases,
        int currentOuts,
        IRandomSource rng) {

        return bipOutcome switch {
            BipOutcome.Out => ResolveOut(currentBases, currentOuts, rng),
            BipOutcome.Single => ResolveSingle(currentBases, currentOuts),
            BipOutcome.Double => ResolveDouble(currentBases, currentOuts),
            BipOutcome.Triple => ResolveTriple(currentBases, currentOuts),
            BipOutcome.HomeRun => ResolveHomeRun(currentBases, currentOuts),
            _ => throw new ArgumentException($"Unknown BIP outcome: {bipOutcome}")
        };
    }

    private PaResolution ResolveOut(BaseState currentBases, int currentOuts, IRandomSource rng) {
        // Check for ROE (5% of outs become errors)
        bool isRoe = rng.NextDouble() < RoeRate;
        if (isRoe) {
            return ResolveReachOnError(currentBases, currentOuts);
        }

        // Check for double play (15% of groundouts with runner on first, less than 2 outs)
        bool canDp = currentBases.OnFirst && currentOuts < 2;
        bool isDp = canDp && rng.NextDouble() < DpRate;

        if (isDp) {
            return ResolveDoublePlay(currentBases, currentOuts);
        }

        // Check for sacrifice fly (runner on third, less than 2 outs, flyout)
        bool canSf = currentBases.OnThird && currentOuts < 2;
        bool isSf = canSf && rng.NextDouble() < 0.3; // 30% of flyouts with R3 are SF

        if (isSf) {
            return ResolveSacrificeFly(currentBases, currentOuts);
        }

        // Regular out (groundout or flyout)
        return ResolveRegularOut(currentBases, currentOuts);
    }

    private PaResolution ResolveReachOnError(BaseState currentBases, int currentOuts) {
        var moves = new List<RunnerMove>();

        // Batter reaches first on error
        moves.Add(new RunnerMove(0, 1, false, false));

        // Runners advance one base on error (simplified v1 logic)
        int runsScored = 0;
        bool newFirst = true; // Batter on first
        bool newSecond = currentBases.OnFirst;
        bool newThird = currentBases.OnSecond;

        if (currentBases.OnThird) {
            runsScored = 1;
            moves.Add(new RunnerMove(3, 4, true, false)); // R3 scores
        }
        if (currentBases.OnSecond) {
            moves.Add(new RunnerMove(2, 3, false, false)); // R2 to third
        }
        if (currentBases.OnFirst) {
            moves.Add(new RunnerMove(1, 2, false, false)); // R1 to second
        }

        var newBases = new BaseState(newFirst, newSecond, newThird);

        return new PaResolution(
            OutsAdded: 0,
            RunsScored: runsScored,
            NewBases: newBases,
            Type: PaType.ReachOnError,
            Tag: OutcomeTag.ROE,
            Flags: null,
            HadError: true,
            RbiForBatter: 0, // ROE = 0 RBI per rules
            AdvanceOnError: null,
            BasesAtThirdOut: null,
            Moves: moves
        );
    }

    private PaResolution ResolveDoublePlay(BaseState currentBases, int currentOuts) {
        var moves = new List<RunnerMove>();

        // Batter out, runner on first out
        int outsAdded = 2;
        int runsScored = 0;

        // Runners on second and third advance
        bool newFirst = false;
        bool newSecond = false;
        bool newThird = currentBases.OnSecond;

        if (currentBases.OnSecond) {
            moves.Add(new RunnerMove(2, 3, false, false)); // R2 to third
        }

        // R3 might score on DP if less than 2 outs initially
        if (currentBases.OnThird && currentOuts < 1) {
            runsScored = 1;
            moves.Add(new RunnerMove(3, 4, true, false)); // R3 scores
            newThird = currentBases.OnSecond; // R2 moves to third
        }

        var newBases = new BaseState(newFirst, newSecond, newThird);
        var basesAtThirdOut = (currentOuts + outsAdded >= 3) ? currentBases : null;

        return new PaResolution(
            OutsAdded: outsAdded,
            RunsScored: runsScored,
            NewBases: newBases,
            Type: PaType.InPlayOut,
            Tag: OutcomeTag.DP,
            Flags: new PaFlags(IsDoublePlay: true, IsSacFly: false),
            HadError: false,
            RbiForBatter: runsScored, // Credit RBI for runs scored on DP
            AdvanceOnError: null,
            BasesAtThirdOut: basesAtThirdOut,
            Moves: moves
        );
    }

    private PaResolution ResolveSacrificeFly(BaseState currentBases, int currentOuts) {
        var moves = new List<RunnerMove>();

        // R3 scores on sacrifice fly
        int runsScored = 1;
        moves.Add(new RunnerMove(3, 4, true, false)); // R3 scores

        // Other runners hold (simplified v1)
        bool newFirst = currentBases.OnFirst;
        bool newSecond = currentBases.OnSecond;
        bool newThird = false; // R3 scored

        var newBases = new BaseState(newFirst, newSecond, newThird);
        var basesAtThirdOut = (currentOuts + 1 >= 3) ? currentBases : null;

        return new PaResolution(
            OutsAdded: 1,
            RunsScored: runsScored,
            NewBases: newBases,
            Type: PaType.InPlayOut,
            Tag: OutcomeTag.SF,
            Flags: new PaFlags(IsDoublePlay: false, IsSacFly: true),
            HadError: false,
            RbiForBatter: 1, // SF = 1 RBI per rules
            AdvanceOnError: null,
            BasesAtThirdOut: basesAtThirdOut,
            Moves: moves
        );
    }

    private PaResolution ResolveRegularOut(BaseState currentBases, int currentOuts) {
        var moves = new List<RunnerMove>();

        // No runner advancement on regular out (simplified v1)
        var newBases = currentBases;
        var basesAtThirdOut = (currentOuts + 1 >= 3) ? currentBases : null;

        return new PaResolution(
            OutsAdded: 1,
            RunsScored: 0,
            NewBases: newBases,
            Type: PaType.InPlayOut,
            Tag: OutcomeTag.InPlayOut,
            Flags: null,
            HadError: false,
            RbiForBatter: 0,
            AdvanceOnError: null,
            BasesAtThirdOut: basesAtThirdOut,
            Moves: moves
        );
    }

    private PaResolution ResolveSingle(BaseState currentBases, int currentOuts) {
        var moves = new List<RunnerMove>();
        int runsScored = 0;

        // Batter to first
        moves.Add(new RunnerMove(0, 1, false, false));

        // R3 scores
        if (currentBases.OnThird) {
            runsScored++;
            moves.Add(new RunnerMove(3, 4, true, false));
        }

        // R2 to third (or scores on aggressive single - 50% chance)
        bool r2Scores = currentBases.OnSecond; // Simplified: R2 always to third on single
        if (currentBases.OnSecond) {
            moves.Add(new RunnerMove(2, 3, false, false));
        }

        // R1 to second
        bool newFirst = true; // Batter
        bool newSecond = currentBases.OnFirst;
        bool newThird = currentBases.OnSecond;

        if (currentBases.OnFirst) {
            moves.Add(new RunnerMove(1, 2, false, false));
        }

        var newBases = new BaseState(newFirst, newSecond, newThird);

        return new PaResolution(
            OutsAdded: 0,
            RunsScored: runsScored,
            NewBases: newBases,
            Type: PaType.Single,
            Tag: OutcomeTag.Single,
            Flags: null,
            HadError: false,
            RbiForBatter: runsScored,
            AdvanceOnError: null,
            BasesAtThirdOut: null,
            Moves: moves
        );
    }

    private PaResolution ResolveDouble(BaseState currentBases, int currentOuts) {
        var moves = new List<RunnerMove>();
        int runsScored = 0;

        // Batter to second
        moves.Add(new RunnerMove(0, 2, false, false));

        // R3 scores
        if (currentBases.OnThird) {
            runsScored++;
            moves.Add(new RunnerMove(3, 4, true, false));
        }

        // R2 scores
        if (currentBases.OnSecond) {
            runsScored++;
            moves.Add(new RunnerMove(2, 4, true, false));
        }

        // R1 to third
        bool newFirst = false;
        bool newSecond = true; // Batter
        bool newThird = currentBases.OnFirst;

        if (currentBases.OnFirst) {
            moves.Add(new RunnerMove(1, 3, false, false));
        }

        var newBases = new BaseState(newFirst, newSecond, newThird);

        return new PaResolution(
            OutsAdded: 0,
            RunsScored: runsScored,
            NewBases: newBases,
            Type: PaType.Double,
            Tag: OutcomeTag.Double,
            Flags: null,
            HadError: false,
            RbiForBatter: runsScored,
            AdvanceOnError: null,
            BasesAtThirdOut: null,
            Moves: moves
        );
    }

    private PaResolution ResolveTriple(BaseState currentBases, int currentOuts) {
        var moves = new List<RunnerMove>();

        // Batter to third
        moves.Add(new RunnerMove(0, 3, false, false));

        // All runners score
        int runsScored = 0;
        if (currentBases.OnThird) {
            runsScored++;
            moves.Add(new RunnerMove(3, 4, true, false));
        }
        if (currentBases.OnSecond) {
            runsScored++;
            moves.Add(new RunnerMove(2, 4, true, false));
        }
        if (currentBases.OnFirst) {
            runsScored++;
            moves.Add(new RunnerMove(1, 4, true, false));
        }

        var newBases = new BaseState(false, false, true); // Batter on third

        return new PaResolution(
            OutsAdded: 0,
            RunsScored: runsScored,
            NewBases: newBases,
            Type: PaType.Triple,
            Tag: OutcomeTag.Triple,
            Flags: null,
            HadError: false,
            RbiForBatter: runsScored,
            AdvanceOnError: null,
            BasesAtThirdOut: null,
            Moves: moves
        );
    }

    private PaResolution ResolveHomeRun(BaseState currentBases, int currentOuts) {
        var moves = new List<RunnerMove>();

        // Batter scores
        moves.Add(new RunnerMove(0, 4, true, false));

        // All runners score
        int runsScored = 1; // Batter
        if (currentBases.OnThird) {
            runsScored++;
            moves.Add(new RunnerMove(3, 4, true, false));
        }
        if (currentBases.OnSecond) {
            runsScored++;
            moves.Add(new RunnerMove(2, 4, true, false));
        }
        if (currentBases.OnFirst) {
            runsScored++;
            moves.Add(new RunnerMove(1, 4, true, false));
        }

        var newBases = new BaseState(false, false, false); // Bases empty

        return new PaResolution(
            OutsAdded: 0,
            RunsScored: runsScored,
            NewBases: newBases,
            Type: PaType.HomeRun,
            Tag: OutcomeTag.HR,
            Flags: null,
            HadError: false,
            RbiForBatter: runsScored, // HR = all runners + batter
            AdvanceOnError: null,
            BasesAtThirdOut: null,
            Moves: moves
        );
    }
}
