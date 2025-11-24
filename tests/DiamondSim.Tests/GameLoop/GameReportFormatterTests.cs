namespace DiamondSim.Tests.GameLoop;

/// <summary>
/// Tests for GameReportFormatter - verifies the string formatting of game reports.
/// These tests use controlled GameResult objects to test formatting logic without running full simulations.
/// </summary>
[TestFixture]
public class GameReportFormatterTests {

    [Test]
    public void FormattedReport_ContainsPlayLogWithInningMarkers() {
        // Arrange
        var result = CreateTestGameResult();

        // Act
        var report = result.ToConsoleReport();

        // Assert - Should have inning markers
        var lines = report.Split('\n');
        var playLines = lines.Where(l => l.Contains("[Top") || l.Contains("[Bot")).ToList();

        Assert.That(playLines, Is.Not.Empty);

        // Should have Top 1 through at least Top 9
        Assert.That(playLines.Any(l => l.Contains("[Top 1]")), Is.True);
        Assert.That(playLines.Any(l => l.Contains("[Top 9]")), Is.True);
    }

    [Test]
    public void FormattedReport_BoxScore_ContainsRequiredColumns() {
        // Arrange
        var result = CreateTestGameResult();

        // Act
        var report = result.ToConsoleReport();

        // Assert - Verify batting stats columns are present
        var lines = report.Split('\n');
        var battingSection = ExtractSection(lines, "BATTING", "PITCHING");
        Assert.That(battingSection, Does.Contain("AB"));
        Assert.That(battingSection, Does.Contain("BB"));
        Assert.That(battingSection, Does.Contain("TOTALS"));
    }

    [Test]
    public void FormattedReport_InningsPitched_UsesThirdsNotation() {
        // Arrange - Create result with pitcher who recorded 5 outs (1.2 IP)
        var result = CreateGameResultWithPitcherStats(outsRecorded: 5);

        // Act
        string report = result.ToConsoleReport();

        // Assert - IP should contain decimal notation (.0, .1, or .2)
        var lines = report.Split('\n');
        var pitchingSection = ExtractSection(lines, "PITCHING", "Seed:");
        Assert.That(pitchingSection, Does.Contain("IP"));

        // Should have IP values with thirds notation
        var ipLines = pitchingSection.Split('\n').Where(l => l.Contains("P") && !l.Contains("PITCHING")).ToList();
        Assert.That(ipLines, Is.Not.Empty);

        // Should have IP in thirds notation (X.0, X.1, or X.2)
        var hasIpValue = ipLines.Any(l => System.Text.RegularExpressions.Regex.IsMatch(l, @"\d+\.[012]"));
        Assert.That(hasIpValue, Is.True, "Expected IP in thirds notation (X.0, X.1, or X.2)");
    }

    [Test]
    public void FormattedReport_LineScore_HasNineInningColumns() {
        // Arrange
        var result = CreateTestGameResult();

        // Act
        string report = result.ToConsoleReport();

        // Assert
        var lines = report.Split('\n');
        var lineScoreHeader = lines.FirstOrDefault(l =>
            l.Contains("1") && l.Contains("2") && l.Contains("9") && l.Contains("R"));
        Assert.That(lineScoreHeader, Is.Not.Null);

        // Should have columns 1-9
        for (var i = 1; i <= 9; i++) {
            Assert.That(lineScoreHeader, Does.Contain(i.ToString()));
        }
    }

    [Test]
    public void FormattedReport_LineScore_ShowsRunsHitsErrors() {
        // Arrange
        var result = CreateTestGameResult(homeScore: 7, awayScore: 4);

        // Act
        string report = result.ToConsoleReport();

        // Assert
        var lines = report.Split('\n');
        var awayScoreLine = lines.FirstOrDefault(l => l.Contains("Away") && l.Contains("|"));
        var homeScoreLine = lines.FirstOrDefault(l => l.Contains("Home") && l.Contains("|"));
        Assert.That(awayScoreLine, Is.Not.Null);
        Assert.That(homeScoreLine, Is.Not.Null);

        // Each should have numeric values (runs per inning and totals)
        Assert.That(System.Text.RegularExpressions.Regex.IsMatch(awayScoreLine!, @"\d"), Is.True);
        Assert.That(System.Text.RegularExpressions.Regex.IsMatch(homeScoreLine!, @"\d"), Is.True);
    }

    [Test]
    public void FormattedReport_FinalScore_MatchesLineScoreTotals() {
        // Arrange
        var result = CreateTestGameResult(homeScore: 6, awayScore: 2);

        // Act
        string report = result.ToConsoleReport();

        // Assert
        var lines = report.Split('\n');
        var finalLine = lines.FirstOrDefault(l => l.StartsWith("Final:"));
        Assert.That(finalLine, Is.Not.Null);
        Assert.That(finalLine, Does.Contain("—"));

        // Should have format "Final: AWAY X — HOME Y"
        var scoreMatch = System.Text.RegularExpressions.Regex.Match(finalLine!, @"Final: \w+ (\d+) — \w+ (\d+)");
        Assert.That(scoreMatch.Success, Is.True);

        // Verify the scores match what we set
        Assert.That(scoreMatch.Groups[1].Value, Is.EqualTo("2")); // Away score
        Assert.That(scoreMatch.Groups[2].Value, Is.EqualTo("6")); // Home score
    }

    [Test]
    public void FormattedReport_PlayLog_EachLineHasRequiredComponents() {
        // Arrange
        var result = CreateTestGameResult();

        // Act
        string report = result.ToConsoleReport();

        // Assert - Each play should have: inning marker, batter name, "vs", pitcher team, "P", outcome
        var lines = report.Split('\n');
        var playLines = lines.Where(l => l.Contains("[Top") || l.Contains("[Bot")).Take(10).ToList();
        foreach (var play in playLines) {
            Assert.That(play, Does.Match(@"\[(Top|Bot) \d+\]"), $"Play missing inning marker: {play}");
            Assert.That(play, Does.Contain("vs"), $"Play missing 'vs': {play}");
            Assert.That(play, Does.Contain("P"), $"Play missing pitcher: {play}");
            Assert.That(play, Does.Contain("—"), $"Play missing outcome separator: {play}");
        }
    }

    [Test]
    public void FormattedReport_PlayLog_OutsPhrase_ShowsCorrectCount() {
        // Arrange
        var result = CreateTestGameResult();

        // Act
        string report = result.ToConsoleReport();

        // Assert - Should have "1 out." or "X outs." format
        var lines = report.Split('\n');
        var playsWithOuts = lines.Where(l => l.Contains("out")).ToList();
        Assert.That(playsWithOuts, Is.Not.Empty);

        var hasOneOut = playsWithOuts.Any(l => l.Contains("1 out."));
        var hasTwoOuts = playsWithOuts.Any(l => l.Contains("2 outs."));
        var hasThreeOuts = playsWithOuts.Any(l => l.Contains("3 outs."));

        // At least one of these should be true in a typical game
        Assert.That(hasOneOut || hasTwoOuts || hasThreeOuts, Is.True);
    }

    [Test]
    public void FormattedReport_Seed_AppearsInHeaderAndFooter() {
        // Arrange
        var seed = 12345;
        var result = CreateTestGameResult(seed: seed);

        // Act
        string report = result.ToConsoleReport();

        // Assert - Should appear at least twice (header and footer)
        var seedString = $"Seed: {seed}";
        var occurrences = System.Text.RegularExpressions.Regex.Matches(report, seedString).Count;
        Assert.That(occurrences, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void FormattedReport_BoxScore_ShowsCorrectBattingColumns() {
        // Arrange
        var result = CreateTestGameResult();

        // Act
        string report = result.ToConsoleReport();

        // Assert - Verify all expected batting stat columns are present
        Assert.That(report, Does.Contain("PA"));
        Assert.That(report, Does.Contain("AB"));
        Assert.That(report, Does.Contain("H"));
        Assert.That(report, Does.Contain("RBI"));
        Assert.That(report, Does.Contain("BB"));
        Assert.That(report, Does.Contain("K"));
        Assert.That(report, Does.Contain("HR"));
    }

    [Test]
    public void FormattedReport_BoxScore_ShowsCorrectPitchingColumns() {
        // Arrange
        var result = CreateGameResultWithPitcherStats(outsRecorded: 9);

        // Act
        string report = result.ToConsoleReport();

        // Assert - Verify all expected pitching stat columns are present
        Assert.That(report, Does.Contain("IP"));
        Assert.That(report, Does.Contain("BF"));
        Assert.That(report, Does.Contain("H"));
        Assert.That(report, Does.Contain("R"));
        Assert.That(report, Does.Contain("ER"));
        Assert.That(report, Does.Contain("BB"));
        Assert.That(report, Does.Contain("K"));
        Assert.That(report, Does.Contain("HR"));
    }

    [Test]
    public void FormattedReport_InningsPitched_ConvertsOutsCorrectly() {
        // Arrange - Test various outs to IP conversions
        var testCases = new[] {
            (outs: 3, expectedIP: "1.0"),
            (outs: 4, expectedIP: "1.1"),
            (outs: 5, expectedIP: "1.2"),
            (outs: 6, expectedIP: "2.0"),
            (outs: 27, expectedIP: "9.0")
        };

        foreach (var (outs, expectedIP) in testCases) {
            // Arrange - Create a result with no runs to avoid validation issues
            var result = CreateTestGameResult(homeScore: 0, awayScore: 0);
            var boxScore = new BoxScore();

            // Add minimal batting stats
            for (var i = 0; i < 9; i++) {
                boxScore.IncrementBatterStats(Team.Away, i, PaType.K, 0, 0, false);
                boxScore.IncrementBatterStats(Team.Home, i, PaType.K, 0, 0, false);
            }

            // Add pitcher with specific outs recorded (no runs)
            for (var i = 0; i < outs; i++) {
                boxScore.IncrementPitcherStats(Team.Home, 0, PaType.K, 1, 0);
                boxScore.IncrementPitcherStats(Team.Away, 0, PaType.K, 1, 0);
            }

            var testResult = new GameResult(
                result.Metadata,
                boxScore,
                result.LineScore,
                result.PlayLog,
                result.FinalState,
                result.HomeLineup,
                result.AwayLineup);

            // Act
            string report = testResult.ToConsoleReport();

            // Assert
            Assert.That(report, Does.Contain(expectedIP),
                $"Expected {outs} outs to display as {expectedIP} IP");
        }
    }

    [Test]
    public void FormattedReport_TeamNames_AppearInReport() {
        // Arrange
        var result = CreateTestGameResult(homeTeam: "Yankees", awayTeam: "RedSox");

        // Act
        string report = result.ToConsoleReport();

        // Assert
        Assert.That(report, Does.Contain("Yankees"));
        Assert.That(report, Does.Contain("RedSox"));
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

    /// <summary>
    /// Creates a minimal GameResult with controlled data for testing formatting.
    /// </summary>
    private GameResult CreateTestGameResult(
        string homeTeam = "Home",
        string awayTeam = "Away",
        int seed = 42,
        int homeScore = 5,
        int awayScore = 3,
        int innings = 9) {

        var metadata = new GameMetadata(homeTeam, awayTeam, seed, DateTime.Now);
        var boxScore = new BoxScore();
        var lineScore = new LineScore();
        var playLog = new List<PlayLogEntry>();

        // Add minimal batting stats for both teams
        for (var i = 0; i < 9; i++) {
            // Away team batters
            boxScore.IncrementBatterStats(Team.Away, i, PaType.Single, 0, 0, false);
            boxScore.IncrementBatterStats(Team.Away, i, PaType.K, 0, 0, false);

            // Home team batters
            boxScore.IncrementBatterStats(Team.Home, i, PaType.Single, 0, 0, false);
            boxScore.IncrementBatterStats(Team.Home, i, PaType.BB, 0, 0, false);
        }

        // Add pitching stats that match the runs scored
        // Home pitchers allowed awayScore runs, Away pitchers allowed homeScore runs
        for (var i = 0; i < 27; i++) {
            // Home pitcher (facing away batters) - distribute away runs across outs
            var runsThisOut = (i < awayScore) ? 1 : 0;
            boxScore.IncrementPitcherStats(Team.Home, 0, PaType.K, 1, runsThisOut);

            // Away pitcher (facing home batters) - distribute home runs across outs
            runsThisOut = (i < homeScore) ? 1 : 0;
            boxScore.IncrementPitcherStats(Team.Away, 0, PaType.K, 1, runsThisOut);
        }

        // Build line score with controlled runs per inning
        var awayRunsRemaining = awayScore;
        var homeRunsRemaining = homeScore;

        for (var inning = 1; inning <= innings; inning++) {
            var awayRuns = Math.Min(1, awayRunsRemaining);
            awayRunsRemaining -= awayRuns;
            lineScore.RecordInning(Team.Away, awayRuns);

            if (inning < innings || homeScore <= awayScore) {
                var homeRuns = Math.Min(1, homeRunsRemaining);
                homeRunsRemaining -= homeRuns;
                lineScore.RecordInning(Team.Home, homeRuns);
            }
            else {
                // Skip bottom 9th if home is winning
                lineScore.RecordSkippedInning(Team.Home);
            }
        }

        // Create minimal play log entries
        for (var inning = 1; inning <= Math.Min(innings, 9); inning++) {
            var topResolution = new PaResolution(
                OutsAdded: 1,
                RunsScored: 0,
                NewBases: new BaseState(false, false, false),
                Type: PaType.Single,
                Tag: OutcomeTag.Single);

            var topEntry = new PlayLogEntry(
                inning,
                InningHalf.Top,
                $"Away B{inning}",
                homeTeam,
                topResolution,
                isWalkoff: false,
                outsAfter: 1);
            playLog.Add(topEntry);

            if (inning < 9 || homeScore <= awayScore) {
                var botResolution = new PaResolution(
                    OutsAdded: 1,
                    RunsScored: 0,
                    NewBases: new BaseState(false, false, false),
                    Type: PaType.K,
                    Tag: OutcomeTag.K);

                var botEntry = new PlayLogEntry(
                    inning,
                    InningHalf.Bottom,
                    $"Home B{inning}",
                    awayTeam,
                    botResolution,
                    isWalkoff: false,
                    outsAfter: 1);
                playLog.Add(botEntry);
            }
        }

        var finalState = new GameState(
            balls: 0,
            strikes: 0,
            inning: innings,
            half: InningHalf.Bottom,
            outs: 0,
            onFirst: false,
            onSecond: false,
            onThird: false,
            awayScore: awayScore,
            homeScore: homeScore,
            awayBattingOrderIndex: 0,
            homeBattingOrderIndex: 0,
            offense: Team.Home,
            defense: Team.Away,
            isFinal: true);

        var homeLineup = new TeamLineup(homeTeam, Enumerable.Range(0, 9).Select(i =>
            new Batter($"Home B{i + 1}", BatterRatings.Average)).ToList());
        var awayLineup = new TeamLineup(awayTeam, Enumerable.Range(0, 9).Select(i =>
            new Batter($"Away B{i + 1}", BatterRatings.Average)).ToList());

        return new GameResult(metadata, boxScore, lineScore, playLog, finalState, homeLineup, awayLineup);
    }

    /// <summary>
    /// Creates a GameResult with specific pitcher stats to test IP thirds notation.
    /// </summary>
    private GameResult CreateGameResultWithPitcherStats(int outsRecorded) {
        var result = CreateTestGameResult();
        var boxScore = new BoxScore();

        // Add minimal batting stats for both teams
        for (var i = 0; i < 9; i++) {
            boxScore.IncrementBatterStats(Team.Away, i, PaType.Single, 0, 0, false);
            boxScore.IncrementBatterStats(Team.Home, i, PaType.Single, 0, 0, false);
        }

        // Add pitcher with specific outs recorded
        // Distribute the runs from the original result across the outs
        var awayScore = result.FinalState.AwayScore;
        var homeScore = result.FinalState.HomeScore;

        for (var i = 0; i < outsRecorded; i++) {
            // Home pitcher (facing away batters)
            var runsThisOut = (i < awayScore) ? 1 : 0;
            boxScore.IncrementPitcherStats(Team.Home, 0, PaType.K, 1, runsThisOut);

            // Away pitcher (facing home batters)
            runsThisOut = (i < homeScore) ? 1 : 0;
            boxScore.IncrementPitcherStats(Team.Away, 0, PaType.K, 1, runsThisOut);
        }

        return new GameResult(
            result.Metadata,
            boxScore,
            result.LineScore,
            result.PlayLog,
            result.FinalState,
            result.HomeLineup,
            result.AwayLineup);
    }
}
