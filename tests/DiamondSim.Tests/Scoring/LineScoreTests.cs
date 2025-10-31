namespace DiamondSim.Tests.Scoring;

/// <summary>
/// Tests for LineScore tracking and LOB (left on base) functionality in Phase 3.
/// </summary>
[TestFixture]
[Category("Scoring")]
public class LineScoreTests {
    private InningScorekeeper _scorekeeper = null!;
    private GameState _initialState = null!;

    [SetUp]
    public void Setup() {
        _scorekeeper = new InningScorekeeper();
        _initialState = new GameState(
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
    }

    [Test]
    public void LineScore_TracksRunsPerInning() {
        // Simulate top 1st: away scores 2 runs, then 3 outs
        var state = _initialState;

        // Score 2 runs
        var resolution1 = new PaResolution(
            OutsAdded: 0,
            RunsScored: 2,
            NewBases: new BaseState(false, false, false),
            Type: PaType.HomeRun,
            Tag: OutcomeTag.HR
        );
        var result1 = _scorekeeper.ApplyPlateAppearance(state, resolution1);
        state = result1.StateAfter;

        // 3 outs to end half
        for (int i = 0; i < 3; i++) {
            var outResolution = new PaResolution(
                OutsAdded: 1,
                RunsScored: 0,
                NewBases: new BaseState(false, false, false),
                Type: PaType.InPlayOut,
                Tag: OutcomeTag.InPlayOut
            );
            var result = _scorekeeper.ApplyPlateAppearance(state, outResolution);
            state = result.StateAfter;
        }

        // Verify line score recorded 2 runs for away team in inning 1
        Assert.That(_scorekeeper.LineScore.GetInningRuns(Team.Away, 1), Is.EqualTo(2));
        Assert.That(_scorekeeper.LineScore.AwayTotal, Is.EqualTo(2));
    }

    [Test]
    public void LineScore_TotalsMatchGameState() {
        // Simulate a few innings with scoring
        var state = _initialState;

        // Top 1st: Away scores 3
        state = ScoreRunsAndEndHalf(state, 3, 3);

        // Bottom 1st: Home scores 1
        state = ScoreRunsAndEndHalf(state, 1, 3);

        // Top 2nd: Away scores 0
        state = ScoreRunsAndEndHalf(state, 0, 3);

        // Bottom 2nd: Home scores 2
        state = ScoreRunsAndEndHalf(state, 2, 3);

        // Verify totals match
        Assert.That(_scorekeeper.LineScore.AwayTotal, Is.EqualTo(state.AwayScore));
        Assert.That(_scorekeeper.LineScore.HomeTotal, Is.EqualTo(state.HomeScore));
        Assert.That(_scorekeeper.LineScore.Validate(state.AwayScore, state.HomeScore), Is.True);
    }

    [Test]
    public void LOB_TrackedAtMomentOf3rdOut() {
        var state = _initialState;

        // Load bases
        var loadBases = new PaResolution(
            OutsAdded: 0,
            RunsScored: 0,
            NewBases: new BaseState(true, true, true),
            Type: PaType.BB,
            Tag: OutcomeTag.BB
        );
        var result = _scorekeeper.ApplyPlateAppearance(state, loadBases);
        state = result.StateAfter;

        // 3rd out with bases loaded - using BasesAtThirdOut snapshot
        for (int i = 0; i < 3; i++) {
            var outResolution = new PaResolution(
                OutsAdded: 1,
                RunsScored: 0,
                NewBases: new BaseState(false, false, false), // Bases cleared after play
                Type: PaType.InPlayOut,
                Tag: OutcomeTag.InPlayOut,
                BasesAtThirdOut: new BaseState(true, true, true) // Bases loaded at instant of 3rd out
            );
            var outResult = _scorekeeper.ApplyPlateAppearance(state, outResolution);
            state = outResult.StateAfter;
        }

        // Verify LOB = 3 for away team
        Assert.That(_scorekeeper.AwayLOB[0], Is.EqualTo(3));
        Assert.That(_scorekeeper.AwayTotalLOB, Is.EqualTo(3));
    }

    /// <summary>
    /// LOB (Left On Base) Tests - Testing BasesAtThirdOut snapshot functionality
    /// These tests verify that LOB is correctly computed from the base state at the instant
    /// the third out occurs, rather than the post-play base state.
    /// </summary>

    [Test]
    public void LOB_TriplePlay_BasesLoaded_EqualsThree() {
        // Arrange: Bases loaded, 0 outs, triple play
        var state = _initialState;
        state = new GameState(
            balls: 0, strikes: 0,
            inning: 5, half: InningHalf.Top, outs: 0,
            onFirst: true, onSecond: true, onThird: true,
            awayScore: 0, homeScore: 0,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 0,
            offense: Team.Away, defense: Team.Home
        );

        var resolution = new PaResolution(
            OutsAdded: 3,
            RunsScored: 0,
            NewBases: new BaseState(false, false, false),
            Type: PaType.InPlayOut,
            Tag: OutcomeTag.InPlayOut,
            BasesAtThirdOut: new BaseState(true, true, true)
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(_scorekeeper.AwayLOB[0], Is.EqualTo(3));
        Assert.That(result.StateAfter.Half, Is.EqualTo(InningHalf.Bottom));
    }

    [Test]
    public void LOB_DoublePlay_RunnerOnThird_EqualsOne() {
        // Arrange: Runner on third, 1 out, double play ends half
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 3, half: InningHalf.Bottom, outs: 1,
            onFirst: false, onSecond: false, onThird: true,
            awayScore: 2, homeScore: 1,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 3,
            offense: Team.Home, defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 2,
            RunsScored: 0,
            NewBases: new BaseState(false, false, false),
            Type: PaType.InPlayOut,
            Tag: OutcomeTag.DP,
            Flags: new PaFlags(IsDoublePlay: true, IsSacFly: false),
            BasesAtThirdOut: new BaseState(false, false, true)
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(_scorekeeper.HomeLOB[0], Is.EqualTo(1));
        Assert.That(result.StateAfter.Half, Is.EqualTo(InningHalf.Top));
        Assert.That(result.StateAfter.Inning, Is.EqualTo(4));
    }

    [Test]
    public void LOB_StrikeoutWithRunners_EqualsTwo() {
        // Arrange: Runners on first and second, 2 outs, strikeout
        var state = new GameState(
            balls: 3, strikes: 2,
            inning: 7, half: InningHalf.Top, outs: 2,
            onFirst: true, onSecond: true, onThird: false,
            awayScore: 3, homeScore: 2,
            awayBattingOrderIndex: 5, homeBattingOrderIndex: 2,
            offense: Team.Away, defense: Team.Home
        );

        var resolution = new PaResolution(
            OutsAdded: 1,
            RunsScored: 0,
            NewBases: new BaseState(true, true, false),
            Type: PaType.K,
            Tag: OutcomeTag.K,
            BasesAtThirdOut: new BaseState(true, true, false)
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(_scorekeeper.AwayLOB[0], Is.EqualTo(2));
        Assert.That(result.StateAfter.Half, Is.EqualTo(InningHalf.Bottom));
    }

    [Test]
    public void LOB_RunnerScoresBeforeThirdOut_NotCountedInLOB() {
        // Arrange: R1/R2, 2 outs, R2 scores before third out on trailing runner
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 8, half: InningHalf.Top, outs: 2,
            onFirst: true, onSecond: true, onThird: false,
            awayScore: 2, homeScore: 3,
            awayBattingOrderIndex: 1, homeBattingOrderIndex: 0,
            offense: Team.Away, defense: Team.Home
        );

        var resolution = new PaResolution(
            OutsAdded: 1,
            RunsScored: 1,  // R2 scores
            NewBases: new BaseState(false, false, false),
            Type: PaType.InPlayOut,
            Tag: OutcomeTag.InPlayOut,
            BasesAtThirdOut: new BaseState(true, false, false)  // Only R1 at instant of third out
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(_scorekeeper.AwayLOB[0], Is.EqualTo(1));  // Only R1 stranded
        Assert.That(result.StateAfter.Half, Is.EqualTo(InningHalf.Bottom));
    }

    [Test]
    public void LOB_EmptyBasesAtThirdOut_EqualsZero() {
        // Arrange: No runners, 2 outs, strikeout
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 1, half: InningHalf.Top, outs: 2,
            onFirst: false, onSecond: false, onThird: false,
            awayScore: 0, homeScore: 0,
            awayBattingOrderIndex: 2, homeBattingOrderIndex: 0,
            offense: Team.Away, defense: Team.Home
        );

        var resolution = new PaResolution(
            OutsAdded: 1,
            RunsScored: 0,
            NewBases: new BaseState(false, false, false),
            Type: PaType.InPlayOut,
            Tag: OutcomeTag.InPlayOut,
            BasesAtThirdOut: new BaseState(false, false, false)
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(_scorekeeper.AwayLOB[0], Is.EqualTo(0));
        Assert.That(result.StateAfter.Half, Is.EqualTo(InningHalf.Bottom));
    }

    [Test]
    public void LOB_WalkoffNonHomeRun_AlwaysZero() {
        // Arrange: Bottom 9th, tied, R1/R3, 2 outs, single wins game
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Bottom, outs: 2,
            onFirst: true, onSecond: false, onThird: true,
            awayScore: 3, homeScore: 3,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 5,
            offense: Team.Home, defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(true, true, false),
            Type: PaType.Single,
            Tag: OutcomeTag.Single,
            BasesAtThirdOut: null  // No third out in walk-off
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(_scorekeeper.HomeLOB[0], Is.EqualTo(0));  // Walk-off: LOB always 0
        Assert.That(result.StateAfter.IsFinal, Is.True);
        Assert.That(result.StateAfter.HomeScore, Is.EqualTo(4));
    }

    [Test]
    public void LOB_WalkoffHomeRun_AlwaysZero() {
        // Arrange: Bottom 9th, down by 2, bases loaded, grand slam
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Bottom, outs: 1,
            onFirst: true, onSecond: true, onThird: true,
            awayScore: 5, homeScore: 3,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 8,
            offense: Team.Home, defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 4,
            NewBases: new BaseState(false, false, false),
            Type: PaType.HomeRun,
            Tag: OutcomeTag.HR,
            BasesAtThirdOut: null  // No third out in walk-off
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(_scorekeeper.HomeLOB[0], Is.EqualTo(0));  // Walk-off: LOB always 0
        Assert.That(result.StateAfter.IsFinal, Is.True);
        Assert.That(result.StateAfter.HomeScore, Is.EqualTo(7));
    }

    [Test]
    public void LOB_BackwardCompatibility_NoSnapshot_UsesNewBases() {
        // Arrange: R1/R2, 2 outs, strikeout (producer not updated yet)
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 4, half: InningHalf.Top, outs: 2,
            onFirst: true, onSecond: true, onThird: false,
            awayScore: 1, homeScore: 0,
            awayBattingOrderIndex: 4, homeBattingOrderIndex: 0,
            offense: Team.Away, defense: Team.Home
        );

        var resolution = new PaResolution(
            OutsAdded: 1,
            RunsScored: 0,
            NewBases: new BaseState(true, true, false),
            Type: PaType.K,
            Tag: OutcomeTag.K,
            BasesAtThirdOut: null  // Producer hasn't been updated yet
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        // Should fall back to NewBases for LOB computation
        Assert.That(_scorekeeper.AwayLOB[0], Is.EqualTo(2));
        Assert.That(result.StateAfter.Half, Is.EqualTo(InningHalf.Bottom));
    }

    [Test]
    public void Walkoff_RecordsPartialInningRuns_NotX() {
        // Setup: Bottom 9th, tie game, runner on 2nd
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 9,
            half: InningHalf.Bottom,
            outs: 1,
            onFirst: false,
            onSecond: true,
            onThird: false,
            awayScore: 3,
            homeScore: 3,
            awayBattingOrderIndex: 0,
            homeBattingOrderIndex: 0,
            offense: Team.Home,
            defense: Team.Away,
            isFinal: false
        );

        // Need to record previous innings first (simulate 8.5 innings)
        for (int i = 0; i < 9; i++) {
            _scorekeeper.LineScore.RecordInning(Team.Away, 0);
            if (i < 8) {
                _scorekeeper.LineScore.RecordInning(Team.Home, 0);
            }
        }

        // Walk-off single scores 1 run
        var walkoffHit = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(true, false, false),
            Type: PaType.Single,
            Tag: OutcomeTag.Single
        );
        var result = _scorekeeper.ApplyPlateAppearance(state, walkoffHit);
        state = result.StateAfter;

        // Verify game is final
        Assert.That(state.IsFinal, Is.True);
        Assert.That(state.HomeScore, Is.EqualTo(4));

        // Verify line score shows actual runs (1), not 'X'
        Assert.That(_scorekeeper.LineScore.GetInningRuns(Team.Home, 9), Is.EqualTo(1));
        Assert.That(_scorekeeper.LineScore.GetInningDisplay(Team.Home, 9), Is.EqualTo("1"));

        // Verify LOB = 0 for walk-off partial inning
        Assert.That(_scorekeeper.HomeLOB[^1], Is.EqualTo(0));
    }

    [Test]
    public void SkipBottom9th_RecordsX_WhenHomeLeading() {
        // Setup: Top 9th, 2 outs, home leading
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 9,
            half: InningHalf.Top,
            outs: 2,
            onFirst: false,
            onSecond: false,
            onThird: false,
            awayScore: 3,
            homeScore: 4,
            awayBattingOrderIndex: 0,
            homeBattingOrderIndex: 0,
            offense: Team.Away,
            defense: Team.Home,
            isFinal: false
        );

        // Simulate previous innings
        for (int i = 0; i < 9; i++) {
            _scorekeeper.LineScore.RecordInning(Team.Away, 0);
            if (i < 8) {
                _scorekeeper.LineScore.RecordInning(Team.Home, 0);
            }
        }

        // 3rd out ends top 9th
        var finalOut = new PaResolution(
            OutsAdded: 1,
            RunsScored: 0,
            NewBases: new BaseState(false, false, false),
            Type: PaType.InPlayOut,
            Tag: OutcomeTag.InPlayOut
        );
        var result = _scorekeeper.ApplyPlateAppearance(state, finalOut);
        state = result.StateAfter;

        // Verify game is final (bottom 9th skipped)
        Assert.That(state.IsFinal, Is.True);
        Assert.That(state.Inning, Is.EqualTo(9));
        Assert.That(state.Half, Is.EqualTo(InningHalf.Top));

        // Verify line score shows 'X' for home 9th
        Assert.That(_scorekeeper.LineScore.GetInningRuns(Team.Home, 9), Is.EqualTo(-1));
        Assert.That(_scorekeeper.LineScore.GetInningDisplay(Team.Home, 9), Is.EqualTo("X"));
    }

    [Test]
    public void LineScore_GetInningDisplay_ReturnsCorrectValues() {
        _scorekeeper.LineScore.RecordInning(Team.Away, 3);
        _scorekeeper.LineScore.RecordInning(Team.Home, 0);
        _scorekeeper.LineScore.RecordSkippedInning(Team.Home);

        Assert.That(_scorekeeper.LineScore.GetInningDisplay(Team.Away, 1), Is.EqualTo("3"));
        Assert.That(_scorekeeper.LineScore.GetInningDisplay(Team.Home, 1), Is.EqualTo("0"));
        Assert.That(_scorekeeper.LineScore.GetInningDisplay(Team.Home, 2), Is.EqualTo("X"));
        Assert.That(_scorekeeper.LineScore.GetInningDisplay(Team.Away, 3), Is.EqualTo("-")); // Not played yet
    }

    /// <summary>
    /// Bottom 9th is skipped when home leads after top 9: line score shows 'X'
    /// and both teams expose exactly 9 inning columns.
    /// </summary>
    [Test]
    public void LineScore_SkippedBottom9_DisplaysX_AndHasNineColumns() {
        // 1) Prefill through completed 8 full innings so column indices align 1..9
        var s = PrefillThroughInning8Quietly();

        // 2) Start top 9th with home already leading
        s = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Top, outs: 0,
            onFirst: false, onSecond: false, onThird: false,
            awayScore: 2, homeScore: 3,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 0,
            offense: Team.Away, defense: Team.Home
        );

        // 3) End the top 9th quietly (0 runs, 3 outs) via the test helper
        var resultState = ScoreRunsAndEndHalf(s, runs: 0, outs: 3);

        // 4) Assertions: game ended, bottom 9 is X, and there are exactly 9 columns per side
        Assert.That(resultState.IsFinal, Is.True, "Game should end since home already led after top 9.");
        Assert.That(_scorekeeper.LineScore.HomeInnings.Count, Is.EqualTo(9), "Home should have 9 inning entries.");
        Assert.That(_scorekeeper.LineScore.AwayInnings.Count, Is.EqualTo(9), "Away should have 9 inning entries.");
        Assert.That(_scorekeeper.LineScore.GetInningDisplay(Team.Home, 9), Is.EqualTo("X"), "Home 9th should display 'X'.");
        // Also sanity: the away 9th was recorded (0 runs)
        Assert.That(_scorekeeper.LineScore.GetInningDisplay(Team.Away, 9), Is.EqualTo("0"));
    }

    /// <summary>
    /// The public display API should always expose 9 columns (1..9) per team,
    /// even if some innings have not been played yet (they render as '-').
    /// </summary>
    [Test]
    public void LineScore_Display_HasExactlyNineColumns_ForEachTeam() {
        // Start from a fresh scoreboard: only top 1st is completed quietly
        var s = new GameState(
            balls: 0, strikes: 0,
            inning: 1, half: InningHalf.Top, outs: 0,
            onFirst: false, onSecond: false, onThird: false,
            awayScore: 0, homeScore: 0,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 0,
            offense: Team.Away, defense: Team.Home
        );
        _ = ScoreRunsAndEndHalf(s, runs: 0, outs: 3);

        // Ask for displays across 1..9 for both teams; should never throw and be short strings
        for (int i = 1; i <= 9; i++) {
            var away = _scorekeeper.LineScore.GetInningDisplay(Team.Away, i);
            var home = _scorekeeper.LineScore.GetInningDisplay(Team.Home, i);
            Assert.That(away, Is.Not.Null);
            Assert.That(home, Is.Not.Null);
            Assert.That(away.Length, Is.LessThanOrEqualTo(2));
            Assert.That(home.Length, Is.LessThanOrEqualTo(2));
        }
    }

    /// <summary>
    /// Helper method to score runs and end a half-inning.
    /// </summary>
    private GameState ScoreRunsAndEndHalf(GameState state, int runs, int outs) {
        // Score runs
        if (runs > 0) {
            var scoreResolution = new PaResolution(
                OutsAdded: 0,
                RunsScored: runs,
                NewBases: new BaseState(false, false, false),
                Type: PaType.HomeRun,
                Tag: OutcomeTag.HR
            );
            var result = _scorekeeper.ApplyPlateAppearance(state, scoreResolution);
            state = result.StateAfter;
        }

        // Record outs to end half
        for (int i = 0; i < outs; i++) {
            var outResolution = new PaResolution(
                OutsAdded: 1,
                RunsScored: 0,
                NewBases: new BaseState(false, false, false),
                Type: PaType.InPlayOut,
                Tag: OutcomeTag.InPlayOut
            );
            var outResult = _scorekeeper.ApplyPlateAppearance(state, outResolution);
            state = outResult.StateAfter;
        }

        return state;
    }

    /// <summary>
    /// Builds a clean scoreboard with innings 1..8 fully played quietly (0 runs each side),
    /// so line score indices are aligned before we assert on the 9th.
    /// </summary>
    private GameState PrefillThroughInning8Quietly() {
        // Begin at a neutral "pre-game" state; we’ll position each half explicitly
        var s = new GameState(
            balls: 0, strikes: 0,
            inning: 1, half: InningHalf.Top, outs: 0,
            onFirst: false, onSecond: false, onThird: false,
            awayScore: 0, homeScore: 0,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 0,
            offense: Team.Away, defense: Team.Home
        );

        for (int inning = 1; inning <= 8; inning++) {
            // Top half (away bats) → quiet half
            s = new GameState(0, 0, inning, InningHalf.Top, 0, false, false, false,
                              s.AwayScore, s.HomeScore, 0, 0, Team.Away, Team.Home);
            s = ScoreRunsAndEndHalf(s, runs: 0, outs: 3);

            // Bottom half (home bats) → quiet half
            s = new GameState(0, 0, inning, InningHalf.Bottom, 0, false, false, false,
                              s.AwayScore, s.HomeScore, 0, 0, Team.Home, Team.Away);
            s = ScoreRunsAndEndHalf(s, runs: 0, outs: 3);
        }

        return s;
    }

    /// <summary>
    /// Sum of per-inning runs equals the team’s scoreboard runs (away & home).
    /// </summary>
    [Test]
    public void Totals_LineScore_AndScoreboard_Agree() {
        // Arrange: build a simple 3-inning script with known runs
        var s = new GameState(0, 0, inning: 1, half: InningHalf.Top, outs: 0,
                              onFirst: false, onSecond: false, onThird: false,
                              awayScore: 0, homeScore: 0,
                              awayBattingOrderIndex: 0, homeBattingOrderIndex: 0,
                              offense: Team.Away, defense: Team.Home);

        // T1: away 2 runs, then end half
        s = ScoreRunsAndEndHalf(s, runs: 2, outs: 3);
        // B1: home 0
        s = new GameState(0, 0, 1, InningHalf.Bottom, 0, false, false, false, s.AwayScore, s.HomeScore, 0, 0, Team.Home, Team.Away);
        s = ScoreRunsAndEndHalf(s, runs: 0, outs: 3);

        // T2: away 1
        s = new GameState(0, 0, 2, InningHalf.Top, 0, false, false, false, s.AwayScore, s.HomeScore, 0, 0, Team.Away, Team.Home);
        s = ScoreRunsAndEndHalf(s, runs: 1, outs: 3);
        // B2: home 3
        s = new GameState(0, 0, 2, InningHalf.Bottom, 0, false, false, false, s.AwayScore, s.HomeScore, 0, 0, Team.Home, Team.Away);
        s = ScoreRunsAndEndHalf(s, runs: 3, outs: 3);

        // T3/B3: zeros (just to have another frame)
        s = new GameState(0, 0, 3, InningHalf.Top, 0, false, false, false, s.AwayScore, s.HomeScore, 0, 0, Team.Away, Team.Home);
        s = ScoreRunsAndEndHalf(s, runs: 0, outs: 3);
        s = new GameState(0, 0, 3, InningHalf.Bottom, 0, false, false, false, s.AwayScore, s.HomeScore, 0, 0, Team.Home, Team.Away);
        s = ScoreRunsAndEndHalf(s, runs: 0, outs: 3);

        // Act: sum line score by team
        int SumInnings(Team t) {
            var total = 0;
            for (int i = 1; i <= 9; i++) {
                var r = _scorekeeper.LineScore.GetInningRuns(t, i);
                if (r > -1) total += r; // -1 means 'X' (skipped)
            }
            return total;
        }

        var lineAway = SumInnings(Team.Away);
        var lineHome = SumInnings(Team.Home);

        // Assert
        Assert.That(lineAway, Is.EqualTo(s.AwayScore), "Away line score sum should match scoreboard runs.");
        Assert.That(lineHome, Is.EqualTo(s.HomeScore), "Home line score sum should match scoreboard runs.");
    }

}
