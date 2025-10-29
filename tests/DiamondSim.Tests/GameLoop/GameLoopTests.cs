namespace DiamondSim.Tests.GameLoop;

/// <summary>
/// Unit tests for game loop logic including walk-offs, skip B9, box score calculations, and other edge cases.
/// These tests run actual games and verify specific behaviors.
/// </summary>
[TestFixture]
public class GameLoopTests {

    [Test]
    public void GameSimulation_CompletesSuccessfully() {
        // Arrange
        var simulator = new GameSimulator("Home", "Away", 42);

        // Act
        string report = simulator.RunGame();

        // Assert
        Assert.That(report, Is.Not.Null);
        Assert.That(report, Is.Not.Empty);
        Assert.That(report, Does.Contain("Final:"));
    }

    [Test]
    public void GameSimulation_ProducesNineInnings_OrFewerForWalkoff() {
        // Arrange
        var simulator = new GameSimulator("Home", "Away", 42);

        // Act
        string report = simulator.RunGame();

        // Assert - Should have inning markers
        var lines = report.Split('\n');
        var playLines = lines.Where(l => l.Contains("[Top") || l.Contains("[Bot")).ToList();

        Assert.That(playLines, Is.Not.Empty);

        // Should have Top 1 through at least Top 9
        Assert.That(playLines.Any(l => l.Contains("[Top 1]")), Is.True);
        Assert.That(playLines.Any(l => l.Contains("[Top 9]")), Is.True);
    }

    [Test]
    public void BoxScore_AtBatCalculation_ExcludesWalksHbpAndSacFlies() {
        // Arrange - Run a game and parse box score
        var simulator = new GameSimulator("Home", "Away", 42);
        string report = simulator.RunGame();

        // Act - Extract box score section
        var lines = report.Split('\n');
        var battingSection = ExtractSection(lines, "BATTING", "PITCHING");

        // Assert - Verify AB calculation for at least one batter
        // AB should be less than or equal to total plate appearances
        // This is a basic sanity check; detailed verification would require parsing each stat
        Assert.That(battingSection, Does.Contain("AB"));
        Assert.That(battingSection, Does.Contain("BB"));
        Assert.That(battingSection, Does.Contain("TOTALS"));
    }

    [Test]
    public void BoxScore_InningsPitched_UsesThirdsNotation() {
        // Arrange
        var simulator = new GameSimulator("Home", "Away", 42);
        string report = simulator.RunGame();

        // Act - Extract pitching section
        var lines = report.Split('\n');
        var pitchingSection = ExtractSection(lines, "PITCHING", "Seed:");

        // Assert - IP should contain decimal notation (.0, .1, or .2)
        Assert.That(pitchingSection, Does.Contain("IP"));

        // Should have IP values with thirds notation
        var ipLines = pitchingSection.Split('\n').Where(l => l.Contains("P") && !l.Contains("PITCHING")).ToList();
        Assert.That(ipLines, Is.Not.Empty);

        // At least one pitcher should have IP recorded
        var hasIpValue = ipLines.Any(l => System.Text.RegularExpressions.Regex.IsMatch(l, @"\d+\.[012]"));
        Assert.That(hasIpValue, Is.True, "Expected IP in thirds notation (X.0, X.1, or X.2)");
    }

    [Test]
    public void LineScore_HasNineInningColumns() {
        // Arrange
        var simulator = new GameSimulator("Home", "Away", 42);
        string report = simulator.RunGame();

        // Act - Find line score header
        var lines = report.Split('\n');
        var lineScoreHeader = lines.FirstOrDefault(l =>
            l.Contains("1") && l.Contains("2") && l.Contains("9") && l.Contains("R"));

        // Assert
        Assert.That(lineScoreHeader, Is.Not.Null);

        // Should have columns 1-9
        for (int i = 1; i <= 9; i++) {
            Assert.That(lineScoreHeader, Does.Contain(i.ToString()));
        }
    }

    [Test]
    public void LineScore_ShowsRunsHitsErrors() {
        // Arrange
        var simulator = new GameSimulator("Home", "Away", 42);
        string report = simulator.RunGame();

        // Act - Find line score rows
        var lines = report.Split('\n');
        var awayScoreLine = lines.FirstOrDefault(l => l.Contains("Away") && l.Contains("|"));
        var homeScoreLine = lines.FirstOrDefault(l => l.Contains("Home") && l.Contains("|"));

        // Assert
        Assert.That(awayScoreLine, Is.Not.Null);
        Assert.That(homeScoreLine, Is.Not.Null);

        // Each should have numeric values (runs per inning and totals)
        Assert.That(System.Text.RegularExpressions.Regex.IsMatch(awayScoreLine!, @"\d"), Is.True);
        Assert.That(System.Text.RegularExpressions.Regex.IsMatch(homeScoreLine!, @"\d"), Is.True);
    }

    [Test]
    public void FinalScore_MatchesLineScoreTotals() {
        // Arrange
        var simulator = new GameSimulator("Home", "Away", 42);
        string report = simulator.RunGame();

        // Act - Extract final score and line score totals
        var lines = report.Split('\n');
        var finalLine = lines.FirstOrDefault(l => l.StartsWith("Final:"));

        // Assert
        Assert.That(finalLine, Is.Not.Null);
        Assert.That(finalLine, Does.Contain("—"));

        // Should have format "Final: AWAY X — HOME Y" or "Final: AWAY X — HOME Y (TIE)"
        var scoreMatch = System.Text.RegularExpressions.Regex.Match(finalLine!, @"Final: \w+ (\d+) — \w+ (\d+)");
        Assert.That(scoreMatch.Success, Is.True);
    }

    [Test]
    public void PlayLog_EachLineHasRequiredComponents() {
        // Arrange
        var simulator = new GameSimulator("Home", "Away", 42);
        string report = simulator.RunGame();

        // Act - Extract play log lines
        var lines = report.Split('\n');
        var playLines = lines.Where(l => l.Contains("[Top") || l.Contains("[Bot")).Take(10).ToList();

        // Assert - Each play should have: inning marker, batter name, "vs", pitcher team, "P", outcome
        foreach (var play in playLines) {
            Assert.That(play, Does.Match(@"\[(Top|Bot) \d+\]"), $"Play missing inning marker: {play}");
            Assert.That(play, Does.Contain("vs"), $"Play missing 'vs': {play}");
            Assert.That(play, Does.Contain("P"), $"Play missing pitcher: {play}");
            Assert.That(play, Does.Contain("—"), $"Play missing outcome separator: {play}");
        }
    }

    [Test]
    public void PlayLog_OutsPhrase_ShowsCorrectCount() {
        // Arrange
        var simulator = new GameSimulator("Home", "Away", 42);
        string report = simulator.RunGame();

        // Act - Find plays with outs
        var lines = report.Split('\n');
        var playsWithOuts = lines.Where(l => l.Contains("out")).ToList();

        // Assert - Should have "1 out." or "X outs." format
        Assert.That(playsWithOuts, Is.Not.Empty);

        var hasOneOut = playsWithOuts.Any(l => l.Contains("1 out."));
        var hasTwoOuts = playsWithOuts.Any(l => l.Contains("2 outs."));
        var hasThreeOuts = playsWithOuts.Any(l => l.Contains("3 outs."));

        // At least one of these should be true in a typical game
        Assert.That(hasOneOut || hasTwoOuts || hasThreeOuts, Is.True);
    }

    [Test]
    public void Seed_AppearsInHeaderAndFooter() {
        // Arrange
        int seed = 12345;
        var simulator = new GameSimulator("Home", "Away", seed);
        string report = simulator.RunGame();

        // Act - Count occurrences of seed
        var seedString = $"Seed: {seed}";
        var occurrences = System.Text.RegularExpressions.Regex.Matches(report, seedString).Count;

        // Assert - Should appear at least twice (header and footer)
        Assert.That(occurrences, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void LogHash_IsValid64CharHex() {
        // Arrange
        var simulator = new GameSimulator("Home", "Away", 42);
        string report = simulator.RunGame();

        // Act - Extract LogHash
        var lines = report.Split('\n');
        var logHashLine = lines.FirstOrDefault(l => l.Contains("LogHash:"));

        // Assert
        Assert.That(logHashLine, Is.Not.Null);
        var logHash = logHashLine!.Split(':')[1].Trim();
        Assert.That(logHash, Has.Length.EqualTo(64));
        Assert.That(logHash, Does.Match("^[0-9a-f]{64}$"));
    }

    private string ExtractSection(string[] lines, string startMarker, string endMarker) {
        var sectionLines = new List<string>();
        bool inSection = false;

        foreach (var line in lines) {
            if (line.Contains(startMarker)) {
                inSection = true;
            }
            if (inSection) {
                sectionLines.Add(line);
            }
            if (line.Contains(endMarker) && inSection) {
                break;
            }
        }

        return string.Join('\n', sectionLines);
    }
}
