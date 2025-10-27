namespace DiamondSim.Tests.Scoring;

/// <summary>
/// Tests for RBI (Runs Batted In) attribution according to official baseball rules.
/// Validates that RBI is computed explicitly by the scorer and never inferred from runs scored.
/// </summary>
[TestFixture]
[Category("Scoring")]
public class RbiAttributionTests {
    private InningScorekeeper _scorer = null!;

    [SetUp]
    public void Setup() {
        _scorer = new InningScorekeeper();
    }

    [Test]
    public void RBI_ROE_IsZero() {
        // Arrange: Runner on 3rd, <2 outs, reach on error
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 1,
            half: InningHalf.Top,
            outs: 1,
            onFirst: false,
            onSecond: false,
            onThird: true,
            awayScore: 0,
            homeScore: 0,
            awayBattingOrderIndex: 0,
            homeBattingOrderIndex: 0,
            offense: Team.Away,
            defense: Team.Home
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.ReachOnError
        );

        // Act
        var result = _scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.AwayScore, Is.EqualTo(1), "Run should count");
        var batterStats = _scorer.BoxScore.AwayBatters[0];
        Assert.That(batterStats.RBI, Is.EqualTo(0), "RBI should be 0 for ROE per Rule 9.06(g)");
    }

    [Test]
    public void RBI_BasesLoadedWalk_IsOne() {
        // Arrange: Bases loaded, walk
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 1,
            half: InningHalf.Top,
            outs: 0,
            onFirst: true,
            onSecond: true,
            onThird: true,
            awayScore: 0,
            homeScore: 0,
            awayBattingOrderIndex: 0,
            homeBattingOrderIndex: 0,
            offense: Team.Away,
            defense: Team.Home
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: true, OnThird: true),
            Type: PaType.BB
        );

        // Act
        var result = _scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.AwayScore, Is.EqualTo(1), "Run should count");
        var batterStats = _scorer.BoxScore.AwayBatters[0];
        Assert.That(batterStats.RBI, Is.EqualTo(1), "RBI should be exactly 1 for bases-loaded walk per Rule 9.04(a)(2)");
    }

    [Test]
    public void RBI_BasesLoadedHbp_IsOne() {
        // Arrange: Bases loaded, HBP
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 1,
            half: InningHalf.Top,
            outs: 0,
            onFirst: true,
            onSecond: true,
            onThird: true,
            awayScore: 0,
            homeScore: 0,
            awayBattingOrderIndex: 1,
            homeBattingOrderIndex: 0,
            offense: Team.Away,
            defense: Team.Home
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: true, OnThird: true),
            Type: PaType.HBP
        );

        // Act
        var result = _scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.AwayScore, Is.EqualTo(1), "Run should count");
        var batterStats = _scorer.BoxScore.AwayBatters[1];
        Assert.That(batterStats.RBI, Is.EqualTo(1), "RBI should be exactly 1 for bases-loaded HBP per Rule 9.04(a)(2)");
    }

    [Test]
    public void RBI_SacFly_IsOne() {
        // Arrange: Runner on 3rd, <2 outs, sacrifice fly
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 1,
            half: InningHalf.Top,
            outs: 1,
            onFirst: false,
            onSecond: false,
            onThird: true,
            awayScore: 0,
            homeScore: 0,
            awayBattingOrderIndex: 2,
            homeBattingOrderIndex: 0,
            offense: Team.Away,
            defense: Team.Home
        );

        var resolution = new PaResolution(
            OutsAdded: 1,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
            Type: PaType.InPlayOut,
            Flags: new PaFlags(IsDoublePlay: false, IsSacFly: true)
        );

        // Act
        var result = _scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.AwayScore, Is.EqualTo(1), "Run should count");
        Assert.That(result.Outs, Is.EqualTo(2), "Outs should increment");
        var batterStats = _scorer.BoxScore.AwayBatters[2];
        Assert.That(batterStats.RBI, Is.EqualTo(1), "RBI should be exactly 1 for sac fly per Rule 9.04(a)(3)");
    }

    [Test]
    public void RBI_HomeRun_AllRunnersPlusBatter() {
        // Arrange: Bases loaded, home run
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 1,
            half: InningHalf.Top,
            outs: 0,
            onFirst: true,
            onSecond: true,
            onThird: true,
            awayScore: 0,
            homeScore: 0,
            awayBattingOrderIndex: 3,
            homeBattingOrderIndex: 0,
            offense: Team.Away,
            defense: Team.Home
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 4,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
            Type: PaType.HomeRun
        );

        // Act
        var result = _scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.AwayScore, Is.EqualTo(4), "All 4 runs should count");
        var batterStats = _scorer.BoxScore.AwayBatters[3];
        Assert.That(batterStats.RBI, Is.EqualTo(4), "RBI should be 4 (all runners + batter) per Rule 9.04(a)(1)");
    }

    [Test]
    public void RBI_WalkoffSingle_UsesClampedRuns() {
        // Arrange: Bottom 9th, tie game, runner on 3rd, single
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 9,
            half: InningHalf.Bottom,
            outs: 1,
            onFirst: false,
            onSecond: false,
            onThird: true,
            awayScore: 3,
            homeScore: 3,
            awayBattingOrderIndex: 0,
            homeBattingOrderIndex: 4,
            offense: Team.Home,
            defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.Single
        );

        // Act
        var result = _scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.HomeScore, Is.EqualTo(4), "Only 1 run should count (walk-off clamping)");
        Assert.That(result.IsFinal, Is.True, "Game should be final");
        var batterStats = _scorer.BoxScore.HomeBatters[4];
        Assert.That(batterStats.RBI, Is.EqualTo(1), "RBI should be 1 (clamped) for walk-off single");
    }

    [Test]
    public void RBI_WalkoffHomeRun_AllRunsCount() {
        // Arrange: Bottom 9th, down 2, bases loaded, home run
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 9,
            half: InningHalf.Bottom,
            outs: 2,
            onFirst: true,
            onSecond: true,
            onThird: true,
            awayScore: 5,
            homeScore: 3,
            awayBattingOrderIndex: 0,
            homeBattingOrderIndex: 5,
            offense: Team.Home,
            defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 4,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
            Type: PaType.HomeRun
        );

        // Act
        var result = _scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.HomeScore, Is.EqualTo(7), "All 4 runs should count (HR exception to walk-off clamping)");
        Assert.That(result.IsFinal, Is.True, "Game should be final");
        var batterStats = _scorer.BoxScore.HomeBatters[5];
        Assert.That(batterStats.RBI, Is.EqualTo(4), "RBI should be 4 (HR exception) for walk-off grand slam");
    }

    [Test]
    public void RBI_Double_TwoScore_CreditsTwo() {
        // Arrange: Runners on 2nd and 3rd, double (both score)
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 1,
            half: InningHalf.Top,
            outs: 0,
            onFirst: false,
            onSecond: true,
            onThird: true,
            awayScore: 0,
            homeScore: 0,
            awayBattingOrderIndex: 6,
            homeBattingOrderIndex: 0,
            offense: Team.Away,
            defense: Team.Home
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 2,
            NewBases: new BaseState(OnFirst: false, OnSecond: true, OnThird: false),
            Type: PaType.Double
        );

        // Act
        var result = _scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.AwayScore, Is.EqualTo(2), "Both runs should count");
        var batterStats = _scorer.BoxScore.AwayBatters[6];
        Assert.That(batterStats.RBI, Is.EqualTo(2), "RBI should be exactly 2 for two-run double");
    }

    [Test]
    public void RBI_WalkNotBasesLoaded_IsZero() {
        // Arrange: Runner on 1st only, walk (no run scores)
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 1,
            half: InningHalf.Top,
            outs: 0,
            onFirst: true,
            onSecond: false,
            onThird: false,
            awayScore: 0,
            homeScore: 0,
            awayBattingOrderIndex: 7,
            homeBattingOrderIndex: 0,
            offense: Team.Away,
            defense: Team.Home
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 0,
            NewBases: new BaseState(OnFirst: true, OnSecond: true, OnThird: false),
            Type: PaType.BB
        );

        // Act
        var result = _scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.AwayScore, Is.EqualTo(0), "No runs should score");
        var batterStats = _scorer.BoxScore.AwayBatters[7];
        Assert.That(batterStats.RBI, Is.EqualTo(0), "RBI should be 0 for walk without bases loaded");
    }

    [Test]
    public void RBI_SingleNoRunsScore_IsZero() {
        // Arrange: Bases empty, single
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
            awayBattingOrderIndex: 8,
            homeBattingOrderIndex: 0,
            offense: Team.Away,
            defense: Team.Home
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 0,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.Single
        );

        // Act
        var result = _scorer.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(result.AwayScore, Is.EqualTo(0), "No runs should score");
        var batterStats = _scorer.BoxScore.AwayBatters[8];
        Assert.That(batterStats.RBI, Is.EqualTo(0), "RBI should be 0 when no runs score");
    }
}
