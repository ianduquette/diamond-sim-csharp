using DiamondSim.Tests.TestHelpers;

namespace DiamondSim.Tests.Scoring;

/// <summary>
/// Tests for RBI (Runs Batted In) attribution according to official baseball rules.
/// Validates that RBI is computed explicitly by the scorer and never inferred from runs scored.
/// </summary>
[TestFixture]
[Category("Scoring")]
public class RbiAttributionTests {

    [Test]
    public void RBI_ROE_IsZero() {
        // Arrange: Runner on 3rd, <2 outs, reach on error
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            outs: 1,
            onThird: true
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.ReachOnError,
            Tag: OutcomeTag.ROE
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(1), "Run should count");
        var batterStats = scorer.BoxScore.AwayBatters[0];
        Assert.That(batterStats.RBI, Is.EqualTo(0), "RBI should be 0 for ROE per Rule 9.06(g)");
    }

    [Test]
    public void RBI_BasesLoadedWalk_IsOne() {
        // Arrange: Bases loaded, walk
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            onFirst: true,
            onSecond: true,
            onThird: true
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: true, OnThird: true),
            Type: PaType.BB,
            Tag: OutcomeTag.BB
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(1), "Run should count");
        var batterStats = scorer.BoxScore.AwayBatters[0];
        Assert.That(batterStats.RBI, Is.EqualTo(1), "RBI should be exactly 1 for bases-loaded walk per Rule 9.04(a)(2)");
    }

    [Test]
    public void RBI_BasesLoadedHbp_IsOne() {
        // Arrange: Bases loaded, HBP
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            onFirst: true,
            onSecond: true,
            onThird: true,
            awayBattingOrderIndex: 1
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: true, OnThird: true),
            Type: PaType.HBP,
            Tag: OutcomeTag.HBP
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(1), "Run should count");
        var batterStats = scorer.BoxScore.AwayBatters[1];
        Assert.That(batterStats.RBI, Is.EqualTo(1), "RBI should be exactly 1 for bases-loaded HBP per Rule 9.04(a)(2)");
    }

    [Test]
    public void RBI_SacFly_IsOne() {
        // Arrange: Runner on 3rd, <2 outs, sacrifice fly
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            outs: 1,
            onThird: true,
            awayBattingOrderIndex: 2
        );

        var resolution = new PaResolution(
            OutsAdded: 1,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
            Type: PaType.InPlayOut,
            Tag: OutcomeTag.SF,
            Flags: new PaFlags(IsDoublePlay: false, IsSacFly: true)
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(1), "Run should count");
        Assert.That(result.StateAfter.Outs, Is.EqualTo(2), "Outs should increment");
        var batterStats = scorer.BoxScore.AwayBatters[2];
        Assert.That(batterStats.RBI, Is.EqualTo(1), "RBI should be exactly 1 for sac fly per Rule 9.04(a)(3)");
    }

    [Test]
    public void RBI_HomeRun_AllRunnersPlusBatter() {
        // Arrange: Bases loaded, home run
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            onFirst: true,
            onSecond: true,
            onThird: true,
            awayBattingOrderIndex: 3
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 4,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
            Type: PaType.HomeRun,
            Tag: OutcomeTag.HR
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(4), "All 4 runs should count");
        var batterStats = scorer.BoxScore.AwayBatters[3];
        Assert.That(batterStats.RBI, Is.EqualTo(4), "RBI should be 4 (all runners + batter) per Rule 9.04(a)(1)");
    }

    [Test]
    public void RBI_WalkoffSingle_UsesClampedRuns() {
        // Arrange: Bottom 9th, tie game, runner on 3rd, single
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            inning: 9,
            half: InningHalf.Bottom,
            outs: 1,
            onThird: true,
            awayScore: 3,
            homeScore: 3,
            homeBattingOrderIndex: 4
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.Single,
            Tag: OutcomeTag.Single
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.HomeScore, Is.EqualTo(4), "Only 1 run should count (walk-off clamping)");
        Assert.That(result.StateAfter.IsFinal, Is.True, "Game should be final");
        var batterStats = scorer.BoxScore.HomeBatters[4];
        Assert.That(batterStats.RBI, Is.EqualTo(1), "RBI should be 1 (clamped) for walk-off single");
    }

    [Test]
    public void RBI_WalkoffHomeRun_AllRunsCount() {
        // Arrange: Bottom 9th, down 2, bases loaded, home run
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            inning: 9,
            half: InningHalf.Bottom,
            outs: 2,
            onFirst: true,
            onSecond: true,
            onThird: true,
            awayScore: 5,
            homeScore: 3,
            homeBattingOrderIndex: 5
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 4,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
            Type: PaType.HomeRun,
            Tag: OutcomeTag.HR
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.HomeScore, Is.EqualTo(7), "All 4 runs should count (HR exception to walk-off clamping)");
        Assert.That(result.StateAfter.IsFinal, Is.True, "Game should be final");
        var batterStats = scorer.BoxScore.HomeBatters[5];
        Assert.That(batterStats.RBI, Is.EqualTo(4), "RBI should be 4 (HR exception) for walk-off grand slam");
    }

    [Test]
    public void RBI_Double_TwoScore_CreditsTwo() {
        // Arrange: Runners on 2nd and 3rd, double (both score)
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            onSecond: true,
            onThird: true,
            awayBattingOrderIndex: 6
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 2,
            NewBases: new BaseState(OnFirst: false, OnSecond: true, OnThird: false),
            Type: PaType.Double,
            Tag: OutcomeTag.Double
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(2), "Both runs should count");
        var batterStats = scorer.BoxScore.AwayBatters[6];
        Assert.That(batterStats.RBI, Is.EqualTo(2), "RBI should be exactly 2 for two-run double");
    }

    [Test]
    public void RBI_WalkNotBasesLoaded_IsZero() {
        // Arrange: Runner on 1st only, walk (no run scores)
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            onFirst: true,
            awayBattingOrderIndex: 7
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 0,
            NewBases: new BaseState(OnFirst: true, OnSecond: true, OnThird: false),
            Type: PaType.BB,
            Tag: OutcomeTag.BB
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(0), "No runs should score");
        var batterStats = scorer.BoxScore.AwayBatters[7];
        Assert.That(batterStats.RBI, Is.EqualTo(0), "RBI should be 0 for walk without bases loaded");
    }

    [Test]
    public void RBI_SingleNoRunsScore_IsZero() {
        // Arrange: Bases empty, single
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            awayBattingOrderIndex: 8
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 0,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.Single,
            Tag: OutcomeTag.Single
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(0), "No runs should score");
        var batterStats = scorer.BoxScore.AwayBatters[8];
        Assert.That(batterStats.RBI, Is.EqualTo(0), "RBI should be 0 when no runs score");
    }

    /// <summary>
    /// Grounded-into-double-play (GIDP) with a run scoring should NOT credit an RBI to the batter.
    /// MLB Rule 9.04 exception.
    /// </summary>
    [Test]
    public void RBI_GIDP_RunScores_NoRBI() {
        // Arrange: Bases loaded, <2 outs. Batter grounds into DP, runner from 3rd scores.
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            inning: 2,
            onFirst: true,
            onSecond: true,
            onThird: true,
            awayBattingOrderIndex: 4
        );

        var resolution = new PaResolution(
            OutsAdded: 2,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: false, OnSecond: true, OnThird: false), // 6-4-3 DP; R3 crosses
            Type: PaType.InPlayOut,
            Tag: OutcomeTag.InPlayOut,
            Flags: new PaFlags(IsDoublePlay: true, IsSacFly: false)
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(1), "Run from third should score.");
        var batterStats = scorer.BoxScore.AwayBatters[4];
        Assert.That(batterStats.RBI, Is.EqualTo(0), "No RBI on a GIDP even if a run scores.");
    }
    /// <summary>
    /// Strikeout should never produce RBI, even if a run scores on a wild pitch/passed ball.
    /// Those runs are charged to the pitcher/catcher, not the batter.
    /// </summary>
    [Test]
    public void RBI_Strikeout_IsZero() {
        // Arrange: Runner on 3rd, strikeout (no run scores)
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            outs: 1,
            onThird: true,
            awayBattingOrderIndex: 5
        );

        var resolution = new PaResolution(
            OutsAdded: 1,
            RunsScored: 0,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: true),
            Type: PaType.K,
            Tag: OutcomeTag.K
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(0), "No runs should score on strikeout");
        Assert.That(result.StateAfter.Outs, Is.EqualTo(2), "Outs should increment");
        var batterStats = scorer.BoxScore.AwayBatters[5];
        Assert.That(batterStats.RBI, Is.EqualTo(0), "RBI should be 0 for strikeout");
    }

    /// <summary>
    /// Triple with multiple runners on base should credit RBI for all runs scored.
    /// </summary>
    [Test]
    public void RBI_Triple_TwoRunnersScore() {
        // Arrange: Runners on 1st and 2nd, triple (both score)
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            onFirst: true,
            onSecond: true,
            awayBattingOrderIndex: 6
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 2,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: true),
            Type: PaType.Triple,
            Tag: OutcomeTag.Triple
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(2), "Both runs should count");
        var batterStats = scorer.BoxScore.AwayBatters[6];
        Assert.That(batterStats.RBI, Is.EqualTo(2), "RBI should be 2 for triple with two runners scoring");
    }

    /// <summary>
    /// Single with runner on 2nd scoring should credit 1 RBI.
    /// </summary>
    [Test]
    public void RBI_Single_RunnerFromSecondScores() {
        // Arrange: Runner on 2nd, single (runner scores)
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            onSecond: true,
            awayBattingOrderIndex: 7
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.Single,
            Tag: OutcomeTag.Single
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(1), "Run should count");
        var batterStats = scorer.BoxScore.AwayBatters[7];
        Assert.That(batterStats.RBI, Is.EqualTo(1), "RBI should be 1 for single with runner from 2nd scoring");
    }

    /// <summary>
    /// HBP without bases loaded should not produce RBI.
    /// HBP only produces RBI when bases are loaded per Rule 9.04(a)(2).
    /// </summary>
    [Test]
    public void RBI_HbpNotBasesLoaded_IsZero() {
        // Arrange: Runner on 1st only, HBP (no run scores)
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            onFirst: true,
            awayBattingOrderIndex: 8
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 0,
            NewBases: new BaseState(OnFirst: true, OnSecond: true, OnThird: false),
            Type: PaType.HBP,
            Tag: OutcomeTag.HBP
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(0), "No runs should score");
        var batterStats = scorer.BoxScore.AwayBatters[8];
        Assert.That(batterStats.RBI, Is.EqualTo(0), "RBI should be 0 for HBP without bases loaded");
    }

    /// <summary>
    /// Ground out with runner on 3rd scoring should credit 1 RBI.
    /// Regular outs (not DP, not SF) can still produce RBI if a run scores.
    /// </summary>
    [Test]
    public void RBI_GroundOut_RunnerFromThirdScores() {
        // Arrange: Runner on 3rd, ground out (runner scores)
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            outs: 1,
            onThird: true,
            awayBattingOrderIndex: 0
        );

        var resolution = new PaResolution(
            OutsAdded: 1,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
            Type: PaType.InPlayOut,
            Tag: OutcomeTag.InPlayOut,
            Flags: new PaFlags(IsDoublePlay: false, IsSacFly: false)
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(1), "Run should count");
        Assert.That(result.StateAfter.Outs, Is.EqualTo(2), "Outs should increment");
        var batterStats = scorer.BoxScore.AwayBatters[0];
        Assert.That(batterStats.RBI, Is.EqualTo(1), "RBI should be 1 for ground out with runner from 3rd scoring");
    }

    /// <summary>
    /// Walk-off double should clamp RBI to runs needed to win, not all runs that could score.
    /// </summary>
    [Test]
    public void RBI_WalkoffDouble_UsesClampedRuns() {
        // Arrange: Bottom 9th, tie game, bases loaded, double (2+ runs could score)
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            inning: 9,
            half: InningHalf.Bottom,
            outs: 1,
            onFirst: true,
            onSecond: true,
            onThird: true,
            awayScore: 4,
            homeScore: 4,
            homeBattingOrderIndex: 1
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 2, // 2 runs could score, but only 1 needed
            NewBases: new BaseState(OnFirst: false, OnSecond: true, OnThird: true),
            Type: PaType.Double,
            Tag: OutcomeTag.Double
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.HomeScore, Is.EqualTo(5), "Only 1 run should count (walk-off clamping)");
        Assert.That(result.StateAfter.IsFinal, Is.True, "Game should be final");
        var batterStats = scorer.BoxScore.HomeBatters[1];
        Assert.That(batterStats.RBI, Is.EqualTo(1), "RBI should be 1 (clamped) for walk-off double");
    }

    /// <summary>
    /// Walk-off triple should clamp RBI to runs needed to win.
    /// </summary>
    [Test]
    public void RBI_WalkoffTriple_UsesClampedRuns() {
        // Arrange: Bottom 9th, down 1, runners on 1st and 2nd, triple (2 runs could score)
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            inning: 9,
            half: InningHalf.Bottom,
            outs: 2,
            onFirst: true,
            onSecond: true,
            awayScore: 5,
            homeScore: 4,
            homeBattingOrderIndex: 2
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 2, // 2 runs score, but only need 2 to win
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: true),
            Type: PaType.Triple,
            Tag: OutcomeTag.Triple
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.HomeScore, Is.EqualTo(6), "2 runs should count (exactly what's needed)");
        Assert.That(result.StateAfter.IsFinal, Is.True, "Game should be final");
        var batterStats = scorer.BoxScore.HomeBatters[2];
        Assert.That(batterStats.RBI, Is.EqualTo(2), "RBI should be 2 (clamped to runs needed) for walk-off triple");
    }

    /// <summary>
    /// Solo home run should credit 1 RBI (batter only).
    /// </summary>
    [Test]
    public void RBI_HomeRun_Solo_IsOne() {
        // Arrange: Bases empty, home run
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            awayBattingOrderIndex: 3
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
            Type: PaType.HomeRun,
            Tag: OutcomeTag.HR
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(1), "1 run should count");
        var batterStats = scorer.BoxScore.AwayBatters[3];
        Assert.That(batterStats.RBI, Is.EqualTo(1), "RBI should be 1 for solo home run");
    }

    /// <summary>
    /// Two-run home run should credit 2 RBI (runner + batter).
    /// </summary>
    [Test]
    public void RBI_HomeRun_TwoRun_IsTwo() {
        // Arrange: Runner on 1st, home run
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            onFirst: true,
            awayBattingOrderIndex: 4
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 2,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
            Type: PaType.HomeRun,
            Tag: OutcomeTag.HR
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(2), "2 runs should count");
        var batterStats = scorer.BoxScore.AwayBatters[4];
        Assert.That(batterStats.RBI, Is.EqualTo(2), "RBI should be 2 for two-run home run");
    }

    /// <summary>
    /// Three-run home run should credit 3 RBI (2 runners + batter).
    /// </summary>
    [Test]
    public void RBI_HomeRun_ThreeRun_IsThree() {
        // Arrange: Runners on 1st and 2nd, home run
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            onFirst: true,
            onSecond: true,
            awayBattingOrderIndex: 5
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 3,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
            Type: PaType.HomeRun,
            Tag: OutcomeTag.HR
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(3), "3 runs should count");
        var batterStats = scorer.BoxScore.AwayBatters[5];
        Assert.That(batterStats.RBI, Is.EqualTo(3), "RBI should be 3 for three-run home run");
    }

    /// <summary>
    /// Double with runner on 3rd only should credit 1 RBI.
    /// </summary>
    [Test]
    public void RBI_Double_RunnerOnThird_IsOne() {
        // Arrange: Runner on 3rd only, double
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            onThird: true,
            awayBattingOrderIndex: 6
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: false, OnSecond: true, OnThird: false),
            Type: PaType.Double,
            Tag: OutcomeTag.Double
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(1), "1 run should count");
        var batterStats = scorer.BoxScore.AwayBatters[6];
        Assert.That(batterStats.RBI, Is.EqualTo(1), "RBI should be 1 for double with runner on 3rd");
    }

    /// <summary>
    /// Double with bases loaded where 3 runs score should credit 3 RBI.
    /// </summary>
    [Test]
    public void RBI_Double_BasesLoaded_ThreeScore() {
        // Arrange: Bases loaded, double (3 runs score)
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            onFirst: true,
            onSecond: true,
            onThird: true,
            awayBattingOrderIndex: 7
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 3,
            NewBases: new BaseState(OnFirst: false, OnSecond: true, OnThird: false),
            Type: PaType.Double,
            Tag: OutcomeTag.Double
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(3), "3 runs should count");
        var batterStats = scorer.BoxScore.AwayBatters[7];
        Assert.That(batterStats.RBI, Is.EqualTo(3), "RBI should be 3 for double with bases loaded, 3 scoring");
    }

    /// <summary>
    /// Double with no runs scoring should credit 0 RBI.
    /// </summary>
    [Test]
    public void RBI_Double_NoRunsScore_IsZero() {
        // Arrange: Runner on 1st only, double (no runs score)
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            onFirst: true,
            awayBattingOrderIndex: 8
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 0,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: true),
            Type: PaType.Double,
            Tag: OutcomeTag.Double
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(0), "No runs should score");
        var batterStats = scorer.BoxScore.AwayBatters[8];
        Assert.That(batterStats.RBI, Is.EqualTo(0), "RBI should be 0 when no runs score on double");
    }

    /// <summary>
    /// Sacrifice fly with 2 outs should still credit 1 RBI.
    /// </summary>
    [Test]
    public void RBI_SacFly_TwoOuts_IsOne() {
        // Arrange: Runner on 3rd, 2 outs, sacrifice fly
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            outs: 2,
            onThird: true,
            awayBattingOrderIndex: 0
        );

        var resolution = new PaResolution(
            OutsAdded: 1,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
            Type: PaType.InPlayOut,
            Tag: OutcomeTag.SF,
            Flags: new PaFlags(IsDoublePlay: false, IsSacFly: true)
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(1), "Run should count");
        Assert.That(result.StateAfter.Outs, Is.EqualTo(0), "Inning should end (3 outs)");
        var batterStats = scorer.BoxScore.AwayBatters[0];
        Assert.That(batterStats.RBI, Is.EqualTo(1), "RBI should be 1 for sac fly even with 2 outs");
    }

    /// <summary>
    /// Sacrifice fly with multiple runners should only credit 1 RBI (for runner from 3rd).
    /// </summary>
    [Test]
    public void RBI_SacFly_MultipleRunners_OnlyOneRBI() {
        // Arrange: Runners on 2nd and 3rd, <2 outs, sacrifice fly (only runner from 3rd scores)
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            outs: 1,
            onSecond: true,
            onThird: true,
            awayBattingOrderIndex: 1
        );

        var resolution = new PaResolution(
            OutsAdded: 1,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: true),
            Type: PaType.InPlayOut,
            Tag: OutcomeTag.SF,
            Flags: new PaFlags(IsDoublePlay: false, IsSacFly: true)
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(1), "1 run should count");
        Assert.That(result.StateAfter.Outs, Is.EqualTo(2), "Outs should increment");
        var batterStats = scorer.BoxScore.AwayBatters[1];
        Assert.That(batterStats.RBI, Is.EqualTo(1), "RBI should be exactly 1 for sac fly regardless of other runners");
    }
    /// <summary>
    /// Single with bases loaded where 2 runs score should credit 2 RBI.
    /// </summary>
    [Test]
    public void RBI_Single_BasesLoaded_TwoScore() {
        // Arrange: Bases loaded, single (2 runs score)
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            onFirst: true,
            onSecond: true,
            onThird: true,
            awayBattingOrderIndex: 2
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 2,
            NewBases: new BaseState(OnFirst: true, OnSecond: true, OnThird: false),
            Type: PaType.Single,
            Tag: OutcomeTag.Single
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(2), "2 runs should count");
        var batterStats = scorer.BoxScore.AwayBatters[2];
        Assert.That(batterStats.RBI, Is.EqualTo(2), "RBI should be 2 for single with bases loaded, 2 scoring");
    }

    /// <summary>
    /// Walk-off single with 2 outs should still clamp RBI.
    /// </summary>
    [Test]
    public void RBI_WalkoffSingle_TwoOuts_UsesClampedRuns() {
        // Arrange: Bottom 9th, 2 outs, tie game, runner on 2nd, single
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            inning: 9,
            half: InningHalf.Bottom,
            outs: 2,
            onSecond: true,
            awayScore: 2,
            homeScore: 2,
            homeBattingOrderIndex: 3
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.Single,
            Tag: OutcomeTag.Single
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.HomeScore, Is.EqualTo(3), "Only 1 run should count (walk-off clamping)");
        Assert.That(result.StateAfter.IsFinal, Is.True, "Game should be final");
        var batterStats = scorer.BoxScore.HomeBatters[3];
        Assert.That(batterStats.RBI, Is.EqualTo(1), "RBI should be 1 (clamped) for walk-off single with 2 outs");
    }

    /// <summary>
    /// Walk-off in extra innings (10th) should clamp RBI.
    /// </summary>
    [Test]
    public void RBI_WalkoffExtraInnings_UsesClampedRuns() {
        // Arrange: Bottom 10th, tie game, runner on 3rd, single
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            inning: 10,
            half: InningHalf.Bottom,
            outs: 1,
            onThird: true,
            awayScore: 5,
            homeScore: 5,
            homeBattingOrderIndex: 4
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.Single,
            Tag: OutcomeTag.Single
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.HomeScore, Is.EqualTo(6), "Only 1 run should count (walk-off clamping)");
        Assert.That(result.StateAfter.IsFinal, Is.True, "Game should be final");
        var batterStats = scorer.BoxScore.HomeBatters[4];
        Assert.That(batterStats.RBI, Is.EqualTo(1), "RBI should be 1 (clamped) for walk-off in extra innings");
    }
    /// <summary>
    /// Walk with bases empty should produce 0 RBI.
    /// </summary>
    [Test]
    public void RBI_WalkEmptyBases_IsZero() {
        // Arrange: Bases empty, walk
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            awayBattingOrderIndex: 5
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 0,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.BB,
            Tag: OutcomeTag.BB
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(0), "No runs should score");
        var batterStats = scorer.BoxScore.AwayBatters[5];
        Assert.That(batterStats.RBI, Is.EqualTo(0), "RBI should be 0 for walk with bases empty");
    }

    /// <summary>
    /// HBP with bases empty should produce 0 RBI.
    /// </summary>
    [Test]
    public void RBI_HbpEmptyBases_IsZero() {
        // Arrange: Bases empty, HBP
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            awayBattingOrderIndex: 6
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 0,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.HBP,
            Tag: OutcomeTag.HBP
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(0), "No runs should score");
        var batterStats = scorer.BoxScore.AwayBatters[6];
        Assert.That(batterStats.RBI, Is.EqualTo(0), "RBI should be 0 for HBP with bases empty");
    }

    /// <summary>
    /// Strikeout with bases empty should produce 0 RBI.
    /// </summary>
    [Test]
    public void RBI_StrikeoutEmptyBases_IsZero() {
        // Arrange: Bases empty, strikeout
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            outs: 1,
            awayBattingOrderIndex: 7
        );

        var resolution = new PaResolution(
            OutsAdded: 1,
            RunsScored: 0,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
            Type: PaType.K,
            Tag: OutcomeTag.K
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(0), "No runs should score");
        Assert.That(result.StateAfter.Outs, Is.EqualTo(2), "Outs should increment");
        var batterStats = scorer.BoxScore.AwayBatters[7];
        Assert.That(batterStats.RBI, Is.EqualTo(0), "RBI should be 0 for strikeout with bases empty");
    }

    /// <summary>
    /// InPlayOut with bases empty should produce 0 RBI.
    /// </summary>
    [Test]
    public void RBI_InPlayOutEmptyBases_IsZero() {
        // Arrange: Bases empty, ground out
        var scorer = new InningScorekeeper();
        var state = GameStateTestHelper.CreateGameState(
            outs: 0,
            awayBattingOrderIndex: 8
        );

        var resolution = new PaResolution(
            OutsAdded: 1,
            RunsScored: 0,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
            Type: PaType.InPlayOut,
            Tag: OutcomeTag.InPlayOut,
            Flags: new PaFlags(IsDoublePlay: false, IsSacFly: false)
        );

        // Act
        var result = scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(0), "No runs should score");
        Assert.That(result.StateAfter.Outs, Is.EqualTo(1), "Outs should increment");
        var batterStats = scorer.BoxScore.AwayBatters[8];
        Assert.That(batterStats.RBI, Is.EqualTo(0), "RBI should be 0 for out with bases empty");
    }



}
