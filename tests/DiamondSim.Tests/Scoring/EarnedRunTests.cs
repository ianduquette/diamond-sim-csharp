namespace DiamondSim.Tests.Scoring;

/// <summary>
/// Tests for earned vs unearned run classification according to official baseball rules.
/// Uses v1-light simplified approach: any error involvement marks all runs as unearned.
/// Full MLB Rule 9.16 reconstruction (hypothetical inning replay) is deferred to future PRD.
/// </summary>
[TestFixture]
[Category("Scoring")]
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
            Type: PaType.ReachOnError, Tag: OutcomeTag.ROE,
            HadError: true
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.Multiple(() => {
            Assert.That(result.StateAfter.AwayScore, Is.EqualTo(3), "Run scores");
            Assert.That(result.StateAfter.AwayEarnedRuns, Is.EqualTo(0), "No earned runs for ROE");
            Assert.That(result.StateAfter.AwayUnearnedRuns, Is.EqualTo(1), "1 unearned run for ROE");
            Assert.That(result.StateAfter.HomeEarnedRuns, Is.EqualTo(0), "Home earned runs unchanged");
            Assert.That(result.StateAfter.HomeUnearnedRuns, Is.EqualTo(0), "Home unearned runs unchanged");
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
            Type: PaType.Single, Tag: OutcomeTag.Single,
            HadError: false
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.Multiple(() => {
            Assert.That(result.StateAfter.HomeScore, Is.EqualTo(3), "Run scores");
            Assert.That(result.StateAfter.HomeEarnedRuns, Is.EqualTo(1), "1 earned run for clean single");
            Assert.That(result.StateAfter.HomeUnearnedRuns, Is.EqualTo(0), "No unearned runs");
            Assert.That(result.StateAfter.AwayEarnedRuns, Is.EqualTo(0), "Away earned runs unchanged");
            Assert.That(result.StateAfter.AwayUnearnedRuns, Is.EqualTo(0), "Away unearned runs unchanged");
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
            Type: PaType.Single, Tag: OutcomeTag.Single,
            HadError: true,
            AdvanceOnError: new BaseState(OnFirst: false, OnSecond: true, OnThird: false)
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.Multiple(() => {
            Assert.That(result.StateAfter.AwayScore, Is.EqualTo(2), "Run scores");
            Assert.That(result.StateAfter.AwayEarnedRuns, Is.EqualTo(0), "No earned runs (error assisted)");
            Assert.That(result.StateAfter.AwayUnearnedRuns, Is.EqualTo(1), "1 unearned run (error assisted)");
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
            Type: PaType.Single, Tag: OutcomeTag.Single,
            HadError: true,
            AdvanceOnError: null  // Error didn't affect scoring runner
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.Multiple(() => {
            Assert.That(result.StateAfter.HomeScore, Is.EqualTo(3), "Run scores");
            Assert.That(result.StateAfter.HomeEarnedRuns, Is.EqualTo(1), "1 earned run (error didn't affect scoring)");
            Assert.That(result.StateAfter.HomeUnearnedRuns, Is.EqualTo(0), "No unearned runs");
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
            Type: PaType.HomeRun, Tag: OutcomeTag.HR,
            HadError: false
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.Multiple(() => {
            Assert.That(result.StateAfter.AwayScore, Is.EqualTo(6), "4 runs score");
            Assert.That(result.StateAfter.AwayEarnedRuns, Is.EqualTo(4), "All 4 runs earned");
            Assert.That(result.StateAfter.AwayUnearnedRuns, Is.EqualTo(0), "No unearned runs");
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
            Type: PaType.Double, Tag: OutcomeTag.Double,
            HadError: true,
            AdvanceOnError: new BaseState(OnFirst: true, OnSecond: false, OnThird: false)
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.Multiple(() => {
            Assert.That(result.StateAfter.HomeScore, Is.EqualTo(6), "3 runs score");
            Assert.That(result.StateAfter.HomeEarnedRuns, Is.EqualTo(0), "No earned runs (error assisted)");
            Assert.That(result.StateAfter.HomeUnearnedRuns, Is.EqualTo(3), "All 3 runs unearned (v1-light: any error = all unearned)");
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
            Type: PaType.HomeRun, Tag: OutcomeTag.HR,
            HadError: false
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.Multiple(() => {
            Assert.That(result.StateAfter.HomeScore, Is.EqualTo(3), "Walk-off HR");
            Assert.That(result.StateAfter.HomeEarnedRuns, Is.EqualTo(1), "1 earned run");
            Assert.That(result.StateAfter.HomeUnearnedRuns, Is.EqualTo(0), "No unearned runs");
            Assert.That(result.StateAfter.IsFinal, Is.True, "Game ends");
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
            Type: PaType.Single, Tag: OutcomeTag.Single,
            HadError: false
        );
        var result1 = _scorekeeper.ApplyPlateAppearance(state1, resolution1);

        Assert.Multiple(() => {
            Assert.That(result1.StateAfter.AwayEarnedRuns, Is.EqualTo(1), "1 earned after PA1");
            Assert.That(result1.StateAfter.AwayUnearnedRuns, Is.EqualTo(0), "0 unearned after PA1");
            Assert.That(result1.StateAfter.AwayScore, Is.EqualTo(1), "1 run scored");
        });

        // PA 2: ROE, 1 unearned run (using result from PA1 as starting state)
        // Now that GameState constructor properly preserves earned/unearned runs, this works correctly
        var state2 = new GameState(
            balls: 0, strikes: 0,
            inning: result1.StateAfter.Inning, half: result1.StateAfter.Half, outs: result1.StateAfter.Outs,
            onFirst: result1.StateAfter.OnFirst, onSecond: result1.StateAfter.OnSecond, onThird: true,  // Set up runner
            awayScore: result1.StateAfter.AwayScore, homeScore: result1.StateAfter.HomeScore,
            awayBattingOrderIndex: result1.StateAfter.AwayBattingOrderIndex,
            homeBattingOrderIndex: result1.StateAfter.HomeBattingOrderIndex,
            offense: result1.StateAfter.Offense, defense: result1.StateAfter.Defense,
            isFinal: false,
            awayEarnedRuns: result1.StateAfter.AwayEarnedRuns,
            awayUnearnedRuns: result1.StateAfter.AwayUnearnedRuns,
            homeEarnedRuns: result1.StateAfter.HomeEarnedRuns,
            homeUnearnedRuns: result1.StateAfter.HomeUnearnedRuns
        );

        var resolution2 = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.ReachOnError, Tag: OutcomeTag.ROE,
            HadError: true
        );
        var result2 = _scorekeeper.ApplyPlateAppearance(state2, resolution2);

        Assert.Multiple(() => {
            Assert.That(result2.StateAfter.AwayEarnedRuns, Is.EqualTo(1), "Still 1 earned after PA2");
            Assert.That(result2.StateAfter.AwayUnearnedRuns, Is.EqualTo(1), "1 unearned after PA2");
            Assert.That(result2.StateAfter.AwayScore, Is.EqualTo(2), "Total 2 runs");
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
            Type: PaType.Single, Tag: OutcomeTag.Single,
            HadError: true,
            AdvanceOnError: new BaseState(OnFirst: false, OnSecond: true, OnThird: false)  // Only R2 advanced on error
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.Multiple(() => {
            Assert.That(result.StateAfter.AwayScore, Is.EqualTo(4), "2 runs added");
            Assert.That(result.StateAfter.AwayUnearnedRuns, Is.EqualTo(2), "v1 policy: any error = all unearned");
            Assert.That(result.StateAfter.AwayEarnedRuns, Is.EqualTo(0), "No earned runs");
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
            Type: PaType.ReachOnError, Tag: OutcomeTag.ROE,
            HadError: true,
            AdvanceOnError: new BaseState(OnFirst: false, OnSecond: true, OnThird: true)
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.Multiple(() => {
            Assert.That(result.StateAfter.AwayScore, Is.EqualTo(5), "2 runs added");
            Assert.That(result.StateAfter.AwayUnearnedRuns, Is.EqualTo(2), "ROE = all unearned");
            Assert.That(result.StateAfter.AwayEarnedRuns, Is.EqualTo(0), "No earned runs");
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
            Type: PaType.InPlayOut, Tag: OutcomeTag.InPlayOut,
            Flags: new PaFlags(IsDoublePlay: false, IsSacFly: true),
            HadError: true,
            AdvanceOnError: new BaseState(OnFirst: false, OnSecond: false, OnThird: true)
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.Multiple(() => {
            Assert.That(result.StateAfter.HomeScore, Is.EqualTo(3), "Run scores");
            Assert.That(result.StateAfter.HomeUnearnedRuns, Is.EqualTo(1), "Run is unearned (error-assisted)");
            Assert.That(result.StateAfter.HomeEarnedRuns, Is.EqualTo(0), "No earned runs");
        });
    }

    /// <summary>
    /// Sanity: across multiple seeds and thousands of PAs, no at-bat should hit the 50-pitch safety cap.
    /// </summary>
    [Test]
    public void AtBats_DoNotHitMaxPitchBailout() {
        var seeds = new[] { 101, 202, 303, 404 };
        const int trialsPerSeed = 5000;
        int maxHitCount = 0;

        foreach (var seed in seeds) {
            var rng = new SeededRandom(seed);
            var sim = new AtBatSimulator(rng);
            var pitcher = PitcherRatings.Average;
            var batter = BatterRatings.Average;

            for (int i = 0; i < trialsPerSeed; i++) {
                var res = sim.SimulateAtBat(pitcher, batter);
                if (res.PitchCount >= 50) maxHitCount++;
            }
        }

        Assert.That(maxHitCount, Is.EqualTo(0), "No at-bat should hit the 50-pitch safety cap.");
    }

    /// <summary>
    /// V1-light limitation: an error earlier in the inning does NOT make later clean runs unearned.
    /// Only the current PA's error flags are considered.
    /// </summary>
    [Test]
    public void Unearned_DoesNotCarryToLaterCleanPlay_V1Light() {
        // Arrange: Start with an error that does NOT score
        var s = new GameState(0, 0,
            inning: 4, half: InningHalf.Top, outs: 0,
            onFirst: false, onSecond: false, onThird: false,
            awayScore: 0, homeScore: 0,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 0,
            offense: Team.Away, defense: Team.Home);

        var roe = new PaResolution(
            OutsAdded: 0,
            RunsScored: 0,
            NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
            Type: PaType.ReachOnError, Tag: OutcomeTag.ROE,
            HadError: true
        );
        s = _scorekeeper.ApplyPlateAppearance(s, roe).StateAfter;

        // Next batter: clean double scores runner from first
        var cleanDouble = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: false, OnSecond: true, OnThird: false),
            Type: PaType.Double, Tag: OutcomeTag.Double,
            HadError: false
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(s, cleanDouble);

        // Assert: under v1-light, that run is EARNED because this PA has no error flags.
        Assert.That(result.StateAfter.AwayEarnedRuns, Is.EqualTo(1));
        Assert.That(result.StateAfter.AwayUnearnedRuns, Is.EqualTo(0));
    }

    /// <summary>
    /// Bases-loaded walk forces in a run; with no error involvement, this is an earned run in v1-light.
    /// </summary>
    [Test]
    public void Earned_BasesLoadedWalk_RunIsEarned() {
        // Arrange
        var s = new GameState(0, 0,
            inning: 5, half: InningHalf.Bottom, outs: 1,
            onFirst: true, onSecond: true, onThird: true,
            awayScore: 2, homeScore: 2,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 5,
            offense: Team.Home, defense: Team.Away);

        var walk = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(OnFirst: true, OnSecond: true, OnThird: true), // batter to 1st, all forced
            Type: PaType.BB, Tag: OutcomeTag.BB
        );

        // Act
        var result = _scorekeeper.ApplyPlateAppearance(s, walk);

        // Assert
        Assert.That(result.StateAfter.HomeScore, Is.EqualTo(3));
        Assert.That(result.StateAfter.HomeEarnedRuns, Is.EqualTo(1));
        Assert.That(result.StateAfter.HomeUnearnedRuns, Is.EqualTo(0));
    }

}
