using System.Security.Cryptography;
using System.Text;

namespace DiamondSim.Tests.GameLoop;

/// <summary>
/// Snapshot tests that verify deterministic game simulation across multiple seeds.
/// These tests ensure that the same seed always produces the same game outcome.
/// </summary>
[TestFixture]
public class SnapshotTests {
    private readonly string _snapshotDir = Path.Combine("GameLoop", "__snapshots__");

    [TestCase(42, "Sharks", "Comets")]
    [TestCase(8675309, "Dragons", "Tigers")]
    [TestCase(12345, "Eagles", "Hawks")]
    [TestCase(20251028, "Wolves", "Bears")]
    [TestCase(314159, "Lions", "Panthers")]
    public void GameSimulation_ProducesDeterministicOutput(int seed, string homeTeam, string awayTeam) {
        // Arrange
        var simulator = new GameSimulator(homeTeam, awayTeam, seed);

        // Act
        string report = simulator.RunGame();

        // Assert - Extract components for verification
        var lines = report.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Verify seed appears in output
        Assert.That(report, Does.Contain($"Seed: {seed}"));

        // Verify team names appear
        Assert.That(report, Does.Contain(homeTeam));
        Assert.That(report, Does.Contain(awayTeam));

        // Verify structure exists
        Assert.That(report, Does.Contain("DH: ON | Extras: OFF"));
        Assert.That(report, Does.Contain("Final:"));
        Assert.That(report, Does.Contain("LogHash:"));

        // Extract and verify LogHash format (64 hex chars)
        var logHashLine = lines.FirstOrDefault(l => l.Contains("LogHash:"));
        Assert.That(logHashLine, Is.Not.Null);
        var logHash = logHashLine!.Split(':')[1].Trim();
        Assert.That(logHash, Has.Length.EqualTo(64));
        Assert.That(logHash, Does.Match("^[0-9a-f]{64}$"));

        // Store snapshot for manual verification
        StoreSnapshot(seed, homeTeam, awayTeam, report);
    }

    [TestCase(42)]
    [TestCase(8675309)]
    [TestCase(12345)]
    [TestCase(20251028)]
    [TestCase(314159)]
    public void GameSimulation_SameSeedProducesSameLogHash(int seed) {
        // Arrange & Act - Run same game twice
        var simulator1 = new GameSimulator("Home", "Away", seed);
        var report1 = simulator1.RunGame();
        var logHash1 = ExtractLogHash(report1);

        var simulator2 = new GameSimulator("Home", "Away", seed);
        var report2 = simulator2.RunGame();
        var logHash2 = ExtractLogHash(report2);

        // Assert - LogHash should be identical
        Assert.That(logHash2, Is.EqualTo(logHash1));
    }

    [TestCase(42)]
    [TestCase(8675309)]
    public void GameSimulation_DifferentSeedsProduceDifferentLogHash(int seed1) {
        // Arrange & Act
        int seed2 = seed1 + 1;
        var simulator1 = new GameSimulator("Home", "Away", seed1);
        var report1 = simulator1.RunGame();
        var logHash1 = ExtractLogHash(report1);

        var simulator2 = new GameSimulator("Home", "Away", seed2);
        var report2 = simulator2.RunGame();
        var logHash2 = ExtractLogHash(report2);

        // Assert - LogHash should be different
        Assert.That(logHash2, Is.Not.EqualTo(logHash1));
    }

    [Test]
    public void GameSimulation_ProducesValidLineScore() {
        // Arrange
        var simulator = new GameSimulator("Home", "Away", 42);

        // Act
        string report = simulator.RunGame();

        // Assert - Verify line score structure
        var lines = report.Split('\n');

        // Find line score section (should have header with inning numbers and R H E)
        // Look for a line that has the inning numbers AND the R H E columns
        var lineScoreHeader = lines.FirstOrDefault(l =>
            l.Contains("|") &&
            l.Contains("1") &&
            l.Contains("2") &&
            l.Contains("3") &&
            l.Contains("R") &&
            l.Contains("H") &&
            l.Contains("E"));
        Assert.That(lineScoreHeader, Is.Not.Null, "Line score header not found");

        // Should have 9 inning columns
        Assert.That(lineScoreHeader, Does.Contain("9"), "Line score should show inning 9");

        // Should have R H E columns
        Assert.That(lineScoreHeader, Does.Contain("R"));
        Assert.That(lineScoreHeader, Does.Contain("H"));
        Assert.That(lineScoreHeader, Does.Contain("E"));
    }

    [Test]
    public void GameSimulation_ProducesValidBoxScore() {
        // Arrange
        var simulator = new GameSimulator("Sharks", "Comets", 42);

        // Act
        string report = simulator.RunGame();

        // Assert - Verify box score structure
        Assert.That(report, Does.Contain("BATTING"));
        Assert.That(report, Does.Contain("PITCHING"));

        // Should have batting stats columns
        Assert.That(report, Does.Contain("AB"));
        Assert.That(report, Does.Contain("RBI"));
        Assert.That(report, Does.Contain("BB"));

        // Should have pitching stats
        Assert.That(report, Does.Contain("IP"));
        Assert.That(report, Does.Contain("BF"));
        Assert.That(report, Does.Contain("ER"));

        // Should have pitcher names
        Assert.That(report, Does.Contain("Sharks P"));
        Assert.That(report, Does.Contain("Comets P"));
    }

    [Test]
    public void GameSimulation_ProducesPlayByPlayLog() {
        // Arrange
        var simulator = new GameSimulator("Home", "Away", 42);

        // Act
        string report = simulator.RunGame();

        // Assert - Verify play-by-play structure
        var lines = report.Split('\n');

        // Should have plays marked with [Top X] or [Bot X]
        var playLines = lines.Where(l => l.Contains("[Top") || l.Contains("[Bot")).ToList();
        Assert.That(playLines, Is.Not.Empty);

        // Each play should have "vs" and "P" (pitcher)
        foreach (var play in playLines.Take(5)) { // Check first 5 plays
            Assert.That(play, Does.Contain("vs"));
            Assert.That(play, Does.Contain("P"));
        }
    }

    private string ExtractLogHash(string report) {
        var lines = report.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var logHashLine = lines.FirstOrDefault(l => l.Contains("LogHash:"));
        if (logHashLine == null) {
            throw new InvalidOperationException("LogHash not found in report");
        }
        return logHashLine.Split(':')[1].Trim();
    }

    private void StoreSnapshot(int seed, string homeTeam, string awayTeam, string report) {
        // Create snapshots directory if it doesn't exist
        var snapshotPath = Path.Combine(Directory.GetCurrentDirectory(), _snapshotDir);
        Directory.CreateDirectory(snapshotPath);

        // Store snapshot file
        var filename = $"Report_seed_{seed}_{homeTeam}_vs_{awayTeam}.txt";
        var filepath = Path.Combine(snapshotPath, filename);
        File.WriteAllText(filepath, report);
    }
}
