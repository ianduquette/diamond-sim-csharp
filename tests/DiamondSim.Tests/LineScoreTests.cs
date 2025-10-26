using NUnit.Framework;

namespace DiamondSim.Tests;

/// <summary>
/// Tests for LineScore tracking and LOB (left on base) functionality in Phase 3.
/// </summary>
[TestFixture]
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
            Type: PaType.HomeRun
        );
        state = _scorekeeper.ApplyPlateAppearance(state, resolution1);

        // 3 outs to end half
        for (int i = 0; i < 3; i++) {
            var outResolution = new PaResolution(
                OutsAdded: 1,
                RunsScored: 0,
                NewBases: new BaseState(false, false, false),
                Type: PaType.InPlayOut
            );
            state = _scorekeeper.ApplyPlateAppearance(state, outResolution);
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
            Type: PaType.BB
        );
        state = _scorekeeper.ApplyPlateAppearance(state, loadBases);

        // 3rd out with bases loaded
        for (int i = 0; i < 3; i++) {
            var outResolution = new PaResolution(
                OutsAdded: 1,
                RunsScored: 0,
                NewBases: new BaseState(true, true, true), // Bases stay loaded
                Type: PaType.InPlayOut
            );
            state = _scorekeeper.ApplyPlateAppearance(state, outResolution);
        }

        // Verify LOB = 3 for away team
        Assert.That(_scorekeeper.AwayLOB[0], Is.EqualTo(3));
        Assert.That(_scorekeeper.AwayTotalLOB, Is.EqualTo(3));
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
            Type: PaType.Single
        );
        state = _scorekeeper.ApplyPlateAppearance(state, walkoffHit);

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
            Type: PaType.InPlayOut
        );
        state = _scorekeeper.ApplyPlateAppearance(state, finalOut);

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
    /// Helper method to score runs and end a half-inning.
    /// </summary>
    private GameState ScoreRunsAndEndHalf(GameState state, int runs, int outs) {
        // Score runs
        if (runs > 0) {
            var scoreResolution = new PaResolution(
                OutsAdded: 0,
                RunsScored: runs,
                NewBases: new BaseState(false, false, false),
                Type: PaType.HomeRun
            );
            state = _scorekeeper.ApplyPlateAppearance(state, scoreResolution);
        }

        // Record outs to end half
        for (int i = 0; i < outs; i++) {
            var outResolution = new PaResolution(
                OutsAdded: 1,
                RunsScored: 0,
                NewBases: new BaseState(false, false, false),
                Type: PaType.InPlayOut
            );
            state = _scorekeeper.ApplyPlateAppearance(state, outResolution);
        }

        return state;
    }
}
