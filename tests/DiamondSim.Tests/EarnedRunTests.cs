using NUnit.Framework;

namespace DiamondSim.Tests;

/// <summary>
/// Tests for earned vs unearned run classification according to official baseball rules.
/// Uses v1-light simplified approach: any error involvement marks all runs as unearned.
/// Full MLB Rule 9.16 reconstruction (hypothetical inning replay) is deferred to future PRD.
/// </summary>
[TestFixture]
public class EarnedRunTests {
    private InningScorekeeper _scorekeeper = null!;

    [SetUp]
    public void Setup() {
        _scorekeeper = new InningScorekeeper();
    }

    [Test]
    public void RoeScoresRunner_UnearnedRun() {
        // Arrange: Runner on 3rd, reach on error scores the run
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 4, half: InningHalf.Top, outs: 1,
            onFirst: false, onSecond: false, onThird: true,
            awayScore: 2, homeScore: 3,
            awayBattingOrderIndex: 5, homeBattingOrderIndex: 0,
            offense: Team.Away, defense: Team.Home
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.ReachOnError,
            HadError: true
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.Multiple(() => {
            Assert.That(result.AwayScore, Is.EqualTo(3), "Run scores");
            Assert.That(result.AwayEarnedRuns, Is.EqualTo(0), "No earned runs for ROE");
            Assert.That(result.AwayUnearnedRuns, Is.EqualTo(1), "1 unearned run for ROE");
            Assert.That(result.HomeEarnedRuns, Is.EqualTo(0), "Home earned runs unchanged");
            Assert.That(result.HomeUnearnedRuns, Is.EqualTo(0), "Home unearned runs unchanged");
        });
    }

    [Test]
    public void CleanSingle_EarnedRun() {
        // Arrange: Runner on 3rd, clean single scores the run
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 6, half: InningHalf.Bottom, outs: 2,
            onFirst: false, onSecond: false, onThird: true,
            awayScore: 4, homeScore: 2,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 7,
            offense: Team.Home, defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.Single,
            HadError: false
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.Multiple(() => {
            Assert.That(result.HomeScore, Is.EqualTo(3), "Run scores");
            Assert.That(result.HomeEarnedRuns, Is.EqualTo(1), "1 earned run for clean single");
            Assert.That(result.HomeUnearnedRuns, Is.EqualTo(0), "No unearned runs");
            Assert.That(result.AwayEarnedRuns, Is.EqualTo(0), "Away earned runs unchanged");
            Assert.That(result.AwayUnearnedRuns, Is.EqualTo(0), "Away unearned runs unchanged");
        });
    }

    [Test]
    public void AdvanceOnError_UnearnedRun() {
        // Arrange: Runner on 2nd, single with error allows runner to score
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 3, half: InningHalf.Top, outs: 0,
            onFirst: false, onSecond: true, onThird: false,
            awayScore: 1, homeScore: 2,
            awayBattingOrderIndex: 3, homeBattingOrderIndex: 0,
            offense: Team.Away, defense: Team.Home
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.Single,
            HadError: true,
            AdvanceOnError: new BaseState(OnFirst: false, OnSecond: true, OnThird: false)
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.Multiple(() => {
            Assert.That(result.AwayScore, Is.EqualTo(2), "Run scores");
            Assert.That(result.AwayEarnedRuns, Is.EqualTo(0), "No earned runs (error assisted)");
            Assert.That(result.AwayUnearnedRuns, Is.EqualTo(1), "1 unearned run (error assisted)");
        });
    }

    [Test]
    public void ErrorButNoAdvance_EarnedRun() {
        // Arrange: Runner on 3rd, single scores runner cleanly despite error elsewhere
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 5, half: InningHalf.Bottom, outs: 1,
            onFirst: false, onSecond: false, onThird: true,
            awayScore: 3, homeScore: 2,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 4,
            offense: Team.Home, defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.Single,
            HadError: true,
            AdvanceOnError: null  // Error didn't affect scoring runner
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.Multiple(() => {
            Assert.That(result.HomeScore, Is.EqualTo(3), "Run scores");
            Assert.That(result.HomeEarnedRuns, Is.EqualTo(1), "1 earned run (error didn't affect scoring)");
            Assert.That(result.HomeUnearnedRuns, Is.EqualTo(0), "No unearned runs");
        });
    }

    [Test]
    public void HomeRun_AllEarned() {
        // Arrange: Bases loaded, grand slam
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 7, half: InningHalf.Top, outs: 2,
            onFirst: true, onSecond: true, onThird: true,
            awayScore: 2, homeScore: 5,
            awayBattingOrderIndex: 8, homeBattingOrderIndex: 0,
            offense: Team.Away, defense: Team.Home
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 4,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
            Type: PaType.HomeRun,
            HadError: false
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.Multiple(() => {
            Assert.That(result.AwayScore, Is.EqualTo(6), "4 runs score");
            Assert.That(result.AwayEarnedRuns, Is.EqualTo(4), "All 4 runs earned");
            Assert.That(result.AwayUnearnedRuns, Is.EqualTo(0), "No unearned runs");
        });
    }

    [Test]
    public void MultipleRunsWithError_AllUnearned() {
        // Arrange: Bases loaded, double with error allows all to score
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 8, half: InningHalf.Bottom, outs: 0,
            onFirst: true, onSecond: true, onThird: true,
            awayScore: 6, homeScore: 3,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 2,
            offense: Team.Home, defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 3,
            NewBases: new BaseState(OnFirst: false, OnSecond: true, OnThird: false),
            Type: PaType.Double,
            HadError: true,
            AdvanceOnError: new BaseState(OnFirst: true, OnSecond: false, OnThird: false)
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.Multiple(() => {
            Assert.That(result.HomeScore, Is.EqualTo(6), "3 runs score");
            Assert.That(result.HomeEarnedRuns, Is.EqualTo(0), "No earned runs (error assisted)");
            Assert.That(result.HomeUnearnedRuns, Is.EqualTo(3), "All 3 runs unearned (v1-light: any error = all unearned)");
        });
    }

    [Test]
    public void WalkoffHomeRun_EarnedRuns() {
        // Arrange: Bottom 9th, tied, solo home run wins it
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Bottom, outs: 2,
            onFirst: false, onSecond: false, onThird: false,
            awayScore: 2, homeScore: 2,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 6,
            offense: Team.Home, defense: Team.Away
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
            Type: PaType.HomeRun,
            HadError: false
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.Multiple(() => {
            Assert.That(result.HomeScore, Is.EqualTo(3), "Walk-off HR");
            Assert.That(result.HomeEarnedRuns, Is.EqualTo(1), "1 earned run");
            Assert.That(result.HomeUnearnedRuns, Is.EqualTo(0), "No unearned runs");
            Assert.That(result.IsFinal, Is.True, "Game ends");
        });
    }

    [Test]
    public void AccumulatedEarnedRuns_MultipleInnings() {
        // Test that earned/unearned runs accumulate correctly across multiple PAs

        // PA 1: Clean single, 1 earned run
        var state1 = new GameState(
            balls: 0, strikes: 0,
            inning: 1, half: InningHalf.Top, outs: 0,
            onFirst: false, onSecond: false, onThird: true,
            awayScore: 0, homeScore: 0,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 0,
            offense: Team.Away, defense: Team.Home
        );

        var resolution1 = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.Single,
            HadError: false
        );
        var result1 = _scorekeeper.ApplyPlateAppearance(state1, resolution1);

        Assert.Multiple(() => {
            Assert.That(result1.AwayEarnedRuns, Is.EqualTo(1), "1 earned after PA1");
            Assert.That(result1.AwayUnearnedRuns, Is.EqualTo(0), "0 unearned after PA1");
            Assert.That(result1.AwayScore, Is.EqualTo(1), "1 run scored");
        });

        // PA 2: ROE, 1 unearned run (using result from PA1 as starting state)
        // Now that GameState constructor properly preserves earned/unearned runs, this works correctly
        var state2 = new GameState(
            balls: 0, strikes: 0,
            inning: result1.Inning, half: result1.Half, outs: result1.Outs,
            onFirst: result1.OnFirst, onSecond: result1.OnSecond, onThird: true,  // Set up runner
            awayScore: result1.AwayScore, homeScore: result1.HomeScore,
            awayBattingOrderIndex: result1.AwayBattingOrderIndex,
            homeBattingOrderIndex: result1.HomeBattingOrderIndex,
            offense: result1.Offense, defense: result1.Defense,
            isFinal: false,
            awayEarnedRuns: result1.AwayEarnedRuns,
            awayUnearnedRuns: result1.AwayUnearnedRuns,
            homeEarnedRuns: result1.HomeEarnedRuns,
            homeUnearnedRuns: result1.HomeUnearnedRuns
        );

        var resolution2 = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.ReachOnError,
            HadError: true
        );
        var result2 = _scorekeeper.ApplyPlateAppearance(state2, resolution2);

        Assert.Multiple(() => {
            Assert.That(result2.AwayEarnedRuns, Is.EqualTo(1), "Still 1 earned after PA2");
            Assert.That(result2.AwayUnearnedRuns, Is.EqualTo(1), "1 unearned after PA2");
            Assert.That(result2.AwayScore, Is.EqualTo(2), "Total 2 runs");
        });
    }
    /// <summary>
    /// Test: Document v1-light policy: if ANY runner advances on error, ALL runs are unearned.
    /// PRD Section 3.2: Earned_MultiRun_Single_ErrorOnlyEnablesLeadRunner_V1MarksAllUnearned
    /// MLB Rule: Earned run determination (simplified v1-light implementation)
    /// </summary>
    [Test]
    public void Earned_MultiRun_Single_ErrorOnlyEnablesLeadRunner_V1MarksAllUnearned() {
        // Arrange: Top 5th, runners on 1st and 2nd, 1 out
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 5, half: InningHalf.Top, outs: 1,
            onFirst: true, onSecond: true, onThird: false,
            awayScore: 2, homeScore: 1,
            awayBattingOrderIndex: 4, homeBattingOrderIndex: 0,
            offense: Team.Away, defense: Team.Home
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 2,  // Both runners score
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.Single,
            HadError: true,
            AdvanceOnError: new BaseState(OnFirst: false, OnSecond: true, OnThird: false)  // Only R2 advanced on error
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.Multiple(() => {
            Assert.That(result.AwayScore, Is.EqualTo(4), "2 runs added");
            Assert.That(result.AwayUnearnedRuns, Is.EqualTo(2), "v1 policy: any error = all unearned");
            Assert.That(result.AwayEarnedRuns, Is.EqualTo(0), "No earned runs");
        });
    }

    /// <summary>
    /// Test: Verify ROE with multiple runners always produces all unearned runs and no RBI.
    /// PRD Section 3.2: Unearned_ROE_MultipleRuns_AllUnearned_NoRBI
    /// MLB Rule: Reach-on-error does not credit RBI; error-caused runs are unearned
    /// </summary>
    [Test]
    public void Unearned_ROE_MultipleRuns_AllUnearned_NoRBI() {
        // Arrange: Top 7th, runners on 2nd and 3rd, 1 out
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 7, half: InningHalf.Top, outs: 1,
            onFirst: false, onSecond: true, onThird: true,
            awayScore: 3, homeScore: 4,
            awayBattingOrderIndex: 2, homeBattingOrderIndex: 0,
            offense: Team.Away, defense: Team.Home
        );

        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 2,  // Both runners score on error
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.ReachOnError,
            HadError: true,
            AdvanceOnError: new BaseState(OnFirst: false, OnSecond: true, OnThird: true)
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.Multiple(() => {
            Assert.That(result.AwayScore, Is.EqualTo(5), "2 runs added");
            Assert.That(result.AwayUnearnedRuns, Is.EqualTo(2), "ROE = all unearned");
            Assert.That(result.AwayEarnedRuns, Is.EqualTo(0), "No earned runs");
        });
    }

    /// <summary>
    /// Test: Verify sacrifice fly credits RBI even when error makes run unearned.
    /// PRD Section 3.2: Earned_SacFly_WithError_RunUnearnedButRbiCredited
    /// MLB Rule: Sacrifice fly credits RBI regardless of error involvement
    /// </summary>
    [Test]
    public void Earned_SacFly_WithError_RunUnearnedButRbiCredited() {
        // Arrange: Bottom 6th, runner on 3rd, 1 out
        var state = new GameState(
            balls: 0, strikes: 0,
            inning: 6, half: InningHalf.Bottom, outs: 1,
            onFirst: false, onSecond: false, onThird: true,
            awayScore: 3, homeScore: 2,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 5,
            offense: Team.Home, defense: Team.Away
        );

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
        Assert.Multiple(() => {
            Assert.That(result.HomeScore, Is.EqualTo(3), "Run scores");
            Assert.That(result.HomeUnearnedRuns, Is.EqualTo(1), "Run is unearned (error-assisted)");
            Assert.That(result.HomeEarnedRuns, Is.EqualTo(0), "No earned runs");
        });
    }
}
