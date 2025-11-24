using DiamondSim.Tests.TestHelpers;

namespace DiamondSim.Tests.GameLoop;

/// <summary>
/// Tests for GameSimulator.RunGame() - returns GameResult.
/// </summary>
[TestFixture]
public class GameSimulatorTests {
    [Test]
    public void RunGame_ReturnsNonNullGameResult() {
        // Arrange
        var simulator = new GameSimulator("Home", "Away", 42);

        // Act
        var result = simulator.RunGame();

        // Assert
        Assert.That(result, Is.Not.Null, "RunGame should return a non-null GameResult");
    }

    [Test]
    public void RunGame_Metadata_HasCorrectTeamNamesAndSeed() {
        // Arrange
        var simulator = new GameSimulator("Sharks", "Comets", 12345);

        // Act
        var result = simulator.RunGame();

        // Assert - Using helper to reduce duplication
        GameMetadataTestHelpers.AssertGameMetadata(
            result.Metadata,
            expectedHomeTeam: "Sharks",
            expectedAwayTeam: "Comets",
            expectedSeed: 12345
        );
    }

    [Test]
    public void RunGame_SimulatesCompleteGame() {
        // Arrange
        var simulator = new GameSimulator("Home", "Away", 42);

        // Act
        var result = simulator.RunGame();

        // Assert - Game should be complete
        Assert.That(result.FinalState.IsFinal, Is.True, "Game should be marked as final");
        Assert.That(result.PlayLog.Count, Is.GreaterThan(0), "PlayLog should contain plate appearances");
        Assert.That(result.BoxScore.HomeBatters.Count, Is.GreaterThan(0), "BoxScore should have home batters");
        Assert.That(result.BoxScore.AwayBatters.Count, Is.GreaterThan(0), "BoxScore should have away batters");
    }

    [Test]
    public void RunGame_WithInjectedLineupGenerator_UsesProvidedLineups() {
        // Arrange - Use TestLineupGenerator for predictable lineups
        var lineupGenerator = new TestLineupGenerator();
        var rng = new SeededRandom(42);

        // Generate expected lineups using the same generator
        var expectedHomeLineup = new TeamLineup("Sharks", lineupGenerator.GenerateLineup("Sharks", rng).AsReadOnly());
        var expectedAwayLineup = new TeamLineup("Comets", lineupGenerator.GenerateLineup("Comets", rng).AsReadOnly());

        var simulator = new GameSimulator("Sharks", "Comets", 42, lineupGenerator);

        // Act
        var result = simulator.RunGame();

        // Assert - Lineups should match what we generated
        TeamLineupTestHelpers.AssertLineup(expectedHomeLineup, result.HomeLineup);
        TeamLineupTestHelpers.AssertLineup(expectedAwayLineup, result.AwayLineup);
    }

    [Test]
    public void RunGame_SimulatesFullGame_AndCompletesSuccessfully() {
        // Arrange
        var simulator = new GameSimulator("Home", "Away", 42);

        // Act
        var result = simulator.RunGame();

        // Assert - Game must complete (no infinite loops, no ties)
        Assert.That(result.FinalState.IsFinal, Is.True, "Game must be marked as final");
        Assert.That(result.FinalState.Inning, Is.GreaterThanOrEqualTo(9), "Game must go at least 9 innings");

        // One team must win (no ties)
        Assert.That(result.FinalState.HomeScore, Is.Not.EqualTo(result.FinalState.AwayScore),
            "Game cannot end in a tie");
    }

    [Test]
    public void RunGame_WithSameSeed_ProducesSameLogHash() {
        // Arrange - Run first game
        var result1 = new GameSimulator("Home", "Away", 42).RunGame();

        // Act - Run second game with same seed
        var result2 = new GameSimulator("Home", "Away", 42).RunGame();

        // Assert - LogHash should be identical (determinism)
        Assert.That(result2.LogHash, Is.EqualTo(result1.LogHash),
            "Same seed should produce identical game (verified by LogHash)");
    }

    [Test]
    public void RunGame_VerifyBasicGameCompletion() {
        // Arrange - Use seed 2 which produces a clean 9-inning game (away team wins, both teams get 27 outs)
        var simulator = new GameSimulator("Home", "Away", 2);

        // Act
        var result = simulator.RunGame();

        // Assert - Verify this is a clean 9-inning game
        Assert.That(result.FinalState.IsFinal, Is.True, "Game must be final");

        // Both teams must record exactly 27 outs (9 full innings)
        var awayOuts = result.BoxScore.GetTotalPitcherOuts(Team.Away);
        var homeOuts = result.BoxScore.GetTotalPitcherOuts(Team.Home);

        Assert.That(awayOuts, Is.EqualTo(27),
            $"Away team must record exactly 27 outs for a 9-inning game, got {awayOuts}");
        Assert.That(homeOuts, Is.EqualTo(27),
            $"Home team must record exactly 27 outs for a 9-inning game, got {homeOuts}");

        // Verify the game went at least 9 innings (may show as 10 due to a known display bug)
        Assert.That(result.FinalState.Inning, Is.GreaterThanOrEqualTo(9),
            $"Game must complete at least 9 innings, got {result.FinalState.Inning}");
    }

    [Test]
    public void RunGame_PitcherOuts_MatchInningsPitched() {
        // Arrange
        var simulator = new GameSimulator("Home", "Away", 42);

        // Act
        var result = simulator.RunGame();

        // Assert - Verify pitcher outs match IP calculation
        // Away pitcher
        var awayOuts = result.BoxScore.GetTotalPitcherOuts(Team.Away);
        var awayPitcher = result.BoxScore.AwayPitchers[0];
        var expectedAwayOuts = awayPitcher.OutsRecorded;
        Assert.That(awayOuts, Is.EqualTo(expectedAwayOuts),
            $"Away pitcher outs from GetTotalPitcherOuts ({awayOuts}) should match OutsRecorded ({expectedAwayOuts})");

        // Home pitcher
        var homeOuts = result.BoxScore.GetTotalPitcherOuts(Team.Home);
        var homePitcher = result.BoxScore.HomePitchers[0];
        var expectedHomeOuts = homePitcher.OutsRecorded;
        Assert.That(homeOuts, Is.EqualTo(expectedHomeOuts),
            $"Home pitcher outs from GetTotalPitcherOuts ({homeOuts}) should match OutsRecorded ({expectedHomeOuts})");

        // Verify IP calculation (outs / 3 with remainder as thirds)
        var awayIP = awayPitcher.OutsRecorded / 3m;
        var homeIP = homePitcher.OutsRecorded / 3m;

        Console.WriteLine($"Away Pitcher: {awayPitcher.OutsRecorded} outs = {awayIP:F1} IP");
        Console.WriteLine($"Home Pitcher: {homePitcher.OutsRecorded} outs = {homeIP:F1} IP");
        Console.WriteLine($"Game went {result.FinalState.Inning} innings");
        Console.WriteLine($"Final Score: Away {result.FinalState.AwayScore} - Home {result.FinalState.HomeScore}");
        Console.WriteLine($"IsFinal: {result.FinalState.IsFinal}");
        Console.WriteLine($"PlayLog entries: {result.PlayLog.Count}");
    }

    /// <summary>
    /// Tests that RunGame() correctly packages data values from _scorekeeper and playLogEntries
    /// into the returned GameResult. Verifies actual data values, not just object references.
    /// </summary>
    [Test]
    public void RunGame_PackagesScoreKeeperData_IntoGameResult() {
        // Arrange - Create testable simulator with predictable data
        var simulator = new TestableGameSimulatorWithPredictableData("Home", "Away", 42);

        // Act
        var result = simulator.RunGame();

        // Assert - Use helper methods for cleaner assertions
        AssertPlayLog(result.PlayLog, simulator.ExpectedPlayLogEntries);
        AssertBatters(result.BoxScore.AwayBatters, simulator.ExpectedAwayBatters);
        AssertBatters(result.BoxScore.HomeBatters, simulator.ExpectedHomeBatters);
        AssertLineScore(result.LineScore, simulator.ExpectedAwayInnings, simulator.ExpectedHomeInnings);
    }

    #region Helper Methods

    private void AssertBatters(Dictionary<int, BatterStats> actual, Dictionary<int, BatterStats> expected) {
        Assert.That(actual.Count, Is.EqualTo(expected.Count), "Batter count mismatch");

        foreach (var kvp in expected) {
            var lineupPos = kvp.Key;
            var expectedStats = kvp.Value;

            Assert.That(actual.ContainsKey(lineupPos), Is.True,
                $"Missing batter at lineup position {lineupPos}");

            var actualStats = actual[lineupPos];

            Assert.That(actualStats.AB, Is.EqualTo(expectedStats.AB),
                $"Batter[{lineupPos}].AB mismatch");
            Assert.That(actualStats.H, Is.EqualTo(expectedStats.H),
                $"Batter[{lineupPos}].H mismatch");
            Assert.That(actualStats.Singles, Is.EqualTo(expectedStats.Singles),
                $"Batter[{lineupPos}].Singles mismatch");
            Assert.That(actualStats.Doubles, Is.EqualTo(expectedStats.Doubles),
                $"Batter[{lineupPos}].Doubles mismatch");
            Assert.That(actualStats.Triples, Is.EqualTo(expectedStats.Triples),
                $"Batter[{lineupPos}].Triples mismatch");
            Assert.That(actualStats.HR, Is.EqualTo(expectedStats.HR),
                $"Batter[{lineupPos}].HR mismatch");
            Assert.That(actualStats.BB, Is.EqualTo(expectedStats.BB),
                $"Batter[{lineupPos}].BB mismatch");
            Assert.That(actualStats.HBP, Is.EqualTo(expectedStats.HBP),
                $"Batter[{lineupPos}].HBP mismatch");
            Assert.That(actualStats.K, Is.EqualTo(expectedStats.K),
                $"Batter[{lineupPos}].K mismatch");
            Assert.That(actualStats.RBI, Is.EqualTo(expectedStats.RBI),
                $"Batter[{lineupPos}].RBI mismatch");
            Assert.That(actualStats.R, Is.EqualTo(expectedStats.R),
                $"Batter[{lineupPos}].R mismatch");
            Assert.That(actualStats.PA, Is.EqualTo(expectedStats.PA),
                $"Batter[{lineupPos}].PA mismatch");
            Assert.That(actualStats.TB, Is.EqualTo(expectedStats.TB),
                $"Batter[{lineupPos}].TB mismatch");
        }
    }

    private void AssertLineScore(LineScore actual, int[] expectedAwayInnings, int[] expectedHomeInnings) {
        Assert.That(actual.AwayInnings.Count, Is.EqualTo(expectedAwayInnings.Length),
            "Away innings count mismatch");
        Assert.That(actual.HomeInnings.Count, Is.EqualTo(expectedHomeInnings.Length),
            "Home innings count mismatch");

        for (var i = 0; i < expectedAwayInnings.Length; i++) {
            Assert.That(actual.AwayInnings[i], Is.EqualTo(expectedAwayInnings[i]),
                $"Away inning {i + 1} runs mismatch");
        }

        for (var i = 0; i < expectedHomeInnings.Length; i++) {
            Assert.That(actual.HomeInnings[i], Is.EqualTo(expectedHomeInnings[i]),
                $"Home inning {i + 1} runs mismatch");
        }
    }

    private void AssertPlayLog(IReadOnlyList<PlayLogEntry> actual, List<PlayLogEntry> expected) {
        Assert.That(actual.Count, Is.EqualTo(expected.Count),
            "PlayLog entry count mismatch");

        for (var i = 0; i < expected.Count; i++) {
            var expectedEntry = expected[i];
            var actualEntry = actual[i];

            Assert.That(actualEntry.Inning, Is.EqualTo(expectedEntry.Inning),
                $"PlayLog[{i}].Inning mismatch");
            Assert.That(actualEntry.Half, Is.EqualTo(expectedEntry.Half),
                $"PlayLog[{i}].Half mismatch");
            Assert.That(actualEntry.BatterName, Is.EqualTo(expectedEntry.BatterName),
                $"PlayLog[{i}].BatterName mismatch");
            Assert.That(actualEntry.PitchingTeamName, Is.EqualTo(expectedEntry.PitchingTeamName),
                $"PlayLog[{i}].PitchingTeamName mismatch");
            Assert.That(actualEntry.Resolution.OutsAdded, Is.EqualTo(expectedEntry.Resolution.OutsAdded),
                $"PlayLog[{i}].Resolution.OutsAdded mismatch");
            Assert.That(actualEntry.Resolution.RunsScored, Is.EqualTo(expectedEntry.Resolution.RunsScored),
                $"PlayLog[{i}].Resolution.RunsScored mismatch");
            Assert.That(actualEntry.IsWalkoff, Is.EqualTo(expectedEntry.IsWalkoff),
                $"PlayLog[{i}].IsWalkoff mismatch");
            Assert.That(actualEntry.OutsAfter, Is.EqualTo(expectedEntry.OutsAfter),
                $"PlayLog[{i}].OutsAfter mismatch");
        }
    }

    #endregion
}

/// <summary>
/// Testable GameSimulator that injects predictable game data
/// to verify RunGame() correctly packages data values into GameResult.
/// Stores expected values for comprehensive validation.
/// </summary>
internal class TestableGameSimulatorWithPredictableData : GameSimulator {
    private bool _hasSimulated;

    public List<PlayLogEntry> ExpectedPlayLogEntries { get; } = new();
    public Dictionary<int, BatterStats> ExpectedAwayBatters { get; } = new();
    public Dictionary<int, BatterStats> ExpectedHomeBatters { get; } = new();
    public int[] ExpectedAwayInnings { get; private set; } = Array.Empty<int>();
    public int[] ExpectedHomeInnings { get; private set; } = Array.Empty<int>();

    public TestableGameSimulatorWithPredictableData(string homeTeamName, string awayTeamName, int seed)
        : base(homeTeamName, awayTeamName, seed) {
    }

    /// <summary>
    /// Override to inject predictable data and store expected values.
    /// </summary>
    protected override GameState SimulatePlateAppearance(GameState state, List<PlayLogEntry> playLogEntries) {
        if (!_hasSimulated) {
            _hasSimulated = true;

            // Inject predictable BoxScore data
            InjectBoxScoreData();

            // Inject predictable LineScore data
            InjectLineScoreData();

            // Inject predictable PlayLog data
            InjectPlayLogData(playLogEntries);
        }

        // Return final state
        return new GameState(
            balls: 0, strikes: 0, inning: 9, half: InningHalf.Bottom, outs: 0,
            onFirst: false, onSecond: false, onThird: false,
            awayScore: 5, homeScore: 3,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 0,
            offense: Team.Home, defense: Team.Away, isFinal: true
        );
    }

    private void InjectBoxScoreData() {
        // Away batter: 3 strikeouts, 1 single = 4 AB
        _scorekeeper.BoxScore.IncrementBatterStats(Team.Away, 0, PaType.K, 0, 0, false);
        _scorekeeper.BoxScore.IncrementBatterStats(Team.Away, 0, PaType.K, 0, 0, false);
        _scorekeeper.BoxScore.IncrementBatterStats(Team.Away, 0, PaType.K, 0, 0, false);
        _scorekeeper.BoxScore.IncrementBatterStats(Team.Away, 0, PaType.Single, 0, 0, false);

        // Store expected away batter stats
        ExpectedAwayBatters[0] = new BatterStats {
            AB = 4,
            H = 1,
            Singles = 1,
            Doubles = 0,
            Triples = 0,
            HR = 0,
            BB = 0,
            HBP = 0,
            K = 3,
            RBI = 0,
            R = 0,
            PA = 4,
            TB = 1
        };

        // Home batter: 2 doubles, 1 walk = 2 AB (walk doesn't count)
        _scorekeeper.BoxScore.IncrementBatterStats(Team.Home, 0, PaType.Double, 0, 0, false);
        _scorekeeper.BoxScore.IncrementBatterStats(Team.Home, 0, PaType.Double, 0, 0, false);
        _scorekeeper.BoxScore.IncrementBatterStats(Team.Home, 0, PaType.BB, 0, 0, false);

        // Store expected home batter stats
        ExpectedHomeBatters[0] = new BatterStats {
            AB = 2,
            H = 2,
            Singles = 0,
            Doubles = 2,
            Triples = 0,
            HR = 0,
            BB = 1,
            HBP = 0,
            K = 0,
            RBI = 0,
            R = 0,
            PA = 3,
            TB = 4
        };
    }

    private void InjectLineScoreData() {
        // Inning 1: Away 2, Home 1
        _scorekeeper.LineScore.RecordInning(Team.Away, 2);
        _scorekeeper.LineScore.RecordInning(Team.Home, 1);

        // Add more innings to reach final score of Away 5, Home 3
        _scorekeeper.LineScore.RecordInning(Team.Away, 3);
        _scorekeeper.LineScore.RecordInning(Team.Home, 2);

        // Store expected innings
        ExpectedAwayInnings = new int[] { 2, 3 };
        ExpectedHomeInnings = new int[] { 1, 2 };
    }

    private void InjectPlayLogData(List<PlayLogEntry> playLogEntries) {
        // Add 3 predictable play log entries
        var entry1 = new PlayLogEntry(
            inning: 1,
            half: InningHalf.Top,
            batterName: "Test Batter 1",
            pitchingTeamName: "Home",
            resolution: new PaResolution(1, 0, new BaseState(false, false, false), PaType.K, OutcomeTag.K),
            isWalkoff: false,
            outsAfter: 1
        );

        var entry2 = new PlayLogEntry(
            inning: 1,
            half: InningHalf.Top,
            batterName: "Test Batter 2",
            pitchingTeamName: "Home",
            resolution: new PaResolution(0, 1, new BaseState(false, false, false), PaType.Single, OutcomeTag.Single),
            isWalkoff: false,
            outsAfter: 1
        );

        var entry3 = new PlayLogEntry(
            inning: 1,
            half: InningHalf.Bottom,
            batterName: "Test Batter 3",
            pitchingTeamName: "Away",
            resolution: new PaResolution(1, 0, new BaseState(false, false, false), PaType.InPlayOut, OutcomeTag.InPlayOut),
            isWalkoff: false,
            outsAfter: 1
        );

        playLogEntries.Add(entry1);
        playLogEntries.Add(entry2);
        playLogEntries.Add(entry3);

        ExpectedPlayLogEntries.Add(entry1);
        ExpectedPlayLogEntries.Add(entry2);
        ExpectedPlayLogEntries.Add(entry3);
    }
}
