using System.Text;

namespace DiamondSim;

/// <summary>
/// Main game simulator that orchestrates a complete 9-inning baseball game.
/// Handles lineup generation, plate appearance loop, and play-by-play logging.
/// </summary>
public class GameSimulator {
    private readonly string _homeTeamName;
    private readonly string _awayTeamName;
    private readonly int _seed;
    private readonly IRandomSource _rng;
    private readonly AtBatSimulator _atBatSimulator;
    private readonly BaseRunnerAdvancement _baseRunnerAdvancement;
    private readonly InningScorekeeper _scorekeeper;
    private readonly List<string> _playLog;

    private List<Batter> _homeLineup = new();
    private List<Batter> _awayLineup = new();
    private Pitcher _homePitcher = null!;
    private Pitcher _awayPitcher = null!;

    /// <summary>
    /// Initializes a new game simulator with the specified teams and seed.
    /// </summary>
    /// <param name="homeTeamName">Name of the home team</param>
    /// <param name="awayTeamName">Name of the away team</param>
    /// <param name="seed">RNG seed for deterministic simulation</param>
    public GameSimulator(string homeTeamName, string awayTeamName, int seed) {
        _homeTeamName = homeTeamName ?? throw new ArgumentNullException(nameof(homeTeamName));
        _awayTeamName = awayTeamName ?? throw new ArgumentNullException(nameof(awayTeamName));
        _seed = seed;

        // Create single RNG instance for entire game
        _rng = new SeededRandom(seed);

        // Initialize components
        _atBatSimulator = new AtBatSimulator(_rng);
        _baseRunnerAdvancement = new BaseRunnerAdvancement();
        _scorekeeper = new InningScorekeeper();
        _playLog = new List<string>();
    }

    /// <summary>
    /// Runs a complete game simulation and returns the formatted report.
    /// </summary>
    /// <returns>Complete game report as text</returns>
    public string RunGame() {
        // PreGame: Generate lineups and initialize state
        _homeLineup = GenerateLineup(_homeTeamName);
        _awayLineup = GenerateLineup(_awayTeamName);
        _homePitcher = new Pitcher($"{_homeTeamName} P", PitcherRatings.Average);
        _awayPitcher = new Pitcher($"{_awayTeamName} P", PitcherRatings.Average);

        // Initialize game state: Top 1st, 0-0, bases empty, 0 outs
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 1,
            half: InningHalf.Top,
            outs: 0,
            onFirst: false,
            onSecond: false,
            onThird: false,
            awayScore: 0,
            homeScore: 0,
            awayBattingOrderIndex: 0,
            homeBattingOrderIndex: 0,
            offense: Team.Away,
            defense: Team.Home,
            isFinal: false
        );

        // Main game loop: InningInProgress until GameComplete
        while (!state.IsFinal) {
            state = SimulatePlateAppearance(state);
        }

        // GameComplete: Format and return report
        var formatter = new GameReportFormatter(
            _homeTeamName,
            _awayTeamName,
            _seed,
            DateTime.Now,
            _scorekeeper.LineScore,
            _scorekeeper.BoxScore,
            _playLog,
            state,
            _scorekeeper,
            _homeLineup,
            _awayLineup
        );

        return formatter.FormatReport();
    }

    /// <summary>
    /// Simulates a single plate appearance and updates game state.
    /// </summary>
    private GameState SimulatePlateAppearance(GameState state) {
        // 1. Get current batter and pitcher
        var batter = GetCurrentBatter(state);
        var pitcher = GetCurrentPitcher(state);

        // 2. Simulate at-bat to TERMINAL outcome only
        var atBatResult = _atBatSimulator.SimulateAtBat(pitcher.Ratings, batter.Ratings);

        // 3. Resolve ball-in-play if needed
        BipOutcome? bipOutcome = null;
        BipType? bipType = null;
        if (atBatResult.Terminal == AtBatTerminal.BallInPlay) {
            bipOutcome = BallInPlayResolver.ResolveBallInPlay(
                batter.Ratings.Power,
                pitcher.Ratings.Stuff,
                _rng
            );

            // Determine BIP type for runner advancement logic
            // Distribution: ~50% ground balls, ~40% fly balls, ~10% line drives
            bipType = DetermineBipType();
        }

        // 4. Calculate PaResolution (pure math, no mutation)
        var resolution = _baseRunnerAdvancement.Resolve(
            atBatResult.Terminal,
            bipOutcome,
            bipType,
            new BaseState(state.OnFirst, state.OnSecond, state.OnThird),
            state.Outs,
            _rng
        );

        // 5. Apply to game state (ONLY mutation point - includes clamp, LOB=0)
        var applyResult = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // 6. Build play log entry AFTER applying (so we know walk-off status and final outs)
        string playLogEntry = BuildPlayLogEntry(
            state.Inning,
            state.Half,
            batter,
            GetTeamName(state.Defense), // Use defense (pitching team) name
            resolution,
            applyResult.IsWalkoff,
            applyResult.OutsAfter
        );
        _playLog.Add(playLogEntry);

        // 7. Return updated state for next iteration
        return applyResult.StateAfter;
    }

    /// <summary>
    /// Generates a lineup of 9 batters for the specified team.
    /// </summary>
    private List<Batter> GenerateLineup(string teamName) {
        var batters = new List<Batter>();
        for (int i = 1; i <= 9; i++) {
            batters.Add(new Batter($"{teamName} {i}", BatterRatings.Average));
        }

        // Randomize order using Fisher-Yates shuffle with RNG
        Shuffle(batters);

        return batters;
    }

    /// <summary>
    /// Fisher-Yates shuffle using the game's RNG for determinism.
    /// </summary>
    private void Shuffle<T>(List<T> list) {
        int n = list.Count;
        for (int i = n - 1; i > 0; i--) {
            int j = (int)(_rng.NextDouble() * (i + 1));
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// Gets the current batter based on game state.
    /// </summary>
    private Batter GetCurrentBatter(GameState state) {
        var lineup = state.Offense == Team.Away ? _awayLineup : _homeLineup;
        int index = state.Offense == Team.Away ? state.AwayBattingOrderIndex : state.HomeBattingOrderIndex;
        return lineup[index];
    }

    /// <summary>
    /// Gets the current pitcher based on game state.
    /// </summary>
    private Pitcher GetCurrentPitcher(GameState state) {
        return state.Defense == Team.Away ? _awayPitcher : _homePitcher;
    }

    /// <summary>
    /// Gets the team name for the specified team.
    /// </summary>
    private string GetTeamName(Team team) {
        return team == Team.Home ? _homeTeamName : _awayTeamName;
    }

    /// <summary>
    /// Builds a play-by-play log entry for the plate appearance.
    /// </summary>
    private string BuildPlayLogEntry(
        int inning,
        InningHalf half,
        Batter batter,
        string pitchingTeamName,
        PaResolution resolution,
        bool isWalkoff,
        int outsAfter) {

        string halfStr = half == InningHalf.Top ? "Top" : "Bot";
        string batterName = batter.Name;

        // Walk-off prefix (now we know for sure)
        string prefix = isWalkoff ? PlayByPlayPhrases.WalkoffPrefix : "";

        // Format outcome using OutcomeTag for easy switching
        string outcome = FormatOutcome(resolution);

        // Format runner movements from resolution.Moves
        string baseRunners = FormatRunnerMoves(resolution.Moves);

        // Outs phrase using outsAfter (not pre-play outs)
        string outsPhrase = resolution.OutsAdded > 0
            ? " " + PlayByPlayPhrases.OutsPhrase(outsAfter)
            : "";

        // Build complete log entry
        var sb = new StringBuilder();
        sb.Append($"[{halfStr} {inning}] {batterName} vs {pitchingTeamName} P — {prefix}{outcome}.");

        if (!string.IsNullOrEmpty(baseRunners)) {
            sb.Append($" {baseRunners}.");
        }

        if (!string.IsNullOrEmpty(outsPhrase)) {
            sb.Append(outsPhrase);
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Formats the outcome description based on the resolution tag.
    /// </summary>
    private string FormatOutcome(PaResolution resolution) {
        // Determine field for hits (simplified: random selection)
        string field = GetRandomField();

        return resolution.Tag switch {
            OutcomeTag.K => PlayByPlayPhrases.Strikeout(looking: _rng.NextDouble() < 0.5),
            OutcomeTag.BB => PlayByPlayPhrases.Walk,
            OutcomeTag.HBP => PlayByPlayPhrases.HitByPitch,
            OutcomeTag.Single => PlayByPlayPhrases.Single(field),
            OutcomeTag.Double => PlayByPlayPhrases.Double(field),
            OutcomeTag.Triple => PlayByPlayPhrases.Triple(field),
            OutcomeTag.HR => PlayByPlayPhrases.HomeRun(field),
            OutcomeTag.ROE => PlayByPlayPhrases.ReachOnError(GetRandomFieldingPosition()),
            OutcomeTag.SF => PlayByPlayPhrases.SacrificeFly(field),
            OutcomeTag.DP => PlayByPlayPhrases.GroundsIntoDP("6-4-3"), // Standard DP
            OutcomeTag.InPlayOut => FormatRegularOut(),
            _ => "Unknown outcome"
        };
    }

    /// <summary>
    /// Determines the type of batted ball for runner advancement logic.
    /// Distribution: ~50% ground balls, ~40% fly balls, ~10% line drives
    /// </summary>
    private BipType DetermineBipType() {
        double roll = _rng.NextDouble();
        if (roll < 0.5) {
            return BipType.GroundBall;
        }
        else if (roll < 0.9) {
            return BipType.FlyBall;
        }
        else {
            return BipType.LineDrive;
        }
    }

    /// <summary>
    /// Formats a regular out (groundout, flyout, or lineout).
    /// </summary>
    private string FormatRegularOut() {
        double roll = _rng.NextDouble();
        if (roll < 0.5) {
            // Groundout
            return PlayByPlayPhrases.Groundout(GetRandomGroundoutPositions());
        }
        else if (roll < 0.9) {
            // Flyout
            return PlayByPlayPhrases.Flyout(GetRandomField());
        }
        else {
            // Lineout
            return PlayByPlayPhrases.Lineout(GetRandomInfieldPosition());
        }
    }

    /// <summary>
    /// Gets a random outfield position (LF, CF, RF).
    /// </summary>
    private string GetRandomField() {
        double roll = _rng.NextDouble();
        if (roll < 0.33) return "LF";
        if (roll < 0.67) return "CF";
        return "RF";
    }

    /// <summary>
    /// Gets random groundout fielding positions.
    /// </summary>
    private string GetRandomGroundoutPositions() {
        double roll = _rng.NextDouble();
        if (roll < 0.4) return "6-3"; // SS to 1B
        if (roll < 0.7) return "4-3"; // 2B to 1B
        if (roll < 0.9) return "5-3"; // 3B to 1B
        return "3-1"; // 1B unassisted
    }

    /// <summary>
    /// Gets a random infield position for lineouts.
    /// </summary>
    private string GetRandomInfieldPosition() {
        double roll = _rng.NextDouble();
        if (roll < 0.25) return "SS";
        if (roll < 0.5) return "2B";
        if (roll < 0.75) return "3B";
        return "1B";
    }

    /// <summary>
    /// Gets a random fielding position (1-9) for error distribution.
    /// </summary>
    private int GetRandomFieldingPosition() {
        return (int)(_rng.NextDouble() * 9) + 1; // Returns 1-9
    }

    /// <summary>
    /// Formats runner movements from the resolution.
    /// </summary>
    private string FormatRunnerMoves(IReadOnlyList<RunnerMove>? moves) {
        if (moves == null || moves.Count == 0) {
            return "";
        }

        var parts = new List<string>();
        foreach (var move in moves) {
            // Skip batter's routine advancement to first on single, etc.
            if (move.FromBase == 0 && !move.Scored) {
                continue;
            }

            if (move.Scored) {
                string runnerLabel = move.FromBase == 0 ? "Batter" : $"R{move.FromBase}";
                parts.Add($"{runnerLabel} scores");
            }
            else if (move.FromBase > 0) {
                parts.Add($"R{move.FromBase} to {move.ToBase}B");
            }
        }

        return parts.Count > 0 ? string.Join(", ", parts) : "";
    }
}

// TODO EXTRAS: When extras are enabled, ensure no-tie rule after 9th (loop until winner).
