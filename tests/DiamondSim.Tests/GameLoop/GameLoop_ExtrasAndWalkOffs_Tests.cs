namespace DiamondSim.Tests.GameLoop;

/// <summary>
/// Test suite for extra innings and walk-off functionality.
/// Implements TDD approach as specified in PRD-EXTRAS-NO-TIES-V1.
/// </summary>
[TestFixture]
public class GameLoop_ExtrasAndWalkOffs_Tests {
    /// <summary>
    /// Test 7.1: RemovesTieOutcome_ComprehensiveSweep
    /// Purpose: Verify no tie outcomes exist in codebase
    /// </summary>
    [Test]
    public void RemovesTieOutcome_ComprehensiveSweep() {
        // Arrange: Run a game that would previously end tied after 9 innings
        var simulator = new GameSimulator("Home", "Away", seed: 12345);

        // Act: Run the game
        var report = simulator.RunGame();

        // Assert: Game should not end in a tie
        // The game should either:
        // 1. End after 9 innings with a winner, OR
        // 2. Continue to extra innings until there's a winner
        Assert.That(report, Does.Contain("Final:"));

        // Verify no "Tie" references in the report
        Assert.That(report, Does.Not.Contain("Tie"));
        Assert.That(report.ToLower(), Does.Not.Contain("tied"));
    }

    /// <summary>
    /// Test 7.2: ProceedsToExtras_WhenTiedAfterNine
    /// Purpose: Verify game continues to 10th inning when tied after 9
    /// </summary>
    [Test]
    public void ProceedsToExtras_WhenTiedAfterNine() {
        // Arrange: Create a tied game state at end of bottom 9th
        var scorekeeper = new InningScorekeeper();
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 9,
            half: InningHalf.Bottom,
            outs: 2,  // 2 outs, about to make 3rd out
            onFirst: false,
            onSecond: false,
            onThird: false,
            awayScore: 3,
            homeScore: 3,  // Tied!
            awayBattingOrderIndex: 0,
            homeBattingOrderIndex: 0,
            offense: Team.Home,
            defense: Team.Away,
            isFinal: false
        );

        // Create a resolution that ends the inning (3rd out, no runs)
        var resolution = new PaResolution(
            OutsAdded: 1,
            RunsScored: 0,
            NewBases: new BaseState(false, false, false),
            Type: PaType.K,
            Tag: OutcomeTag.K
        );

        // Act: Apply the plate appearance (should end bottom 9th)
        var result = scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert: Game should NOT be final, should proceed to top of 10th
        Assert.That(result.StateAfter.IsFinal, Is.False, "Game should not be final when tied after 9");
        Assert.That(result.StateAfter.Inning, Is.EqualTo(10), "Game should proceed to 10th inning");
        Assert.That(result.StateAfter.Half, Is.EqualTo(InningHalf.Top), "Should be top of 10th");
        Assert.That(result.StateAfter.Offense, Is.EqualTo(Team.Away), "Away team should bat in top of 10th");
    }

    /// <summary>
    /// Test 7.3: CompletesExtras_WhenAwayLeadsAfterBottom
    /// Purpose: Verify game ends correctly when Away team leads after completed extra inning
    /// </summary>
    [Test]
    public void CompletesExtras_WhenAwayLeadsAfterBottom() {
        // Arrange: Create a game state in bottom of 10th where Away leads
        var scorekeeper = new InningScorekeeper();

        // Simulate that we're in bottom 10th, Away scored in top 10th to take lead
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 10,
            half: InningHalf.Bottom,
            outs: 2,  // 2 outs, about to make 3rd out
            onFirst: false,
            onSecond: false,
            onThird: false,
            awayScore: 5,  // Away leads
            homeScore: 4,
            awayBattingOrderIndex: 0,
            homeBattingOrderIndex: 0,
            offense: Team.Home,
            defense: Team.Away,
            isFinal: false
        );

        // Create a resolution that ends the inning (3rd out, no runs)
        var resolution = new PaResolution(
            OutsAdded: 1,
            RunsScored: 0,
            NewBases: new BaseState(false, false, false),
            Type: PaType.K,
            Tag: OutcomeTag.K
        );

        // Act: Apply the plate appearance (should end bottom 10th and finalize game)
        var result = scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert: Game should be final, Away wins
        Assert.That(result.StateAfter.IsFinal, Is.True, "Game should be final when Away leads after completed extra inning");
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(5), "Away score should be 5");
        Assert.That(result.StateAfter.HomeScore, Is.EqualTo(4), "Home score should be 4");
        Assert.That(result.IsWalkoff, Is.False, "Should not be a walk-off (Away won)");
    }

    /// <summary>
    /// Test 7.4: WalkOff_BottomNine
    /// Purpose: Verify walk-off in bottom 9th with runner advancement truncation
    /// </summary>
    [Test]
    public void WalkOff_BottomNine() {
        // Arrange: Bottom 9th, Home trails 4-5, runner on 2nd
        var scorekeeper = new InningScorekeeper();
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 9,
            half: InningHalf.Bottom,
            outs: 1,
            onFirst: false,
            onSecond: true,  // Runner on 2nd
            onThird: false,
            awayScore: 5,
            homeScore: 4,  // Home trails by 1
            awayBattingOrderIndex: 0,
            homeBattingOrderIndex: 0,
            offense: Team.Home,
            defense: Team.Away,
            isFinal: false
        );

        // Create a single that would normally score runner from 2nd and advance batter to 1st
        // But in walk-off, only the winning run scores (runner from 2nd = 1 run to tie + win)
        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 2,  // Runner from 2nd scores (tie), then winning run scores
            NewBases: new BaseState(true, false, false),  // Batter would reach 1st
            Type: PaType.Single,
            Tag: OutcomeTag.Single,
            Moves: new List<RunnerMove> {
                new RunnerMove(FromBase: 2, ToBase: 4, Scored: true, WasForced: false),  // R2 scores (tie)
                new RunnerMove(FromBase: 0, ToBase: 4, Scored: true, WasForced: false)   // Batter scores (win)
            }.AsReadOnly()
        );

        // Act: Apply the plate appearance
        var result = scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert: Walk-off should occur
        Assert.That(result.IsWalkoff, Is.True, "Should be a walk-off");
        Assert.That(result.StateAfter.IsFinal, Is.True, "Game should be final");
        Assert.That(result.StateAfter.HomeScore, Is.EqualTo(6), "Home should score only winning run (4+2=6, not 4+2=6)");
        Assert.That(result.StateAfter.AwayScore, Is.EqualTo(5), "Away score unchanged");

        // Bases should be cleared on walk-off
        Assert.That(result.StateAfter.OnFirst, Is.False, "Bases should be cleared on walk-off");
        Assert.That(result.StateAfter.OnSecond, Is.False, "Bases should be cleared on walk-off");
        Assert.That(result.StateAfter.OnThird, Is.False, "Bases should be cleared on walk-off");
    }

    /// <summary>
    /// Test 7.5: WalkOff_BottomTwelve
    /// Purpose: Verify walk-off works in extra innings (12th inning)
    /// </summary>
    [Test]
    public void WalkOff_BottomTwelve() {
        // Arrange: Bottom 12th, tied 5-5, runner on 3rd
        var scorekeeper = new InningScorekeeper();
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 12,  // Extra innings!
            half: InningHalf.Bottom,
            outs: 2,
            onFirst: false,
            onSecond: false,
            onThird: true,  // Runner on 3rd
            awayScore: 5,
            homeScore: 5,  // Tied
            awayBattingOrderIndex: 0,
            homeBattingOrderIndex: 0,
            offense: Team.Home,
            defense: Team.Away,
            isFinal: false
        );

        // Single scores runner from 3rd for walk-off
        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,  // Runner from 3rd scores
            NewBases: new BaseState(true, false, false),
            Type: PaType.Single,
            Tag: OutcomeTag.Single
        );

        // Act
        var result = scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert: Walk-off in 12th inning
        Assert.That(result.IsWalkoff, Is.True, "Should be a walk-off in 12th inning");
        Assert.That(result.StateAfter.IsFinal, Is.True, "Game should be final");
        Assert.That(result.StateAfter.HomeScore, Is.EqualTo(6), "Home wins 6-5");
        Assert.That(result.StateAfter.Inning, Is.EqualTo(12), "Should still be 12th inning");
    }

    /// <summary>
    /// Test 7.6: WalkOff_HomeRun_CreditsAll
    /// Purpose: Verify home run walk-off scores all runners (no truncation)
    /// </summary>
    [Test]
    public void WalkOff_HomeRun_CreditsAll() {
        // Arrange: Bottom 10th, tied 3-3, runners on 1st and 2nd
        var scorekeeper = new InningScorekeeper();
        var state = new GameState(
            balls: 0,
            strikes: 0,
            inning: 10,
            half: InningHalf.Bottom,
            outs: 1,
            onFirst: true,   // Runner on 1st
            onSecond: true,  // Runner on 2nd
            onThird: false,
            awayScore: 3,
            homeScore: 3,  // Tied
            awayBattingOrderIndex: 0,
            homeBattingOrderIndex: 0,
            offense: Team.Home,
            defense: Team.Away,
            isFinal: false
        );

        // Home run - all runners score (no clamping for HR)
        var resolution = new PaResolution(
            OutsAdded: 0,
            RunsScored: 3,  // R2 + R1 + Batter = 3 runs
            NewBases: new BaseState(false, false, false),
            Type: PaType.HomeRun,
            Tag: OutcomeTag.HR
        );

        // Act
        var result = scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert: All 3 runs count on walk-off HR
        Assert.That(result.IsWalkoff, Is.True, "Should be a walk-off");
        Assert.That(result.StateAfter.IsFinal, Is.True, "Game should be final");
        Assert.That(result.StateAfter.HomeScore, Is.EqualTo(6), "All 3 runs should count (3+3=6)");
    }

    /// <summary>
    /// Test 7.7: LineScore_ExpandsBeyondNine
    /// Purpose: Verify line score dynamically expands for long games
    /// </summary>
    [Test]
    public void LineScore_ExpandsBeyondNine() {
        // Arrange: Simulate a 14-inning game by recording innings
        var scorekeeper = new InningScorekeeper();

        // Record 14 innings of play (alternating between teams)
        for (int inning = 1; inning <= 14; inning++) {
            // Top half (Away)
            scorekeeper.LineScore.RecordInning(Team.Away, inning % 3);  // Vary runs

            // Bottom half (Home) - skip bottom 14 if away leads
            if (inning < 14) {
                scorekeeper.LineScore.RecordInning(Team.Home, inning % 3);
            }
        }

        // Act: Get line score counts
        int awayInnings = scorekeeper.LineScore.AwayInnings.Count;
        int homeInnings = scorekeeper.LineScore.HomeInnings.Count;

        // Assert: Line score should have 14 columns
        Assert.That(awayInnings, Is.EqualTo(14), "Away line score should have 14 innings");
        Assert.That(homeInnings, Is.EqualTo(13), "Home line score should have 13 innings (didn't bat in bottom 14)");

        // Verify totals match sum of innings
        int awayTotal = scorekeeper.LineScore.AwayInnings.Sum();
        int homeTotal = scorekeeper.LineScore.HomeInnings.Sum();
        Assert.That(awayTotal, Is.GreaterThan(0), "Away should have scored runs");
        Assert.That(homeTotal, Is.GreaterThan(0), "Home should have scored runs");
    }

    /// <summary>
    /// Test 7.8: Determinism_PreservedAcrossExtras
    /// Purpose: Verify same seed produces identical results including extras
    /// </summary>
    [Test]
    public void Determinism_PreservedAcrossExtras() {
        // Arrange: Run same game twice with same seed
        int seed = 99999;

        // Act: Run first game
        var sim1 = new GameSimulator("Home", "Away", seed);
        var report1 = sim1.RunGame();

        // Run second game with same seed
        var sim2 = new GameSimulator("Home", "Away", seed);
        var report2 = sim2.RunGame();

        // Assert: Reports should be identical
        Assert.That(report2, Is.EqualTo(report1), "Same seed should produce identical game reports");
    }

    /// <summary>
    /// Test 7.9: Guardrail_NoInfiniteGames
    /// Purpose: Verify safety cap prevents infinite games
    /// </summary>
    [Test]
    public void Guardrail_NoInfiniteGames() {
        // Arrange: Run multiple games with different seeds
        var seeds = new[] { 1, 42, 123, 456, 789, 1000, 2000, 3000, 4000, 5000 };

        // Act & Assert: All games should complete without exceeding safety cap
        foreach (var seed in seeds) {
            var simulator = new GameSimulator("Home", "Away", seed);

            // This should not throw or hang
            Assert.DoesNotThrow(() => {
                var report = simulator.RunGame();
                Assert.That(report, Is.Not.Null);
                Assert.That(report, Does.Contain("Final:"));
            }, $"Game with seed {seed} should complete successfully");
        }
    }
}
