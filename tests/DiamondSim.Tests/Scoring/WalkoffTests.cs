using DiamondSim.Tests.TestHelpers;

namespace DiamondSim.Tests.Scoring;

/// <summary>
/// Tests for walk-off game ending logic, including the critical distinction between
/// home runs (all runs count) and non-home runs (only minimum runs needed to win).
/// </summary>
[TestFixture]
[Category("Scoring")]
public class WalkoffTests {
    private InningScorekeeper _scorekeeper = null!;

    [SetUp]
    public void Setup() {
        _scorekeeper = new InningScorekeeper();
    }

    [Test]
    public void WalkoffSingle_TiedGame_ClampsToOne() {
        // Arrange: Bottom 9th, tied 3-3, runner on 3rd
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Bottom, outs: 1,
            onFirst: false, onSecond: false, onThird: true,
            awayScore: 3, homeScore: 3,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 5,
            offense: Team.Home, defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.Single
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        var snapshot = result.ToTestSnapshot();
        Assert.Multiple(() => {
            Assert.That(snapshot.HomeScore, Is.EqualTo(4), "Home should score exactly 1 run to win");
            Assert.That(snapshot.AwayScore, Is.EqualTo(3), "Away score unchanged");
            Assert.That(snapshot.IsFinal, Is.True, "Game should end");
            Assert.That(_scorekeeper.HomeLOB.Last(), Is.EqualTo(0), "Walk-off LOB always 0");
        });
    }

    [Test]
    public void WalkoffHomeRun_TrailingByTwo_AllRunsCount() {
        // Arrange: Bottom 9th, down 5-3, bases loaded
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Bottom, outs: 2,
            onFirst: true, onSecond: true, onThird: true,
            awayScore: 5, homeScore: 3,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 3,
            offense: Team.Home, defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 4,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
            Type: PaType.HomeRun
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        var snapshot = result.ToTestSnapshot();
        Assert.Multiple(() => {
            Assert.That(snapshot.HomeScore, Is.EqualTo(7), "All 4 runs count for walk-off HR");
            Assert.That(snapshot.AwayScore, Is.EqualTo(5), "Away score unchanged");
            Assert.That(snapshot.IsFinal, Is.True, "Game should end");
            Assert.That(_scorekeeper.HomeLOB.Last(), Is.EqualTo(0), "Walk-off LOB always 0");
        });
    }

    [Test]
    public void WalkoffGrandSlam_TiedGame_AllFourRuns() {
        // Arrange: Bottom 9th, tied 2-2, bases loaded
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Bottom, outs: 0,
            onFirst: true, onSecond: true, onThird: true,
            awayScore: 2, homeScore: 2,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 8,
            offense: Team.Home, defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 4,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
            Type: PaType.HomeRun
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        var snapshot = result.ToTestSnapshot();
        Assert.Multiple(() => {
            Assert.That(snapshot.HomeScore, Is.EqualTo(6), "All 4 runs count for walk-off grand slam");
            Assert.That(snapshot.AwayScore, Is.EqualTo(2), "Away score unchanged");
            Assert.That(snapshot.IsFinal, Is.True, "Game should end");
            Assert.That(_scorekeeper.HomeLOB.Last(), Is.EqualTo(0), "Walk-off LOB always 0");
        });
    }

    [Test]
    public void WalkoffSoloHomeRun_TiedGame_OneRun() {
        // Arrange: Bottom 9th, tied 1-1, bases empty
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Bottom, outs: 1,
            onFirst: false, onSecond: false, onThird: false,
            awayScore: 1, homeScore: 1,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 2,
            offense: Team.Home, defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
            Type: PaType.HomeRun
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        var snapshot = result.ToTestSnapshot();
        Assert.Multiple(() => {
            Assert.That(snapshot.HomeScore, Is.EqualTo(2), "Solo HR counts as 1 run");
            Assert.That(snapshot.AwayScore, Is.EqualTo(1), "Away score unchanged");
            Assert.That(snapshot.IsFinal, Is.True, "Game should end");
            Assert.That(_scorekeeper.HomeLOB.Last(), Is.EqualTo(0), "Walk-off LOB always 0");
        });
    }

    [Test]
    public void WalkoffDouble_TrailingByOne_ClampsToTwo() {
        // Arrange: Bottom 9th, down 4-3, runners on 2nd and 3rd
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Bottom, outs: 1,
            onFirst: false, onSecond: true, onThird: true,
            awayScore: 4, homeScore: 3,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 4,
            offense: Team.Home, defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 2,
            NewBases: new BaseState(OnFirst: false, OnSecond: true, OnThird: false),
            Type: PaType.Double
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        var snapshot = result.ToTestSnapshot();
        Assert.Multiple(() => {
            Assert.That(snapshot.HomeScore, Is.EqualTo(5), "Only 2 runs needed to win");
            Assert.That(snapshot.AwayScore, Is.EqualTo(4), "Away score unchanged");
            Assert.That(snapshot.IsFinal, Is.True, "Game should end");
            Assert.That(_scorekeeper.HomeLOB.Last(), Is.EqualTo(0), "Walk-off LOB always 0");
        });
    }

    [Test]
    public void WalkoffSingle_BasesLoaded_ClampsToOne() {
        // Arrange: Bottom 9th, tied 0-0, bases loaded
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Bottom, outs: 2,
            onFirst: true, onSecond: true, onThird: true,
            awayScore: 0, homeScore: 0,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 6,
            offense: Team.Home, defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: true, OnThird: true),
            Type: PaType.Single
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        var snapshot = result.ToTestSnapshot();
        Assert.Multiple(() => {
            Assert.That(snapshot.HomeScore, Is.EqualTo(1), "Only 1 run needed to win");
            Assert.That(snapshot.AwayScore, Is.EqualTo(0), "Away score unchanged");
            Assert.That(snapshot.IsFinal, Is.True, "Game should end");
            Assert.That(_scorekeeper.HomeLOB.Last(), Is.EqualTo(0), "Walk-off LOB always 0 (not 3)");
            Assert.That(snapshot.OnFirst, Is.False, "Bases cleared on walk-off");
            Assert.That(snapshot.OnSecond, Is.False, "Bases cleared on walk-off");
            Assert.That(snapshot.OnThird, Is.False, "Bases cleared on walk-off");
        });
    }

    [Test]
    public void TopNinth_NoWalkoff() {
        // Arrange: Top 9th, tied 2-2, runner on 3rd
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Top, outs: 1,
            onFirst: false, onSecond: false, onThird: true,
            awayScore: 2, homeScore: 2,
            awayBattingOrderIndex: 3, homeBattingOrderIndex: 0,
            offense: Team.Away, defense: Team.Home
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.Single
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        var snapshot = result.ToTestSnapshot();
        Assert.Multiple(() => {
            Assert.That(snapshot.AwayScore, Is.EqualTo(3), "Away scores in top 9th");
            Assert.That(snapshot.HomeScore, Is.EqualTo(2), "Home score unchanged");
            Assert.That(snapshot.IsFinal, Is.False, "Game continues - home gets bottom 9th");
            Assert.That(snapshot.OnFirst, Is.True, "Batter on first");
        });
    }

    [Test]
    public void ExtraInnings_WalkoffStillApplies() {
        // Arrange: Bottom 10th, tied 5-5, runner on 3rd
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 10, half: InningHalf.Bottom, outs: 0,
            onFirst: false, onSecond: false, onThird: true,
            awayScore: 5, homeScore: 5,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 1,
            offense: Team.Home, defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.Single
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        var snapshot = result.ToTestSnapshot();
        Assert.Multiple(() => {
            Assert.That(snapshot.HomeScore, Is.EqualTo(6), "Home wins in extras");
            Assert.That(snapshot.AwayScore, Is.EqualTo(5), "Away score unchanged");
            Assert.That(snapshot.IsFinal, Is.True, "Game ends on walk-off in extras");
            Assert.That(_scorekeeper.HomeLOB.Last(), Is.EqualTo(0), "Walk-off LOB always 0");
        });
    }

    [Test]
    public void ExtraInnings_WalkoffHomeRun_AllRuns() {
        // Arrange: Bottom 10th, down 6-5, runners on 1st and 3rd
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 10, half: InningHalf.Bottom, outs: 1,
            onFirst: true, onSecond: false, onThird: true,
            awayScore: 6, homeScore: 5,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 7,
            offense: Team.Home, defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 3,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
            Type: PaType.HomeRun
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        var snapshot = result.ToTestSnapshot();
        Assert.Multiple(() => {
            Assert.That(snapshot.HomeScore, Is.EqualTo(8), "All 3 runs count for walk-off HR in extras");
            Assert.That(snapshot.AwayScore, Is.EqualTo(6), "Away score unchanged");
            Assert.That(snapshot.IsFinal, Is.True, "Game ends");
            Assert.That(_scorekeeper.HomeLOB.Last(), Is.EqualTo(0), "Walk-off LOB always 0");
        });
    }

    [Test]
    public void BottomNinth_HomeAlreadyLeading_NoWalkoffNeeded() {
        // Arrange: Bottom 9th, home leading 5-2, runner on 1st
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Bottom, outs: 0,
            onFirst: true, onSecond: false, onThird: false,
            awayScore: 2, homeScore: 5,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 0,
            offense: Team.Home, defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.Single
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        var snapshot = result.ToTestSnapshot();
        Assert.Multiple(() => {
            Assert.That(snapshot.HomeScore, Is.EqualTo(6), "Run counts normally");
            Assert.That(snapshot.AwayScore, Is.EqualTo(2), "Away score unchanged");
            Assert.That(snapshot.IsFinal, Is.False, "Game continues - no walk-off when already leading");
            Assert.That(snapshot.OnFirst, Is.True, "Batter on first");
        });
    }

    [Test]
    public void WalkoffWalk_BasesLoaded_ClampsToOne() {
        // Arrange: Bottom 9th, tied 3-3, bases loaded
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Bottom, outs: 1,
            onFirst: true, onSecond: true, onThird: true,
            awayScore: 3, homeScore: 3,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 5,
            offense: Team.Home, defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: true, OnThird: true),
            Type: PaType.BB
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        var snapshot = result.ToTestSnapshot();
        Assert.Multiple(() => {
            Assert.That(snapshot.HomeScore, Is.EqualTo(4), "Only 1 run needed to win");
            Assert.That(snapshot.AwayScore, Is.EqualTo(3), "Away score unchanged");
            Assert.That(snapshot.IsFinal, Is.True, "Game ends on walk-off walk");
            Assert.That(_scorekeeper.HomeLOB.Last(), Is.EqualTo(0), "Walk-off LOB always 0");
        });
    }
    /// <summary>
    /// Test: Walk-off on reach-on-error credits no RBI and marks run as unearned.
    /// PRD Section 3.1: Walkoff_Roe_Tie_BasesLoaded_PlatesOne
    /// MLB Rule: Reach-on-error does not credit RBI; walk-off game ending when winning run scores
    /// </summary>
    [Test]
    public void Walkoff_Roe_Tie_BasesLoaded_PlatesOne() {
        // Arrange: Bottom 9th, tied 5-5, bases loaded, 1 out
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Bottom, outs: 1,
            onFirst: true, onSecond: true, onThird: true,
            awayScore: 5, homeScore: 5,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 4,
            offense: Team.Home, defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: true, OnThird: true),
            Type: PaType.ReachOnError,
            HadError: true,
            AdvanceOnError: new BaseState(OnFirst: false, OnSecond: false, OnThird: true)
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        var snapshot = result.ToTestSnapshot();
        Assert.Multiple(() => {
            Assert.That(snapshot.HomeScore, Is.EqualTo(6), "Only 1 run needed to win");
            Assert.That(snapshot.AwayScore, Is.EqualTo(5), "Away score unchanged");
            Assert.That(snapshot.IsFinal, Is.True, "Game ends on walk-off");
            Assert.That(_scorekeeper.HomeLOB.Last(), Is.EqualTo(0), "Walk-off LOB always 0");
            Assert.That(snapshot.OnFirst, Is.False, "Bases cleared on walk-off");
            Assert.That(snapshot.OnSecond, Is.False, "Bases cleared on walk-off");
            Assert.That(snapshot.OnThird, Is.False, "Bases cleared on walk-off");
            Assert.That(result.HomeUnearnedRuns, Is.GreaterThan(0), "Run is unearned (ROE)");
        });
    }

    /// <summary>
    /// Test: Walk-off on bases-loaded HBP credits exactly 1 RBI and marks run as earned.
    /// PRD Section 3.1: Walkoff_HBP_Tie_BasesLoaded_ForcedInRun
    /// MLB Rule: Bases-loaded HBP credits 1 RBI (forced run)
    /// </summary>
    [Test]
    public void Walkoff_HBP_Tie_BasesLoaded_ForcedInRun() {
        // Arrange: Bottom 9th, tied 3-3, bases loaded, 2 outs
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Bottom, outs: 2,
            onFirst: true, onSecond: true, onThird: true,
            awayScore: 3, homeScore: 3,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 7,
            offense: Team.Home, defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: true, OnThird: true),
            Type: PaType.HBP,
            HadError: false
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        var snapshot = result.ToTestSnapshot();
        Assert.Multiple(() => {
            Assert.That(snapshot.HomeScore, Is.EqualTo(4), "Only 1 run needed");
            Assert.That(snapshot.AwayScore, Is.EqualTo(3));
            Assert.That(snapshot.IsFinal, Is.True);
            Assert.That(_scorekeeper.HomeLOB.Last(), Is.EqualTo(0));
            Assert.That(snapshot.OnFirst, Is.False, "Bases cleared");
            Assert.That(snapshot.OnSecond, Is.False, "Bases cleared");
            Assert.That(snapshot.OnThird, Is.False, "Bases cleared");
            Assert.That(result.HomeEarnedRuns, Is.GreaterThan(0), "Run is earned (no error)");
        });
    }

    /// <summary>
    /// Test: Walk-off sacrifice fly credits RBI even if error enabled the score.
    /// PRD Section 3.1: Walkoff_SacFly_WithError_CreditsRbiButUnearned
    /// MLB Rule: Sacrifice fly credits RBI regardless of error; error-assisted advancement makes run unearned
    /// </summary>
    [Test]
    public void Walkoff_SacFly_WithError_CreditsRbiButUnearned() {
        // Arrange: Bottom 9th, tied 2-2, runner on 3rd, 1 out
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Bottom, outs: 1,
            onFirst: false, onSecond: false, onThird: true,
            awayScore: 2, homeScore: 2,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 3,
            offense: Team.Home, defense: Team.Away
        );

        // Sacrifice fly with error enabling score
        var resolution = new PaResolution(
            OutsAdded: 1,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
            Type: PaType.InPlayOut,
            Flags: new PaFlags(IsDoublePlay: false, IsSacFly: true),
            HadError: true,
            AdvanceOnError: new BaseState(OnFirst: false, OnSecond: false, OnThird: true)
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        var snapshot = result.ToTestSnapshot();
        Assert.Multiple(() => {
            Assert.That(snapshot.HomeScore, Is.EqualTo(3), "Walk-off on sac fly");
            Assert.That(snapshot.IsFinal, Is.True);
            Assert.That(_scorekeeper.HomeLOB.Last(), Is.EqualTo(0));
            Assert.That(result.HomeUnearnedRuns, Is.GreaterThan(0), "Run is unearned (error-assisted)");
        });
    }

    /// <summary>
    /// Test: Non-HR walk-off clamps to minimum runs needed, suppressing extra runners.
    /// PRD Section 3.1: Walkoff_Double_TrailingByOne_ClampsToTwo
    /// MLB Rule: Game ends when winning run scores on non-home run play; only minimum runs needed are credited
    /// </summary>
    [Test]
    public void Walkoff_Double_TrailingByOne_ClampsToTwo() {
        // Arrange: Bottom 9th, home down 1 (4-5), runners on 2nd and 3rd, 0 outs
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Bottom, outs: 0,
            onFirst: false, onSecond: true, onThird: true,
            awayScore: 5, homeScore: 4,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 2,
            offense: Team.Home, defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 2,  // Both runners would score
            NewBases: new BaseState(OnFirst: false, OnSecond: true, OnThird: false),
            Type: PaType.Double,
            HadError: false
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        var snapshot = result.ToTestSnapshot();
        Assert.Multiple(() => {
            Assert.That(snapshot.HomeScore, Is.EqualTo(6), "Exactly 2 runs to win");
            Assert.That(snapshot.AwayScore, Is.EqualTo(5));
            Assert.That(snapshot.IsFinal, Is.True);
            Assert.That(_scorekeeper.HomeLOB.Last(), Is.EqualTo(0), "Walk-off LOB always 0");
            Assert.That(snapshot.OnFirst, Is.False, "Bases cleared on walk-off");
            Assert.That(snapshot.OnSecond, Is.False, "Batter not on 2nd (walk-off suppresses)");
            Assert.That(result.HomeEarnedRuns, Is.EqualTo(2), "Both runs earned");
        });
    }

    /// <summary>
    /// Test: Walk-off grand slam exception - all runs count (dead ball rule).
    /// PRD Section 3.1: Walkoff_GrandSlam_TrailingByTwo_AllFourRunsCount
    /// MLB Rule: Home run is dead ball; all runners must touch all bases; all runs count even in walk-off situations
    /// </summary>
    [Test]
    public void Walkoff_GrandSlam_TrailingByTwo_AllFourRunsCount() {
        // Arrange: Bottom 9th, home down 2 (3-5), bases loaded, 1 out
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Bottom, outs: 1,
            onFirst: true, onSecond: true, onThird: true,
            awayScore: 5, homeScore: 3,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 6,
            offense: Team.Home, defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 4,  // Grand slam
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
            Type: PaType.HomeRun,
            HadError: false
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        var snapshot = result.ToTestSnapshot();
        Assert.Multiple(() => {
            Assert.That(snapshot.HomeScore, Is.EqualTo(7), "ALL 4 runs count (HR exception)");
            Assert.That(snapshot.AwayScore, Is.EqualTo(5));
            Assert.That(snapshot.IsFinal, Is.True);
            Assert.That(_scorekeeper.HomeLOB.Last(), Is.EqualTo(0));
            Assert.That(result.HomeEarnedRuns, Is.EqualTo(4), "All runs earned");
        });
    }
}
