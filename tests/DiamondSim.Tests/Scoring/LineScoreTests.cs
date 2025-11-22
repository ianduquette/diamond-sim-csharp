using DiamondSim.Tests.TestHelpers;

namespace DiamondSim.Tests.Scoring;

/// <summary>
/// Tests for LineScore tracking and LOB (left on base) functionality in Phase 3.
/// </summary>
[TestFixture]
[Category("Scoring")]
public class LineScoreTests {
    [Test]
    public void LineScore_TracksRunsPerInning() {
        // Arrange
        var scorekeeper = new InningScorekeeper();
        var state = CreateInitialState();

        // Score 2 runs
        var resolution1 = new PaResolution(
            OutsAdded: 0,
            RunsScored: 2,
            NewBases: new BaseState(false, false, false),
            Type: PaType.HomeRun,
            Tag: OutcomeTag.HR
        );
        var result1 = scorekeeper.ApplyPlateAppearance(state, resolution1);
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
            var result = scorekeeper.ApplyPlateAppearance(state, outResolution);
            state = result.StateAfter;
        }

        // Assert
        Assert.That(scorekeeper.LineScore.GetInningRuns(Team.Away, 1), Is.EqualTo(2));
        Assert.That(scorekeeper.LineScore.AwayTotal, Is.EqualTo(2));
    }

    [Test]
    public void LineScore_TotalsMatchGameState() {
        // Arrange
        var scorekeeper = new InningScorekeeper();
        var state = CreateInitialState();

        // Top 1st: Away scores 3
        state = ScoreRunsAndEndHalf(scorekeeper, state, 3, 3);

        // Bottom 1st: Home scores 1
        state = ScoreRunsAndEndHalf(scorekeeper, state, 1, 3);

        // Top 2nd: Away scores 0
        state = ScoreRunsAndEndHalf(scorekeeper, state, 0, 3);

        // Act - Bottom 2nd: Home scores 2
        state = ScoreRunsAndEndHalf(scorekeeper, state, 2, 3);

        // Assert
        Assert.That(scorekeeper.LineScore.AwayTotal, Is.EqualTo(state.AwayScore));
        Assert.That(scorekeeper.LineScore.HomeTotal, Is.EqualTo(state.HomeScore));
        Assert.That(scorekeeper.LineScore.Validate(state.AwayScore, state.HomeScore), Is.True);
    }

    [Test]
    public void LOB_TrackedAtMomentOf3rdOut() {
        // Arrange
        var scorekeeper = new InningScorekeeper();
        var state = CreateInitialState();

        // Load bases
        var loadBases = new PaResolution(
            OutsAdded: 0,
            RunsScored: 0,
            NewBases: new BaseState(true, true, true),
            Type: PaType.BB,
            Tag: OutcomeTag.BB
        );
        var result = scorekeeper.ApplyPlateAppearance(state, loadBases);
        state = result.StateAfter;

        // Act - 3rd out with bases loaded - using BasesAtThirdOut snapshot
        for (int i = 0; i < 3; i++) {
            var outResolution = new PaResolution(
                OutsAdded: 1,
                RunsScored: 0,
                NewBases: new BaseState(false, false, false), // Bases cleared after play
                Type: PaType.InPlayOut,
                Tag: OutcomeTag.InPlayOut,
                BasesAtThirdOut: new BaseState(true, true, true) // Bases loaded at instant of 3rd out
            );
            var outResult = scorekeeper.ApplyPlateAppearance(state, outResolution);
            state = outResult.StateAfter;
        }

        // Assert
        Assert.That(scorekeeper.AwayLOB[0], Is.EqualTo(3));
        Assert.That(scorekeeper.AwayTotalLOB, Is.EqualTo(3));
    }

    /// <summary>
    /// LOB (Left On Base) Tests - Testing BasesAtThirdOut snapshot functionality
    /// These tests verify that LOB is correctly computed from the base state at the instant
    /// the third out occurs, rather than the post-play base state.
    /// </summary>

    [Test]
    public void LOB_TriplePlay_BasesLoaded_EqualsThree() {
        // Arrange: Bases loaded, 0 outs, triple play
        var scorekeeper = new InningScorekeeper();
        var state = CreateGameState(
            inning: 5,
            onFirst: true,
            onSecond: true,
            onThird: true
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
        var result = scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(scorekeeper.AwayLOB[0], Is.EqualTo(3));
        Assert.That(result.StateAfter.Half, Is.EqualTo(InningHalf.Bottom));
    }

    [Test]
    public void LOB_DoublePlay_RunnerOnThird_EqualsOne() {
        // Arrange: Runner on third, 1 out, double play ends half
        var scorekeeper = new InningScorekeeper();
        var state = CreateGameState(
            inning: 3,
            half: InningHalf.Bottom,
            outs: 1,
            onThird: true,
            awayScore: 2,
            homeScore: 1,
            homeBattingOrderIndex: 3
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
        var result = scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(scorekeeper.HomeLOB[0], Is.EqualTo(1));
        Assert.That(result.StateAfter.Half, Is.EqualTo(InningHalf.Top));
        Assert.That(result.StateAfter.Inning, Is.EqualTo(4));
    }

    [Test]
    public void LOB_StrikeoutWithRunners_EqualsTwo() {
        // Arrange: Runners on first and second, 2 outs, strikeout
        var scorekeeper = new InningScorekeeper();
        var state = CreateGameState(
            balls: 3,
            strikes: 2,
            inning: 7,
            outs: 2,
            onFirst: true,
            onSecond: true,
            awayScore: 3,
            homeScore: 2,
            awayBattingOrderIndex: 5,
            homeBattingOrderIndex: 2
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
        var result = scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(scorekeeper.AwayLOB[0], Is.EqualTo(2));
        Assert.That(result.StateAfter.Half, Is.EqualTo(InningHalf.Bottom));
    }

    [Test]
    public void LOB_RunnerScoresBeforeThirdOut_NotCountedInLOB() {
        // Arrange: R1/R2, 2 outs, R2 scores before third out on trailing runner
        var scorekeeper = new InningScorekeeper();
        var state = CreateGameState(
            inning: 8,
            outs: 2,
            onFirst: true,
            onSecond: true,
            awayScore: 2,
            homeScore: 3,
            awayBattingOrderIndex: 1
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
        var result = scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(scorekeeper.AwayLOB[0], Is.EqualTo(1));  // Only R1 stranded
        Assert.That(result.StateAfter.Half, Is.EqualTo(InningHalf.Bottom));
    }

    [Test]
    public void LOB_EmptyBasesAtThirdOut_EqualsZero() {
        // Arrange: No runners, 2 outs, strikeout
        var scorekeeper = new InningScorekeeper();
        var state = CreateGameState(
            outs: 2,
            awayBattingOrderIndex: 2
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
        var result = scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(scorekeeper.AwayLOB[0], Is.EqualTo(0));
        Assert.That(result.StateAfter.Half, Is.EqualTo(InningHalf.Bottom));
    }

    [Test]
    public void LOB_WalkoffNonHomeRun_AlwaysZero() {
        // Arrange: Bottom 9th, tied, R1/R3, 2 outs, single wins game
        var scorekeeper = new InningScorekeeper();
        var state = CreateGameState(
            inning: 9,
            half: InningHalf.Bottom,
            outs: 2,
            onFirst: true,
            onThird: true,
            awayScore: 3,
            homeScore: 3,
            homeBattingOrderIndex: 5
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
        var result = scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(scorekeeper.HomeLOB[0], Is.EqualTo(0));  // Walk-off: LOB always 0
        Assert.That(result.StateAfter.IsFinal, Is.True);
        Assert.That(result.StateAfter.HomeScore, Is.EqualTo(4));
    }

    [Test]
    public void LOB_WalkoffHomeRun_AlwaysZero() {
        // Arrange: Bottom 9th, down by 2, bases loaded, grand slam
        var scorekeeper = new InningScorekeeper();
        var state = CreateGameState(
            inning: 9,
            half: InningHalf.Bottom,
            outs: 1,
            onFirst: true,
            onSecond: true,
            onThird: true,
            awayScore: 5,
            homeScore: 3,
            homeBattingOrderIndex: 8
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
        var result = scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        Assert.That(scorekeeper.HomeLOB[0], Is.EqualTo(0));  // Walk-off: LOB always 0
        Assert.That(result.StateAfter.IsFinal, Is.True);
        Assert.That(result.StateAfter.HomeScore, Is.EqualTo(7));
    }

    [Test]
    public void LOB_BackwardCompatibility_NoSnapshot_UsesNewBases() {
        // Arrange: R1/R2, 2 outs, strikeout (producer not updated yet)
        var scorekeeper = new InningScorekeeper();
        var state = CreateGameState(
            inning: 4,
            outs: 2,
            onFirst: true,
            onSecond: true,
            awayScore: 1,
            awayBattingOrderIndex: 4
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
        var result = scorekeeper.ApplyPlateAppearance(state, resolution);

        // Assert
        // Should fall back to NewBases for LOB computation
        Assert.That(scorekeeper.AwayLOB[0], Is.EqualTo(2));
        Assert.That(result.StateAfter.Half, Is.EqualTo(InningHalf.Bottom));
    }

    [Test]
    public void Walkoff_RecordsPartialInningRuns_NotX() {
        // Arrange
        var scorekeeper = new InningScorekeeper();

        // Simulate 8.5 innings (through top of 9th)
        GameStateTestHelper.RecordInningsThroughTopOf(scorekeeper);

        var state = CreateGameState(
            inning: 9,
            half: InningHalf.Bottom,
            outs: 1,
            onSecond: true,
            awayScore: 3,
            homeScore: 3
        );

        // Act - Walk-off single scores 1 run
        var walkoffHit = new PaResolution(
            OutsAdded: 0,
            RunsScored: 1,
            NewBases: new BaseState(true, false, false),
            Type: PaType.Single,
            Tag: OutcomeTag.Single
        );
        var result = scorekeeper.ApplyPlateAppearance(state, walkoffHit);
        state = result.StateAfter;

        // Assert
        Assert.That(state.IsFinal, Is.True);
        Assert.That(state.HomeScore, Is.EqualTo(4));

        Assert.That(scorekeeper.LineScore.GetInningRuns(Team.Home, 9), Is.EqualTo(1));
        Assert.That(scorekeeper.LineScore.GetInningDisplay(Team.Home, 9), Is.EqualTo("1"));

        Assert.That(scorekeeper.HomeLOB[^1], Is.EqualTo(0));
    }

    [Test]
    public void SkipBottom9th_RecordsX_WhenHomeLeading() {
        // Arrange
        var scorekeeper = new InningScorekeeper();

        // Simulate 8.5 innings (through top of 9th)
        GameStateTestHelper.RecordInningsThroughTopOf(scorekeeper);

        var state = CreateGameState(
            inning: 9,
            outs: 2,
            awayScore: 3,
            homeScore: 4
        );

        // Act - 3rd out ends top 9th
        var finalOut = new PaResolution(
            OutsAdded: 1,
            RunsScored: 0,
            NewBases: new BaseState(false, false, false),
            Type: PaType.InPlayOut,
            Tag: OutcomeTag.InPlayOut
        );
        var result = scorekeeper.ApplyPlateAppearance(state, finalOut);
        state = result.StateAfter;

        // Assert
        Assert.That(state.IsFinal, Is.True);
        Assert.That(state.Inning, Is.EqualTo(9));
        Assert.That(state.Half, Is.EqualTo(InningHalf.Top));

        Assert.That(scorekeeper.LineScore.GetInningRuns(Team.Home, 9), Is.EqualTo(-1));
        Assert.That(scorekeeper.LineScore.GetInningDisplay(Team.Home, 9), Is.EqualTo("X"));
    }

    [Test]
    public void LineScore_GetInningDisplay_ReturnsCorrectValues() {
        // Arrange
        var scorekeeper = new InningScorekeeper();
        scorekeeper.LineScore.RecordInning(Team.Away, 3);
        scorekeeper.LineScore.RecordInning(Team.Home, 0);
        scorekeeper.LineScore.RecordSkippedInning(Team.Home);

        // Act & Assert
        Assert.That(scorekeeper.LineScore.GetInningDisplay(Team.Away, 1), Is.EqualTo("3"));
        Assert.That(scorekeeper.LineScore.GetInningDisplay(Team.Home, 1), Is.EqualTo("0"));
        Assert.That(scorekeeper.LineScore.GetInningDisplay(Team.Home, 2), Is.EqualTo("X"));
        Assert.That(scorekeeper.LineScore.GetInningDisplay(Team.Away, 3), Is.EqualTo("-")); // Not played yet
    }

    /// <summary>
    /// Bottom 9th is skipped when home leads after top 9: line score shows 'X'
    /// and both teams expose exactly 9 inning columns.
    /// </summary>
    [Test]
    public void LineScore_SkippedBottom9_DisplaysX_AndHasNineColumns() {
        // Arrange
        var scorekeeper = new InningScorekeeper();

        // Prefill through completed 8 full innings so column indices align 1..9
        _ = PrefillThroughInning8Quietly(scorekeeper);

        // Start top 9th with home already leading
        var state = CreateGameState(
            inning: 9,
            awayScore: 2,
            homeScore: 3
        );

        // Act - End the top 9th quietly (0 runs, 3 outs)
        var resultState = ScoreRunsAndEndHalf(scorekeeper, state, runs: 0, outs: 3);

        // Assert
        Assert.That(resultState.IsFinal, Is.True, "Game should end since home already led after top 9.");
        Assert.That(scorekeeper.LineScore.HomeInnings.Count, Is.EqualTo(9), "Home should have 9 inning entries.");
        Assert.That(scorekeeper.LineScore.AwayInnings.Count, Is.EqualTo(9), "Away should have 9 inning entries.");
        Assert.That(scorekeeper.LineScore.GetInningDisplay(Team.Home, 9), Is.EqualTo("X"), "Home 9th should display 'X'.");

        Assert.That(scorekeeper.LineScore.GetInningDisplay(Team.Away, 9), Is.EqualTo("0"));
    }

    /// <summary>
    /// The public display API should always expose 9 columns (1..9) per team,
    /// even if some innings have not been played yet (they render as '-').
    /// </summary>
    [Test]
    public void LineScore_Display_HasExactlyNineColumns_ForEachTeam() {
        // Arrange
        var scorekeeper = new InningScorekeeper();
        var s = CreateGameState();

        // Act - Start from a fresh scoreboard: only top 1st is completed quietly
        _ = ScoreRunsAndEndHalf(scorekeeper, s, runs: 0, outs: 3);

        // Assert - Ask for displays across 1..9 for both teams; should never throw and be short strings
        for (int i = 1; i <= 9; i++) {
            var away = scorekeeper.LineScore.GetInningDisplay(Team.Away, i);
            var home = scorekeeper.LineScore.GetInningDisplay(Team.Home, i);
            Assert.That(away, Is.Not.Null);
            Assert.That(home, Is.Not.Null);
            Assert.That(away.Length, Is.LessThanOrEqualTo(2));
            Assert.That(home.Length, Is.LessThanOrEqualTo(2));
        }
    }

    /// <summary>
    /// Helper method to score runs and end a half-inning.
    /// </summary>
    private static GameState ScoreRunsAndEndHalf(InningScorekeeper scorekeeper, GameState state, int runs, int outs) {
        // Score runs
        if (runs > 0) {
            var scoreResolution = new PaResolution(
                OutsAdded: 0,
                RunsScored: runs,
                NewBases: new BaseState(false, false, false),
                Type: PaType.HomeRun,
                Tag: OutcomeTag.HR
            );
            var result = scorekeeper.ApplyPlateAppearance(state, scoreResolution);
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
            var outResult = scorekeeper.ApplyPlateAppearance(state, outResolution);
            state = outResult.StateAfter;
        }

        return state;
    }

    /// <summary>
    /// Builds a clean scoreboard with innings 1..8 fully played quietly (0 runs each side),
    /// so line score indices are aligned before we assert on the 9th.
    /// </summary>
    private static GameState PrefillThroughInning8Quietly(InningScorekeeper scorekeeper) {
        var s = CreateGameState();

        for (int inning = 1; inning <= 8; inning++) {
            // Top half (away bats) → quiet half
            s = CreateGameState(inning: inning, awayScore: s.AwayScore, homeScore: s.HomeScore);
            s = ScoreRunsAndEndHalf(scorekeeper, s, runs: 0, outs: 3);

            // Bottom half (home bats) → quiet half
            s = CreateGameState(inning: inning, half: InningHalf.Bottom, awayScore: s.AwayScore, homeScore: s.HomeScore);
            s = ScoreRunsAndEndHalf(scorekeeper, s, runs: 0, outs: 3);
        }

        return s;
    }

    /// <summary>
    /// Sum of per-inning runs equals the team's scoreboard runs (away & home).
    /// </summary>
    [Test]
    public void Totals_LineScore_AndScoreboard_Agree() {
        // Arrange
        var scorekeeper = new InningScorekeeper();
        var s = CreateGameState();

        // T1: away 2 runs, then end half
        s = ScoreRunsAndEndHalf(scorekeeper, s, runs: 2, outs: 3);
        // B1: home 0
        s = CreateGameState(inning: 1, half: InningHalf.Bottom, awayScore: s.AwayScore, homeScore: s.HomeScore);
        s = ScoreRunsAndEndHalf(scorekeeper, s, runs: 0, outs: 3);

        // T2: away 1
        s = CreateGameState(inning: 2, awayScore: s.AwayScore, homeScore: s.HomeScore);
        s = ScoreRunsAndEndHalf(scorekeeper, s, runs: 1, outs: 3);
        // B2: home 3
        s = CreateGameState(inning: 2, half: InningHalf.Bottom, awayScore: s.AwayScore, homeScore: s.HomeScore);
        s = ScoreRunsAndEndHalf(scorekeeper, s, runs: 3, outs: 3);

        // T3/B3: zeros (just to have another frame)
        s = CreateGameState(inning: 3, awayScore: s.AwayScore, homeScore: s.HomeScore);
        s = ScoreRunsAndEndHalf(scorekeeper, s, runs: 0, outs: 3);
        s = CreateGameState(inning: 3, half: InningHalf.Bottom, awayScore: s.AwayScore, homeScore: s.HomeScore);
        s = ScoreRunsAndEndHalf(scorekeeper, s, runs: 0, outs: 3);

        // Act - sum line score by team
        var lineAway = SumLineScoreRuns(scorekeeper, Team.Away);
        var lineHome = SumLineScoreRuns(scorekeeper, Team.Home);

        // Assert
        Assert.That(lineAway, Is.EqualTo(s.AwayScore), "Away line score sum should match scoreboard runs.");
        Assert.That(lineHome, Is.EqualTo(s.HomeScore), "Home line score sum should match scoreboard runs.");
    }

    /// <summary>
    /// Unit test: Line score must track all innings including extras.
    /// The sum of all inning runs must equal the team's total runs.
    /// </summary>
    [Test]
    public void LineScore_TracksAllInnings_IncludingExtras() {
        // Arrange: Create a line score with runs in extra innings
        var lineScore = new LineScore();

        // Regular 9 innings
        for (int i = 1; i <= 9; i++) {
            lineScore.RecordInning(Team.Away, 0);
            lineScore.RecordInning(Team.Home, 0);
        }

        // Act - Extra innings with runs
        lineScore.RecordInning(Team.Away, 1); // Inning 10
        lineScore.RecordInning(Team.Home, 0);
        lineScore.RecordInning(Team.Away, 0); // Inning 11
        lineScore.RecordInning(Team.Home, 2); // Walk-off in 11th

        // Sum all innings
        int awaySum = lineScore.AwayInnings.Where(r => r >= 0).Sum();
        int homeSum = lineScore.HomeInnings.Where(r => r >= 0).Sum();

        // Assert
        Assert.That(awaySum, Is.EqualTo(lineScore.AwayTotal), "Away inning sum must equal AwayTotal");
        Assert.That(homeSum, Is.EqualTo(lineScore.HomeTotal), "Home inning sum must equal HomeTotal");
        Assert.That(awaySum, Is.EqualTo(1), "Away scored 1 run in extras");
        Assert.That(homeSum, Is.EqualTo(2), "Home scored 2 runs in extras");
    }

    /// <summary>
    /// Helper method to create initial game state - delegates to shared helper.
    /// </summary>
    private static GameState CreateInitialState() {
        return GameStateTestHelper.CreateInitialState();
    }

    /// <summary>
    /// Helper method to create a GameState - delegates to shared helper.
    /// </summary>
    private static GameState CreateGameState(
        int balls = 0,
        int strikes = 0,
        int inning = 1,
        InningHalf half = InningHalf.Top,
        int outs = 0,
        bool onFirst = false,
        bool onSecond = false,
        bool onThird = false,
        int awayScore = 0,
        int homeScore = 0,
        int awayBattingOrderIndex = 0,
        int homeBattingOrderIndex = 0,
        Team? offense = null,
        Team? defense = null,
        bool isFinal = false
    ) {
        return GameStateTestHelper.CreateGameState(
            balls, strikes, inning, half, outs,
            onFirst, onSecond, onThird,
            awayScore, homeScore,
            awayBattingOrderIndex, homeBattingOrderIndex,
            offense, defense, isFinal
        );
    }

    /// <summary>
    /// Helper method to sum all runs from a team's line score (excluding skipped innings marked as -1).
    /// </summary>
    private static int SumLineScoreRuns(InningScorekeeper scorekeeper, Team team) {
        var total = 0;
        for (int i = 1; i <= 9; i++) {
            var runs = scorekeeper.LineScore.GetInningRuns(team, i);
            if (runs > -1) total += runs; // -1 means 'X' (skipped inning)
        }
        return total;
    }

}
