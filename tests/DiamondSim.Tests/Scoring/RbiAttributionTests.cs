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

}
