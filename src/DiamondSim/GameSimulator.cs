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
    private readonly ILineupGenerator _lineupGenerator;
    private readonly AtBatSimulator _atBatSimulator;
    private readonly BaseRunnerAdvancement _baseRunnerAdvancement;
    protected readonly InningScorekeeper _scorekeeper;

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
    /// <param name="lineupGenerator">Optional lineup generator (defaults to DefaultLineupGenerator)</param>
    public GameSimulator(string homeTeamName, string awayTeamName, int seed, ILineupGenerator? lineupGenerator = null) {
        _homeTeamName = homeTeamName ?? throw new ArgumentNullException(nameof(homeTeamName));
        _awayTeamName = awayTeamName ?? throw new ArgumentNullException(nameof(awayTeamName));
        _seed = seed;
        _lineupGenerator = lineupGenerator ?? new DefaultLineupGenerator();

        // Create single RNG instance for entire game
        _rng = new SeededRandom(seed);

        // Initialize components
        _atBatSimulator = new AtBatSimulator(_rng);
        _baseRunnerAdvancement = new BaseRunnerAdvancement();
        _scorekeeper = new InningScorekeeper();
    }

    /// <summary>
    /// Runs a complete game simulation and returns a rich GameResult object.
    /// </summary>
    /// <returns>Complete game result with all data</returns>
    public GameResult RunGame() {
        return RunGameInternal();
    }

    private GameResult RunGameInternal() {
        // PreGame: Generate lineups using injected generator
        _homeLineup = _lineupGenerator.GenerateLineup(_homeTeamName, _rng);
        _awayLineup = _lineupGenerator.GenerateLineup(_awayTeamName, _rng);
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

        // Main game loop: simulate until game is final
        var playLogEntries = new List<PlayLogEntry>();
        while (!state.IsFinal) {
            state = SimulatePlateAppearance(state, playLogEntries);
        }

        // Build GameResult with all data
        var metadata = new GameMetadata(
            homeTeamName: _homeTeamName,
            awayTeamName: _awayTeamName,
            seed: _seed,
            timestamp: DateTime.Now
        );

        var homeLineup = new TeamLineup(_homeTeamName, _homeLineup.AsReadOnly());
        var awayLineup = new TeamLineup(_awayTeamName, _awayLineup.AsReadOnly());

        return new GameResult(
            metadata: metadata,
            boxScore: _scorekeeper.BoxScore,
            lineScore: _scorekeeper.LineScore,
            playLog: playLogEntries.AsReadOnly(),
            finalState: state,
            homeLineup: homeLineup,
            awayLineup: awayLineup,
            homeTotalLOB: _scorekeeper.HomeTotalLOB,
            awayTotalLOB: _scorekeeper.AwayTotalLOB
        );
    }

    /// <summary>
    /// Simulates a single plate appearance (creates PlayLogEntry objects).
    /// Made virtual to allow test subclasses to inject controlled data.
    /// </summary>
    protected virtual GameState SimulatePlateAppearance(GameState state, List<PlayLogEntry> playLogEntries) {
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

        // 5. Apply to game state
        var applyResult = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // 6. Create PlayLogEntry object
        var playLogEntry = new PlayLogEntry(
            inning: state.Inning,
            half: state.Half,
            batterName: batter.Name,
            pitchingTeamName: GetTeamName(state.Defense),
            resolution: resolution,
            isWalkoff: applyResult.IsWalkoff,
            outsAfter: applyResult.OutsAfter
        );
        playLogEntries.Add(playLogEntry);

        // 7. Return updated state for next iteration
        return applyResult.StateAfter;
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
}

// TODO EXTRAS: When extras are enabled, ensure no-tie rule after 9th (loop until winner).
