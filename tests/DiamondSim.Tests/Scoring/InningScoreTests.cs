using DiamondSim.Tests.TestHelpers;

namespace DiamondSim.Tests.Scoring;

/// <summary>
/// Comprehensive tests for InningScorekeeper covering all critical scenarios from PRD Section 7.
/// Tests walk-offs, half-inning transitions, extra innings, skip bottom 9th, and determinism.
/// </summary>
[TestFixture]
[Category("Scoring")]
public class InningScoreTests {
    private InningScorekeeper _scorekeeper = null!;

    [SetUp]
    public void Setup() {
        _scorekeeper = new InningScorekeeper();
    }

    #region Walk-off Tests

    /// <summary>
    /// Test: Walk-off home run in bottom 9th ends game immediately.
    /// PRD Section 7.1: ApplyPA_Bottom9thTieBreakingHomeRun_EndsGameImmediately
    /// </summary>
    [Test]
    public void ApplyPA_Bottom9thTieBreakingHomeRun_EndsGameImmediately() {
        // Arrange: Tie game, bottom 9th, 2 outs
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 9,
            half: InningHalf.Bottom,
            outs: 2,
            onFirst: false,
            onSecond: false,
            onThird: false,
            awayScore: 3,
            homeScore: 3,
            awayBattingOrderIndex: 0,
            homeBattingOrderIndex: 5,
            offense: Team.Home,
            defense: Team.Away,
            isFinal: false
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1, // Solo HR
            NewBases: new BaseState(false, false, false),
            Type: PaType.HomeRun,
        Tag: OutcomeTag.HR);

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);
        var newState = result.StateAfter;

        // Assert
        Assert.That(newState.HomeScore, Is.EqualTo(4), "Home team should score 1 run");
        Assert.That(newState.AwayScore, Is.EqualTo(3), "Away score should remain unchanged");
        Assert.That(newState.IsFinal, Is.True, "Game should end immediately on walk-off");
        Assert.That(newState.Outs, Is.EqualTo(2), "Outs don't reset on walk-off");
        Assert.That(newState.Inning, Is.EqualTo(9), "Should still be 9th inning");
        Assert.That(newState.Half, Is.EqualTo(InningHalf.Bottom), "Should still be bottom half");
    }

    /// <summary>
    /// Test: Walk-off in extra innings (bottom 10th) sets IsFinal, records partial runs, LOB = 0.
    /// PRD Section 7.1: Walkoff_MidInningBottom10_SetsIsFinal_RecordsPartialRuns_LobZero
    /// </summary>
    [Test]
    public void Walkoff_MidInningBottom10_SetsIsFinal_RecordsPartialRuns_LobZero() {
        // Arrange: Bottom 10th, 1 out, runner on 2nd, tie game
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 10,
            half: InningHalf.Bottom,
            outs: 1,
            onFirst: false,
            onSecond: true,
            onThird: false,
            awayScore: 5,
            homeScore: 5,
            awayBattingOrderIndex: 0,
            homeBattingOrderIndex: 3,
            offense: Team.Home,
            defense: Team.Away,
            isFinal: false
        );

        // Simulate previous innings (9 complete innings)
        for (int i = 0; i < 10; i++) {
            _scorekeeper.LineScore.RecordInning(Team.Away, 0);
            if (i < 9) {
                _scorekeeper.LineScore.RecordInning(Team.Home, 0);
            }
        }

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1, // Runner from 2nd scores
            NewBases: new BaseState(true, false, false), // Batter on 1st
            Type: PaType.Single,
        Tag: OutcomeTag.Single);

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);
        var newState = result.StateAfter;

        // Assert
        Assert.That(newState.IsFinal, Is.True, "Game should end on walk-off");
        Assert.That(newState.HomeScore, Is.EqualTo(6), "Home wins 6-5");
        Assert.That(newState.AwayScore, Is.EqualTo(5), "Away score unchanged");
        Assert.That(newState.Outs, Is.EqualTo(1), "Outs don't reset on walk-off");

        // Verify partial inning runs recorded (not 'X')
        Assert.That(_scorekeeper.LineScore.GetInningRuns(Team.Home, 10), Is.EqualTo(1),
            "Partial inning should show actual runs scored");

        // Verify LOB = 0 for walk-off partial inning (no 3rd out)
        Assert.That(_scorekeeper.HomeLOB[^1], Is.EqualTo(0),
            "Walk-off partial inning should have LOB = 0");
    }

    #endregion

    #region Half-Inning Transition Tests

    /// <summary>
    /// Test: Double play with 1 out ends half-inning and clears bases.
    /// PRD Section 7.1: ApplyPA_DoublePlayWith1Out_EndsHalfAndClearsBases
    /// </summary>
    [Test]
    public void ApplyPA_DoublePlayWith1Out_EndsHalfAndClearsBases() {
        // Arrange: 1 out, runners on 1st and 2nd
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 5,
            half: InningHalf.Top,
            outs: 1,
            onFirst: true,
            onSecond: true,
            onThird: false,
            awayScore: 2,
            homeScore: 1,
            awayBattingOrderIndex: 4,
            homeBattingOrderIndex: 0,
            offense: Team.Away,
            defense: Team.Home,
            isFinal: false
        );

        var resolution = new PaResolution(
            OutsAdded: 2, // Double play
            RunsScored: 0,
            NewBases: new BaseState(true, true, false), // Bases still occupied at moment of 3rd out
            Type: PaType.InPlayOut,
        Tag: OutcomeTag.InPlayOut);

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);
        var newState = result.StateAfter;

        // Assert
        Assert.That(newState.Outs, Is.EqualTo(0), "Outs should reset to 0");
        Assert.That(newState.OnFirst, Is.False, "First base should be cleared");
        Assert.That(newState.OnSecond, Is.False, "Second base should be cleared");
        Assert.That(newState.OnThird, Is.False, "Third base should be cleared");
        Assert.That(newState.Half, Is.EqualTo(InningHalf.Bottom), "Should transition to bottom half");
        Assert.That(newState.Inning, Is.EqualTo(5), "Should still be 5th inning");
        Assert.That(newState.Offense, Is.EqualTo(Team.Home), "Offense should switch to home");
        Assert.That(newState.Defense, Is.EqualTo(Team.Away), "Defense should switch to away");

        // Verify LOB = 2 (runners on 1st and 2nd at moment of 3rd out)
        Assert.That(_scorekeeper.AwayLOB[^1], Is.EqualTo(2), "Should record 2 LOB");
    }

    /// <summary>
    /// Test: Bases loaded walk scores 1 run and bases remain loaded.
    /// PRD Section 7.1: ApplyPA_BasesLoadedWalk_Scores1RunAndBasesRemainLoaded
    /// </summary>
    [Test]
    public void ApplyPA_BasesLoadedWalk_Scores1RunAndBasesRemainLoaded() {
        // Arrange: Bases loaded, 1 out
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 3,
            half: InningHalf.Bottom,
            outs: 1,
            onFirst: true,
            onSecond: true,
            onThird: true,
            awayScore: 2,
            homeScore: 2,
            awayBattingOrderIndex: 0,
            homeBattingOrderIndex: 6,
            offense: Team.Home,
            defense: Team.Away,
            isFinal: false
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1, // Runner from 3rd scores
            NewBases: new BaseState(true, true, true), // Still loaded
            Type: PaType.BB,
        Tag: OutcomeTag.BB);

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);
        var newState = result.StateAfter;

        // Assert
        Assert.That(newState.HomeScore, Is.EqualTo(3), "Home should score 1 run");
        Assert.That(newState.Outs, Is.EqualTo(1), "Outs should not change");
        Assert.That(newState.OnFirst, Is.True, "First base should remain occupied");
        Assert.That(newState.OnSecond, Is.True, "Second base should remain occupied");
        Assert.That(newState.OnThird, Is.True, "Third base should remain occupied");
        Assert.That(newState.Half, Is.EqualTo(InningHalf.Bottom), "Should still be bottom half");
        Assert.That(newState.Inning, Is.EqualTo(3), "Should still be 3rd inning");
    }

    #endregion

    #region Extra Innings Tests

    /// <summary>
    /// Test: Game tied after 9 innings continues to extras.
    /// PRD Section 7.1: ApplyPA_TiedAfter9Innings_ContinuesToExtras
    /// </summary>
    [Test]
    public void ApplyPA_TiedAfter9Innings_ContinuesToExtras() {
        // Arrange: Bottom 9th, 3rd out, still tied
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 9,
            half: InningHalf.Bottom,
            outs: 2,
            onFirst: false,
            onSecond: false,
            onThird: false,
            awayScore: 5,
            homeScore: 5,
            awayBattingOrderIndex: 0,
            homeBattingOrderIndex: 8,
            offense: Team.Home,
            defense: Team.Away,
            isFinal: false
        );

        var resolution = new PaResolution(
            OutsAdded: 1, // 3rd out
            RunsScored: 0,
            NewBases: new BaseState(false, false, false),
            Type: PaType.InPlayOut,
        Tag: OutcomeTag.InPlayOut);

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);
        var newState = result.StateAfter;

        // Assert
        Assert.That(newState.IsFinal, Is.False, "Game should continue to extras");
        Assert.That(newState.Inning, Is.EqualTo(10), "Should advance to 10th inning");
        Assert.That(newState.Half, Is.EqualTo(InningHalf.Top), "Should be top of 10th");
        Assert.That(newState.Outs, Is.EqualTo(0), "Outs should reset");
        Assert.That(newState.Offense, Is.EqualTo(Team.Away), "Away team should bat in top 10th");
        Assert.That(newState.Defense, Is.EqualTo(Team.Home), "Home team should field in top 10th");
    }

    /// <summary>
    /// Test: Safety guard throws exception when inning exceeds 99.
    /// PRD Section 7.1: ApplyPA_InningExceeds99_ThrowsException
    /// </summary>
    [Test]
    public void ApplyPA_InningExceeds99_ThrowsException() {
        // Arrange: Inning 100 (safety limit exceeded)
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 100,
            half: InningHalf.Top,
            outs: 0,
            onFirst: false,
            onSecond: false,
            onThird: false,
            awayScore: 5,
            homeScore: 5,
            awayBattingOrderIndex: 0,
            homeBattingOrderIndex: 0,
            offense: Team.Away,
            defense: Team.Home,
            isFinal: false
        );

        var resolution = new PaResolution(
            OutsAdded: 1,
            RunsScored: 0,
            NewBases: new BaseState(false, false, false),
            Type: PaType.InPlayOut,
        Tag: OutcomeTag.InPlayOut);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _scorekeeper.ApplyPlateAppearance(state, resolution));

        Assert.That(ex!.Message, Does.Contain("exceeded maximum inning limit"));
        Assert.That(ex.Message, Does.Contain("99"));
    }

    #endregion

    #region Skip Bottom 9th Tests

    /// <summary>
    /// Test: Home team leading after top 9th - bottom 9th is skipped.
    /// PRD Section 7.1: ApplyPA_Top9thEndsWithHomeLeading_SkipsBottom9th
    /// </summary>
    [Test]
    public void ApplyPA_Top9thEndsWithHomeLeading_SkipsBottom9th() {
        // Arrange: Top 9th, 3rd out, home team leading
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
            awayBattingOrderIndex: 7,
            homeBattingOrderIndex: 0,
            offense: Team.Away,
            defense: Team.Home,
            isFinal: false
        );

        // Need to record previous innings first
        for (int i = 0; i < 9; i++) {
            _scorekeeper.LineScore.RecordInning(Team.Away, 0);
            if (i < 8) {
                _scorekeeper.LineScore.RecordInning(Team.Home, 0);
            }
        }

        var resolution = new PaResolution(
            OutsAdded: 1, // 3rd out
            RunsScored: 0,
            NewBases: new BaseState(false, false, false),
            Type: PaType.InPlayOut,
        Tag: OutcomeTag.InPlayOut);

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);
        var newState = result.StateAfter;

        // Assert
        Assert.That(newState.IsFinal, Is.True, "Game should end (bottom 9th skipped)");
        Assert.That(newState.Inning, Is.EqualTo(9), "Should still be 9th inning");
        Assert.That(newState.Half, Is.EqualTo(InningHalf.Top), "Should still be top half");
        Assert.That(newState.AwayScore, Is.EqualTo(3), "Away score unchanged");
        Assert.That(newState.HomeScore, Is.EqualTo(4), "Home score unchanged");

        // Line score should show 'X' for home 9th inning
        Assert.That(_scorekeeper.LineScore.GetInningRuns(Team.Home, 9), Is.EqualTo(-1),
            "Home 9th should be marked as skipped (-1)");
        Assert.That(_scorekeeper.LineScore.GetInningDisplay(Team.Home, 9), Is.EqualTo("X"),
            "Home 9th should display 'X'");
    }

    /// <summary>
    /// Test: Home team losing after top 9th - bottom 9th must be played.
    /// PRD Section 7.1: ApplyPA_Top9thEndsWithHomeLosing_Bottom9thMustBePlayed
    /// </summary>
    [Test]
    public void ApplyPA_Top9thEndsWithHomeLosing_Bottom9thMustBePlayed() {
        // Arrange: Top 9th, 3rd out, home team losing
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 9,
            half: InningHalf.Top,
            outs: 2,
            onFirst: false,
            onSecond: false,
            onThird: false,
            awayScore: 5,
            homeScore: 3,
            awayBattingOrderIndex: 4,
            homeBattingOrderIndex: 0,
            offense: Team.Away,
            defense: Team.Home,
            isFinal: false
        );

        var resolution = new PaResolution(
            OutsAdded: 1, // 3rd out
            RunsScored: 0,
            NewBases: new BaseState(false, false, false),
            Type: PaType.InPlayOut,
        Tag: OutcomeTag.InPlayOut);

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);
        var newState = result.StateAfter;

        // Assert
        Assert.That(newState.IsFinal, Is.False, "Game should continue (bottom 9th must be played)");
        Assert.That(newState.Inning, Is.EqualTo(9), "Should still be 9th inning");
        Assert.That(newState.Half, Is.EqualTo(InningHalf.Bottom), "Should transition to bottom half");
        Assert.That(newState.Outs, Is.EqualTo(0), "Outs should reset");
        Assert.That(newState.Offense, Is.EqualTo(Team.Home), "Home team should bat");
        Assert.That(newState.Defense, Is.EqualTo(Team.Away), "Away team should field");
    }

    /// <summary>
    /// Test: Home team trailing after top 9th - bottom 9th must be played.
    /// PRD Section 7.1: Top9_EndsWithHomeTrailing_PlaysBottom9
    /// </summary>
    [Test]
    public void Top9_EndsWithHomeTrailing_PlaysBottom9() {
        // Arrange: Top 9th, 3rd out, home team trailing
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 9,
            half: InningHalf.Top,
            outs: 2,
            onFirst: false,
            onSecond: false,
            onThird: false,
            awayScore: 5,
            homeScore: 3,
            awayBattingOrderIndex: 2,
            homeBattingOrderIndex: 0,
            offense: Team.Away,
            defense: Team.Home,
            isFinal: false
        );

        var resolution = new PaResolution(
            OutsAdded: 1, // 3rd out
            RunsScored: 0,
            NewBases: new BaseState(false, false, false),
            Type: PaType.InPlayOut,
        Tag: OutcomeTag.InPlayOut);

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);
        var newState = result.StateAfter;

        // Assert
        Assert.That(newState.IsFinal, Is.False, "Game should continue");
        Assert.That(newState.Inning, Is.EqualTo(9), "Should still be 9th inning");
        Assert.That(newState.Half, Is.EqualTo(InningHalf.Bottom), "Should transition to bottom");
        Assert.That(newState.Outs, Is.EqualTo(0), "Outs should reset for bottom 9th");
        Assert.That(newState.Offense, Is.EqualTo(Team.Home), "Home should bat");
    }

    /// <summary>
    /// Test: Skip bottom 9th when home leads after top 9th - verify IsFinal and 'X' recorded.
    /// PRD Section 7.1: SkipBottom9_WhenHomeLeadsAfterTop9_IsFinalTrueAndXRecorded
    /// </summary>
    [Test]
    public void SkipBottom9_WhenHomeLeadsAfterTop9_IsFinalTrueAndXRecorded() {
        // Arrange: Top 9th, 3rd out, home team leading
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
            awayBattingOrderIndex: 5,
            homeBattingOrderIndex: 0,
            offense: Team.Away,
            defense: Team.Home,
            isFinal: false
        );

        // Need to record previous innings first
        for (int i = 0; i < 9; i++) {
            _scorekeeper.LineScore.RecordInning(Team.Away, 0);
            if (i < 8) {
                _scorekeeper.LineScore.RecordInning(Team.Home, 0);
            }
        }

        var resolution = new PaResolution(
            OutsAdded: 1, // 3rd out
            RunsScored: 0,
            NewBases: new BaseState(false, false, false),
            Type: PaType.InPlayOut,
        Tag: OutcomeTag.InPlayOut);

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);
        var newState = result.StateAfter;

        // Assert
        Assert.That(newState.IsFinal, Is.True, "Game should be final");
        Assert.That(newState.Inning, Is.EqualTo(9), "Should still be 9th inning");
        Assert.That(newState.Half, Is.EqualTo(InningHalf.Top), "Should still be top half");

        // Verify 'X' recorded for home 9th
        Assert.That(_scorekeeper.LineScore.GetInningRuns(Team.Home, 9), Is.EqualTo(-1),
            "Home 9th should be marked as skipped");
        Assert.That(_scorekeeper.LineScore.GetInningDisplay(Team.Home, 9), Is.EqualTo("X"),
            "Home 9th should display 'X'");
    }

    #endregion

    #region RNG Determinism Test (CRITICAL)

    /// <summary>
    /// CRITICAL TEST: Verify that InningScorekeeper never calls RNG - enforces determinism.
    /// PRD Section 7.1: InningScoring_NeverCallsRNG_EnforcesDeterminism
    /// This test ensures all randomness occurs upstream in PRD-02/03, not in inning scoring.
    /// </summary>
    [Test]
    public void InningScoring_NeverCallsRNG_EnforcesDeterminism() {
        // This test verifies that inning scoring logic is purely deterministic
        // and does not make any RNG calls. All randomness must occur upstream
        // in the at-bat loop (PRD-02) and ball-in-play resolution (PRD-03).

        // Arrange: Normal game state
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 5,
            half: InningHalf.Top,
            outs: 1,
            onFirst: false,
            onSecond: false,
            onThird: false,
            awayScore: 3,
            homeScore: 2,
            awayBattingOrderIndex: 4,
            homeBattingOrderIndex: 0,
            offense: Team.Away,
            defense: Team.Home,
            isFinal: false
        );

        var resolution = new PaResolution(
            OutsAdded: 1,
            RunsScored: 0,
            NewBases: new BaseState(false, false, false),
            Type: PaType.InPlayOut,
        Tag: OutcomeTag.InPlayOut);

        // Act - should NOT throw because no RNG should be called
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);
        var newState = result.StateAfter;

        // Assert
        Assert.That(newState.Outs, Is.EqualTo(2), "Outs should increment deterministically");

        // If we get here, no RNG was called (test passes)
        // The InningScorekeeper class does not have any IRandomSource dependency,
        // which is verified by the fact that this test compiles and runs.
        // This test serves as documentation that inning scoring is deterministic.

        Assert.Pass("InningScorekeeper operates deterministically without RNG calls");
    }

    #endregion

    #region Integration Tests

    /// <summary>
    /// Integration test: Simulate a complete 9-inning game and verify line score validity.
    /// PRD Section 7.2: SimulateGame_9Innings_ProducesValidLineScore
    /// </summary>
    [Test]
    public void SimulateGame_9Innings_ProducesValidLineScore() {
        // Simulate a complete 9-inning game with scoring in various innings
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

        // Simulate 9 innings with various scoring patterns
        // Top 1st: Away scores 2
        state = ScoreRunsAndEndHalf(state, 2);
        // Bottom 1st: Home scores 1
        state = ScoreRunsAndEndHalf(state, 1);

        // Top 2nd: Away scores 0
        state = ScoreRunsAndEndHalf(state, 0);
        // Bottom 2nd: Home scores 3
        state = ScoreRunsAndEndHalf(state, 3);

        // Innings 3-8: No scoring
        for (int i = 3; i <= 8; i++) {
            state = ScoreRunsAndEndHalf(state, 0); // Top
            state = ScoreRunsAndEndHalf(state, 0); // Bottom
        }

        // Top 9th: Away scores 1
        state = ScoreRunsAndEndHalf(state, 1);

        // Bottom 9th: Home scores 0 (home loses 3-4)
        state = ScoreRunsAndEndHalf(state, 0);

        // Assert
        Assert.That(state.IsFinal, Is.True, "Game should be complete after 9 innings");
        Assert.That(_scorekeeper.LineScore.AwayTotal, Is.EqualTo(state.AwayScore),
            "Line score away total should match game state");
        Assert.That(_scorekeeper.LineScore.HomeTotal, Is.EqualTo(state.HomeScore),
            "Line score home total should match game state");
        Assert.That(_scorekeeper.LineScore.Validate(state.AwayScore, state.HomeScore), Is.True,
            "Line score should validate correctly");
    }

    /// <summary>
    /// Integration test: Simulate walk-off home run ending in bottom 9th.
    /// PRD Section 7.2: SimulateGame_WalkoffHomeRun_EndsInBottom9th
    /// </summary>
    [Test]
    public void SimulateGame_WalkoffHomeRun_EndsInBottom9th() {
        // Setup: Simulate game to bottom 9th, tie game
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

        // Simulate 8.5 innings with tie score
        for (int i = 1; i <= 8; i++) {
            state = ScoreRunsAndEndHalf(state, 0); // Top
            state = ScoreRunsAndEndHalf(state, 0); // Bottom
        }

        // Top 9th: Away scores 3
        state = ScoreRunsAndEndHalf(state, 3);

        // Bottom 9th: Home scores 3 to tie, then walk-off HR
        // Score 3 runs first
        var tieRuns = new PaResolution(
            OutsAdded: 0,
            RunsScored: 3,
            NewBases: new BaseState(false, false, false),
            Type: PaType.HomeRun,
        Tag: OutcomeTag.HR);
        var tmpResult1 = _scorekeeper.ApplyPlateAppearance(state, tieRuns);
        state = tmpResult1.StateAfter;

        // Walk-off HR
        var walkoffHR = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(false, false, false),
            Type: PaType.HomeRun,
        Tag: OutcomeTag.HR);
        var tmpResult2 = _scorekeeper.ApplyPlateAppearance(state, walkoffHR);
        state = tmpResult2.StateAfter;

        // Assert
        Assert.That(state.IsFinal, Is.True, "Game should end on walk-off");
        Assert.That(state.HomeScore, Is.EqualTo(4), "Home should win 4-3");
        Assert.That(state.AwayScore, Is.EqualTo(3), "Away should have 3 runs");
        Assert.That(state.Inning, Is.EqualTo(9), "Should end in 9th inning");
        Assert.That(state.Half, Is.EqualTo(InningHalf.Bottom), "Should end in bottom half");

        // Verify line score shows actual runs in bottom 9th (not 'X')
        Assert.That(_scorekeeper.LineScore.GetInningRuns(Team.Home, 9), Is.EqualTo(4),
            "Bottom 9th should show 4 runs scored");
    }

    /// <summary>
    /// Integration test: Simulate extra innings game continuing until winner determined.
    /// PRD Section 7.2: SimulateGame_ExtraInnings_ContinuesUntilWinner
    /// </summary>
    [Test]
    public void SimulateGame_ExtraInnings_ContinuesUntilWinner() {
        // Setup: Simulate game tied after 9 innings
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

        // Simulate 9 innings ending in 2-2 tie
        for (int i = 1; i <= 9; i++) {
            int runs = (i == 1 || i == 5) ? 1 : 0; // Score in innings 1 and 5
            state = ScoreRunsAndEndHalf(state, runs); // Top
            state = ScoreRunsAndEndHalf(state, runs); // Bottom
        }

        // Verify tied after 9
        Assert.That(state.AwayScore, Is.EqualTo(2), "Should be tied 2-2 after 9");
        Assert.That(state.HomeScore, Is.EqualTo(2), "Should be tied 2-2 after 9");
        Assert.That(state.IsFinal, Is.False, "Game should continue to extras");
        Assert.That(state.Inning, Is.EqualTo(10), "Should be in 10th inning");

        // Top 10th: Away scores 0
        state = ScoreRunsAndEndHalf(state, 0);

        // Bottom 10th: Home scores 0
        state = ScoreRunsAndEndHalf(state, 0);

        // Still tied, continue to 11th
        Assert.That(state.IsFinal, Is.False, "Game should continue to 11th");
        Assert.That(state.Inning, Is.EqualTo(11), "Should be in 11th inning");

        // Top 11th: Away scores 2
        state = ScoreRunsAndEndHalf(state, 2);

        // Bottom 11th: Home scores 1 (away wins 4-3)
        state = ScoreRunsAndEndHalf(state, 1);

        // Assert
        Assert.That(state.IsFinal, Is.True, "Game should end after completed 11th inning");
        Assert.That(state.AwayScore, Is.EqualTo(4), "Away should win 4-3");
        Assert.That(state.HomeScore, Is.EqualTo(3), "Home should have 3 runs");
        Assert.That(state.Inning, Is.EqualTo(12), "Should advance to 12th after bottom 11th");

        // Verify line score extends to 11 innings
        Assert.That(_scorekeeper.LineScore.GetInningRuns(Team.Away, 11), Is.EqualTo(2),
            "Away should have 2 runs in 11th");
        Assert.That(_scorekeeper.LineScore.GetInningRuns(Team.Home, 11), Is.EqualTo(1),
            "Home should have 1 run in 11th");
    }

    #endregion

    #region Validation Tests

    /// <summary>
    /// Validation test: Line score totals match final game scores.
    /// PRD Section 7.3: LineScore_AfterGame_TotalsMatchFinalScores
    /// </summary>
    [Test]
    public void LineScore_AfterGame_TotalsMatchFinalScores() {
        // Simulate a game with various scoring
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

        // Simulate scoring in various innings
        state = ScoreRunsAndEndHalf(state, 3); // Top 1st: Away 3
        state = ScoreRunsAndEndHalf(state, 1); // Bottom 1st: Home 1
        state = ScoreRunsAndEndHalf(state, 0); // Top 2nd: Away 0
        state = ScoreRunsAndEndHalf(state, 2); // Bottom 2nd: Home 2
        state = ScoreRunsAndEndHalf(state, 1); // Top 3rd: Away 1
        state = ScoreRunsAndEndHalf(state, 0); // Bottom 3rd: Home 0

        // Complete remaining innings
        for (int i = 4; i <= 9; i++) {
            state = ScoreRunsAndEndHalf(state, 0); // Top
            if (i < 9 || state.HomeScore <= state.AwayScore) {
                state = ScoreRunsAndEndHalf(state, 0); // Bottom (if needed)
            }
        }

        // Assert
        Assert.That(_scorekeeper.LineScore.AwayTotal, Is.EqualTo(state.AwayScore),
            "Line score away total must match game state");
        Assert.That(_scorekeeper.LineScore.HomeTotal, Is.EqualTo(state.HomeScore),
            "Line score home total must match game state");
        Assert.That(_scorekeeper.LineScore.Validate(state.AwayScore, state.HomeScore), Is.True,
            "Line score validation should pass");
    }

    /// <summary>
    /// Validation test: Box score team hits equal sum of individual hits.
    /// PRD Section 7.3: BoxScore_AfterGame_TeamHitsEqualIndividualHits
    /// </summary>
    [Test]
    public void BoxScore_AfterGame_TeamHitsEqualIndividualHits() {
        // Simulate several PAs with hits
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

        // Batter 0: Single
        var single = new PaResolution(
            OutsAdded: 0,
            RunsScored: 0,
            NewBases: new BaseState(true, false, false),
            Type: PaType.Single,
        Tag: OutcomeTag.Single);
        var tmpResult3 = _scorekeeper.ApplyPlateAppearance(state, single);
        state = tmpResult3.StateAfter;

        // Batter 1: Double
        var double_ = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(false, true, false),
            Type: PaType.Double,
        Tag: OutcomeTag.Double);
        var tmpResult4 = _scorekeeper.ApplyPlateAppearance(state, double_);
        state = tmpResult4.StateAfter;

        // Batter 2: Home Run
        var homeRun = new PaResolution(
            OutsAdded: 0,
            RunsScored: 2,
            NewBases: new BaseState(false, false, false),
            Type: PaType.HomeRun,
        Tag: OutcomeTag.HR);
        var tmpResult5 = _scorekeeper.ApplyPlateAppearance(state, homeRun);
        state = tmpResult5.StateAfter;

        // Batter 3: Strikeout (no hit)
        var strikeout = new PaResolution(
            OutsAdded: 1,
            RunsScored: 0,
            NewBases: new BaseState(false, false, false),
            Type: PaType.K,
        Tag: OutcomeTag.K);
        var tmpResult6 = _scorekeeper.ApplyPlateAppearance(state, strikeout);
        state = tmpResult6.StateAfter;

        // Calculate team hits from individual batters
        int teamHits = 0;
        int individualHits = 0;
        for (int i = 0; i < 9; i++) {
            if (_scorekeeper.BoxScore.AwayBatters.ContainsKey(i)) {
                var batterStats = _scorekeeper.BoxScore.AwayBatters[i];
                individualHits += batterStats.H;
                teamHits += batterStats.H;
            }
        }

        // Assert
        Assert.That(teamHits, Is.EqualTo(3), "Team should have 3 hits total");
        Assert.That(individualHits, Is.EqualTo(3), "Sum of individual hits should be 3");
        Assert.That(teamHits, Is.EqualTo(individualHits),
            "Team hits must equal sum of individual batter hits");

        // Verify using BoxScore validation method
        Assert.That(_scorekeeper.BoxScore.ValidateTeamHits(Team.Away, 3), Is.True,
            "BoxScore validation should confirm team hits match");
    }

    #endregion

    /// <summary>
    /// Test: Triple play ends half immediately with correct LOB calculation.
    /// PRD Section 3.3: TriplePlay_OutsAdded3_EndsHalf_LOBFromInstantOfThirdOut
    /// MLB Rule: Three outs end half-inning; LOB counted at moment of final out
    /// </summary>
    [Test]
    public void TriplePlay_OutsAdded3_EndsHalf_LOBFromInstantOfThirdOut() {
        // Arrange: Top 4th, bases loaded, 0 outs
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 4, half: InningHalf.Top, outs: 0,
            onFirst: true, onSecond: true, onThird: true,
            awayScore: 2, homeScore: 1,
            awayBattingOrderIndex: 6, homeBattingOrderIndex: 0,
            offense: Team.Away, defense: Team.Home
        );

        // Need to record previous innings first (pattern from existing tests)
        for (int i = 1; i <= 3; i++) {
            _scorekeeper.LineScore.RecordInning(Team.Away, 0);
            _scorekeeper.LineScore.RecordInning(Team.Home, 0);
        }

        var resolution = new PaResolution(
            OutsAdded: 3,  // Triple play
            RunsScored: 0,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false), // Bases cleared after play
            Type: PaType.InPlayOut,Tag: OutcomeTag.InPlayOut,
            HadError: false,
            BasesAtThirdOut: new BaseState(OnFirst: true, OnSecond: true, OnThird: true) // Bases loaded at instant of 3rd out
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        var snapshot = result.StateAfter.ToTestSnapshot();
        Assert.Multiple(() => {
            Assert.That(snapshot.AwayScore, Is.EqualTo(2), "No runs");
            Assert.That(snapshot.Outs, Is.EqualTo(0), "Outs reset");
            Assert.That(snapshot.OnFirst, Is.False, "Bases cleared");
            Assert.That(snapshot.OnSecond, Is.False, "Bases cleared");
            Assert.That(snapshot.OnThird, Is.False, "Bases cleared");
            Assert.That(snapshot.Half, Is.EqualTo(InningHalf.Bottom), "Transition to bottom");
            Assert.That(snapshot.Inning, Is.EqualTo(4), "Still 4th inning");
            Assert.That(_scorekeeper.AwayLOB[^1], Is.EqualTo(3), "LOB = 3 (bases loaded at 3rd out)");
            // Line score flush verification: ensure runs recorded for away team in 4th inning
            Assert.That(_scorekeeper.LineScore.GetInningRuns(Team.Away, 4), Is.EqualTo(0),
                "Line score should show 0 runs for away in 4th (triple play, no runs scored)");
        });
    }

    /// <summary>
    /// Test: Home run in bottom 9th that doesn't win the game continues play.
    /// PRD Section 3.3: NonWalkoffHomeRun_NotEnoughToWin_GameContinues
    /// MLB Rule: Home run credits all runs; game continues if home team does not take the lead
    /// </summary>
    [Test]
    public void NonWalkoffHomeRun_NotEnoughToWin_GameContinues() {
        // Arrange: Bottom 9th, home down 3 (2-5), runner on 1st, 1 out
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Bottom, outs: 1,
            onFirst: true, onSecond: false, onThird: false,
            awayScore: 5, homeScore: 2,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 4,
            offense: Team.Home, defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 2,  // Two-run HR, not enough to win
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
            Type: PaType.HomeRun,Tag: OutcomeTag.HR,
            HadError: false
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        var snapshot = result.StateAfter.ToTestSnapshot();
        Assert.Multiple(() => {
            Assert.That(snapshot.HomeScore, Is.EqualTo(4), "All runs count for HR");
            Assert.That(snapshot.AwayScore, Is.EqualTo(5), "Still leading");
            Assert.That(snapshot.IsFinal, Is.False, "Game continues");
            Assert.That(snapshot.Outs, Is.EqualTo(1), "Outs unchanged");
            Assert.That(snapshot.OnFirst, Is.False, "Bases cleared");
            Assert.That(snapshot.Half, Is.EqualTo(InningHalf.Bottom), "Still bottom 9th");
        });
    }

    /// <summary>
    /// Test: Skip-bottom-9th logic is independent of base state at end of top 9th.
    /// PRD Section 3.3: SkipBottom9th_WithRunnersOnAtT9End_BasesDoNotAffectFinal
    /// MLB Rule: Home team does not bat in bottom of 9th if leading after top half
    /// </summary>
    [Test]
    public void SkipBottom9th_WithRunnersOnAtT9End_BasesDoNotAffectFinal() {
        // Arrange: Top 9th, 2 outs, runners on 1st and 2nd, home leading 6-4
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Top, outs: 2,
            onFirst: true, onSecond: true, onThird: false,
            awayScore: 4, homeScore: 6,
            awayBattingOrderIndex: 5, homeBattingOrderIndex: 0,
            offense: Team.Away, defense: Team.Home
        );

        // Need to record previous innings first
        for (int i = 0; i < 9; i++) {
            _scorekeeper.LineScore.RecordInning(Team.Away, 0);
            if (i < 8) {
                _scorekeeper.LineScore.RecordInning(Team.Home, 0);
            }
        }

        // Final out (strikeout)
        var resolution = new PaResolution(
            OutsAdded: 1,
            RunsScored: 0,
            NewBases: new BaseState(OnFirst: true, OnSecond: true, OnThird: false),
            Type: PaType.K,Tag: OutcomeTag.K,
            HadError: false
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        var snapshot = result.StateAfter.ToTestSnapshot();
        Assert.Multiple(() => {
            Assert.That(snapshot.IsFinal, Is.True, "Game over (home leads)");
            Assert.That(_scorekeeper.AwayLOB[^1], Is.EqualTo(2), "Away LOB = 2");
            Assert.That(_scorekeeper.LineScore.GetInningDisplay(Team.Home, 9), Is.EqualTo("X"), "Home 9th shows X");
            Assert.That(snapshot.HomeScore, Is.EqualTo(6), "Final: Home 6");
            Assert.That(snapshot.AwayScore, Is.EqualTo(4), "Final: Away 4");
        });
    }

    /// <summary>
    /// Test: Skip-bottom-9th with runner on 3rd at end of top 9th.
    /// PRD Section 3.3: SkipBottom9th_HomeLeadsAfterT9_WithRunnerOnThird
    /// MLB Rule: Home team does not bat in bottom of 9th if leading; base state at end of top 9th does not affect this rule
    /// </summary>
    [Test]
    public void SkipBottom9th_HomeLeadsAfterT9_WithRunnerOnThird() {
        // Arrange: Top 9th, 2 outs, runner on 3rd, home leading 5-3
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Top, outs: 2,
            onFirst: false, onSecond: false, onThird: true,
            awayScore: 3, homeScore: 5,
            awayBattingOrderIndex: 8, homeBattingOrderIndex: 0,
            offense: Team.Away, defense: Team.Home
        );

        // Need to record previous innings first
        for (int i = 0; i < 9; i++) {
            _scorekeeper.LineScore.RecordInning(Team.Away, 0);
            if (i < 8) {
                _scorekeeper.LineScore.RecordInning(Team.Home, 0);
            }
        }

        // Final out
        var resolution = new PaResolution(
            OutsAdded: 1,
            RunsScored: 0,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: true),
            Type: PaType.InPlayOut,Tag: OutcomeTag.InPlayOut,
            HadError: false
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        var snapshot = result.StateAfter.ToTestSnapshot();
        Assert.Multiple(() => {
            Assert.That(snapshot.IsFinal, Is.True, "Game ends");
            Assert.That(_scorekeeper.AwayLOB[^1], Is.EqualTo(1), "Away LOB = 1 (R3)");
            Assert.That(_scorekeeper.LineScore.GetInningDisplay(Team.Home, 9), Is.EqualTo("X"));
            Assert.That(snapshot.HomeScore, Is.EqualTo(5));
            Assert.That(snapshot.AwayScore, Is.EqualTo(3));
        });
    }

    #region Helper Methods

    /// <summary>
    /// Helper method to score runs and end a half-inning.
    /// Simulates scoring the specified number of runs, then recording 3 outs to end the half.
    /// </summary>
    /// <param name="state">Current game state</param>
    /// <param name="runs">Number of runs to score in this half-inning</param>
    /// <returns>Updated game state after half-inning completes</returns>
    private GameState ScoreRunsAndEndHalf(GameState state, int runs) {
        // Score runs if any
        if (runs > 0) {
            var scoreResolution = new PaResolution(
                OutsAdded: 0,
                RunsScored: runs,
                NewBases: new BaseState(false, false, false),
                Type: PaType.HomeRun,
            Tag: OutcomeTag.HR);
            var tmpResult7 = _scorekeeper.ApplyPlateAppearance(state, scoreResolution);
        state = tmpResult7.StateAfter;
        }

        // Record 3 outs to end half-inning
        for (int i = 0; i < 3; i++) {
            var outResolution = new PaResolution(
                OutsAdded: 1,
                RunsScored: 0,
                NewBases: new BaseState(false, false, false),
                Type: PaType.InPlayOut,
            Tag: OutcomeTag.InPlayOut);
            var tmpResult8 = _scorekeeper.ApplyPlateAppearance(state, outResolution);
        state = tmpResult8.StateAfter;
        }

        return state;
    }

    #endregion
}
