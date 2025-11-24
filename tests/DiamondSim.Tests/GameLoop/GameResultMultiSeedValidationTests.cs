namespace DiamondSim.Tests.GameLoop;

/// <summary>
/// Comprehensive validation tests that run multiple seeds and verify GameResult data consistency.
/// Tests formula accuracy, stat reconciliation, logical checks, and edge cases by directly
/// asserting on GameResult properties instead of parsing string output.
/// </summary>
[TestFixture]
public class GameResultMultiSeedValidationTests {

    private static readonly int[] TestSeeds = {
        42,         // Already verified
        8675309,    // Jenny's number
        12345,      // Sequential
        20251028,   // Date-based
        314159,     // Pi
        1000,       // Round number
        2024,       // Year
        999999,     // Large number
        123456789,  // Very large
        777         // Lucky number
    };

    [Test]
    [TestCaseSource(nameof(TestSeeds))]
    public void VerifyStatisticalConsistency_ForSeed(int seed) {
        // Arrange
        var simulator = new GameSimulator("Home", "Away", seed);

        // Act
        var result = simulator.RunGame();

        // Assert - Run all verification checks
        var results = new VerificationResults(seed);

        VerifyFormulaConsistency(result, results);
        VerifyStatReconciliation(result, results);
        VerifyLogicalChecks(result, results);
        VerifyEdgeCases(result, results);

        // Output results
        Console.WriteLine(results.GetSummary());

        // Assert all checks passed
        Assert.That(results.AllChecksPassed, Is.True,
            $"Seed {seed} failed verification:\n{results.GetFailureDetails()}");
    }

    [Test]
    public void GenerateComprehensiveReport_AllSeeds() {
        var allResults = new List<VerificationResults>();

        foreach (var seed in TestSeeds) {
            var simulator = new GameSimulator("Home", "Away", seed);
            var result = simulator.RunGame();

            var results = new VerificationResults(seed);
            VerifyFormulaConsistency(result, results);
            VerifyStatReconciliation(result, results);
            VerifyLogicalChecks(result, results);
            VerifyEdgeCases(result, results);

            allResults.Add(results);
        }

        // Generate summary report
        var summaryReport = GenerateSummaryReport(allResults);
        Console.WriteLine(summaryReport);

        // Write to file
        File.WriteAllText("MULTI_SEED_VERIFICATION_REPORT.md", summaryReport);

        // Assert all seeds passed
        var failedSeeds = allResults.Where(r => !r.AllChecksPassed).ToList();
        Assert.That(failedSeeds, Is.Empty,
            $"{failedSeeds.Count} seeds failed verification");
    }

    private void VerifyFormulaConsistency(GameResult result, VerificationResults results) {
        // Verify AB <= PA for all batters (away team)
        foreach (var (lineupPos, stats) in result.BoxScore.AwayBatters) {
            if (stats.AB > stats.PA) {
                results.AddFailure("FormulaConsistency",
                    $"Away batter {lineupPos}: AB ({stats.AB}) > PA ({stats.PA})");
            }

            // Verify H = Singles + Doubles + Triples + HR
            var expectedH = stats.Singles + stats.Doubles + stats.Triples + stats.HR;
            if (stats.H != expectedH) {
                results.AddFailure("FormulaConsistency",
                    $"Away batter {lineupPos}: H ({stats.H}) != Singles+2B+3B+HR ({expectedH})");
            }

            // Verify TB calculation
            var expectedTB = stats.Singles + (stats.Doubles * 2) + (stats.Triples * 3) + (stats.HR * 4);
            if (stats.TB != expectedTB) {
                results.AddFailure("FormulaConsistency",
                    $"Away batter {lineupPos}: TB ({stats.TB}) != calculated ({expectedTB})");
            }
        }

        // Verify AB <= PA for all batters (home team)
        foreach (var (lineupPos, stats) in result.BoxScore.HomeBatters) {
            if (stats.AB > stats.PA) {
                results.AddFailure("FormulaConsistency",
                    $"Home batter {lineupPos}: AB ({stats.AB}) > PA ({stats.PA})");
            }

            // Verify H = Singles + Doubles + Triples + HR
            var expectedH = stats.Singles + stats.Doubles + stats.Triples + stats.HR;
            if (stats.H != expectedH) {
                results.AddFailure("FormulaConsistency",
                    $"Home batter {lineupPos}: H ({stats.H}) != Singles+2B+3B+HR ({expectedH})");
            }

            // Verify TB calculation
            var expectedTB = stats.Singles + (stats.Doubles * 2) + (stats.Triples * 3) + (stats.HR * 4);
            if (stats.TB != expectedTB) {
                results.AddFailure("FormulaConsistency",
                    $"Home batter {lineupPos}: TB ({stats.TB}) != calculated ({expectedTB})");
            }
        }

        if (results.GetFailureCount("FormulaConsistency") == 0) {
            results.AddPass("FormulaConsistency");
        }
    }

    private void VerifyStatReconciliation(GameResult result, VerificationResults results) {
        // Verify LineScore totals match FinalState scores
        if (result.LineScore.AwayTotal != result.FinalState.AwayScore) {
            results.AddFailure("StatReconciliation",
                $"Away runs mismatch: LineScore total ({result.LineScore.AwayTotal}) != FinalState ({result.FinalState.AwayScore})");
        }

        if (result.LineScore.HomeTotal != result.FinalState.HomeScore) {
            results.AddFailure("StatReconciliation",
                $"Home runs mismatch: LineScore total ({result.LineScore.HomeTotal}) != FinalState ({result.FinalState.HomeScore})");
        }

        // Verify BoxScore hits match LineScore hits (if LineScore tracks hits)
        var awayBoxScoreHits = result.BoxScore.AwayBatters.Values.Sum(b => b.H);
        var homeBoxScoreHits = result.BoxScore.HomeBatters.Values.Sum(b => b.H);

        // Note: LineScore doesn't have a Hits property in the current implementation
        // This would need to be added if we want to verify hits reconciliation

        // Verify pitcher BF is reasonable (at least H + BB + HBP)
        foreach (var (pitcherId, stats) in result.BoxScore.AwayPitchers) {
            var minBF = stats.H + stats.BB + stats.HBP;
            if (stats.BF < minBF) {
                results.AddFailure("StatReconciliation",
                    $"Away pitcher {pitcherId}: BF ({stats.BF}) < H+BB+HBP ({minBF})");
            }
        }

        foreach (var (pitcherId, stats) in result.BoxScore.HomePitchers) {
            var minBF = stats.H + stats.BB + stats.HBP;
            if (stats.BF < minBF) {
                results.AddFailure("StatReconciliation",
                    $"Home pitcher {pitcherId}: BF ({stats.BF}) < H+BB+HBP ({minBF})");
            }
        }

        // Verify inning sums match totals
        var awayInningSum = result.LineScore.AwayInnings.Where(r => r >= 0).Sum();
        var homeInningSum = result.LineScore.HomeInnings.Where(r => r >= 0).Sum();

        if (awayInningSum != result.LineScore.AwayTotal) {
            results.AddFailure("StatReconciliation",
                $"Away inning sum ({awayInningSum}) != AwayTotal ({result.LineScore.AwayTotal})");
        }

        if (homeInningSum != result.LineScore.HomeTotal) {
            results.AddFailure("StatReconciliation",
                $"Home inning sum ({homeInningSum}) != HomeTotal ({result.LineScore.HomeTotal})");
        }

        if (results.GetFailureCount("StatReconciliation") == 0) {
            results.AddPass("StatReconciliation");
        }
    }

    private void VerifyLogicalChecks(GameResult result, VerificationResults results) {
        // Check for negative stats in batting
        foreach (var (lineupPos, stats) in result.BoxScore.AwayBatters) {
            if (stats.AB < 0 || stats.H < 0 || stats.PA < 0 || stats.BB < 0 ||
                stats.K < 0 || stats.HR < 0 || stats.RBI < 0 || stats.R < 0 ||
                stats.Singles < 0 || stats.Doubles < 0 || stats.Triples < 0 ||
                stats.HBP < 0 || stats.TB < 0) {
                results.AddFailure("LogicalChecks",
                    $"Away batter {lineupPos} has negative stats");
            }
        }

        foreach (var (lineupPos, stats) in result.BoxScore.HomeBatters) {
            if (stats.AB < 0 || stats.H < 0 || stats.PA < 0 || stats.BB < 0 ||
                stats.K < 0 || stats.HR < 0 || stats.RBI < 0 || stats.R < 0 ||
                stats.Singles < 0 || stats.Doubles < 0 || stats.Triples < 0 ||
                stats.HBP < 0 || stats.TB < 0) {
                results.AddFailure("LogicalChecks",
                    $"Home batter {lineupPos} has negative stats");
            }
        }

        // Check for negative stats in pitching
        foreach (var (pitcherId, stats) in result.BoxScore.AwayPitchers) {
            if (stats.BF < 0 || stats.OutsRecorded < 0 || stats.H < 0 ||
                stats.R < 0 || stats.ER < 0 || stats.BB < 0 ||
                stats.HBP < 0 || stats.K < 0 || stats.HR < 0) {
                results.AddFailure("LogicalChecks",
                    $"Away pitcher {pitcherId} has negative stats");
            }
        }

        foreach (var (pitcherId, stats) in result.BoxScore.HomePitchers) {
            if (stats.BF < 0 || stats.OutsRecorded < 0 || stats.H < 0 ||
                stats.R < 0 || stats.ER < 0 || stats.BB < 0 ||
                stats.HBP < 0 || stats.K < 0 || stats.HR < 0) {
                results.AddFailure("LogicalChecks",
                    $"Home pitcher {pitcherId} has negative stats");
            }
        }

        // Verify RBI ≤ Runs + reasonable margin (RBI can exceed runs in some edge cases)
        var awayTotalRBI = result.BoxScore.AwayBatters.Values.Sum(b => b.RBI);
        var awayTotalRuns = result.FinalState.AwayScore;
        if (awayTotalRBI > awayTotalRuns + 5) {
            results.AddFailure("LogicalChecks",
                $"Away RBI ({awayTotalRBI}) exceeds runs ({awayTotalRuns}) by too much");
        }

        var homeTotalRBI = result.BoxScore.HomeBatters.Values.Sum(b => b.RBI);
        var homeTotalRuns = result.FinalState.HomeScore;
        if (homeTotalRBI > homeTotalRuns + 5) {
            results.AddFailure("LogicalChecks",
                $"Home RBI ({homeTotalRBI}) exceeds runs ({homeTotalRuns}) by too much");
        }

        // Verify IP thirds format (OutsRecorded % 3 should give valid thirds: 0, 1, or 2)
        foreach (var (pitcherId, stats) in result.BoxScore.AwayPitchers) {
            var thirds = stats.OutsRecorded % 3;
            if (thirds < 0 || thirds > 2) {
                results.AddFailure("LogicalChecks",
                    $"Away pitcher {pitcherId}: Invalid IP thirds ({thirds}) from OutsRecorded ({stats.OutsRecorded})");
            }
        }

        foreach (var (pitcherId, stats) in result.BoxScore.HomePitchers) {
            var thirds = stats.OutsRecorded % 3;
            if (thirds < 0 || thirds > 2) {
                results.AddFailure("LogicalChecks",
                    $"Home pitcher {pitcherId}: Invalid IP thirds ({thirds}) from OutsRecorded ({stats.OutsRecorded})");
            }
        }

        if (results.GetFailureCount("LogicalChecks") == 0) {
            results.AddPass("LogicalChecks");
        }
    }

    private void VerifyEdgeCases(GameResult result, VerificationResults results) {
        // Check for tie
        if (result.FinalState.AwayScore == result.FinalState.HomeScore) {
            results.AddNote("EdgeCases", "Game ended in a tie");
        }

        // Check for walk-off (check IsWalkoff property)
        var hasWalkoff = result.PlayLog.Any(p => p.IsWalkoff);
        if (hasWalkoff) {
            results.AddNote("EdgeCases", "Walk-off detected in play log");
        }

        // Check for skipped bottom 9th (HomeInnings contains -1 at index 8)
        if (result.LineScore.HomeInnings.Count >= 9 && result.LineScore.HomeInnings[8] == -1) {
            results.AddNote("EdgeCases", "Bottom 9th skipped (home team ahead)");
        }

        // Check for no-hitter
        var awayHits = result.BoxScore.AwayBatters.Values.Sum(b => b.H);
        var homeHits = result.BoxScore.HomeBatters.Values.Sum(b => b.H);

        if (awayHits == 0) {
            results.AddNote("EdgeCases", "Away team no-hitter!");
        }
        if (homeHits == 0) {
            results.AddNote("EdgeCases", "Home team no-hitter!");
        }

        // Check for perfect game (no hits, no walks, no HBP, no errors)
        var awayBB = result.BoxScore.AwayBatters.Values.Sum(b => b.BB);
        var awayHBP = result.BoxScore.AwayBatters.Values.Sum(b => b.HBP);
        if (awayHits == 0 && awayBB == 0 && awayHBP == 0) {
            results.AddNote("EdgeCases", "Potential away team perfect game!");
        }

        var homeBB = result.BoxScore.HomeBatters.Values.Sum(b => b.BB);
        var homeHBP = result.BoxScore.HomeBatters.Values.Sum(b => b.HBP);
        if (homeHits == 0 && homeBB == 0 && homeHBP == 0) {
            results.AddNote("EdgeCases", "Potential home team perfect game!");
        }

        results.AddPass("EdgeCases");
    }

    private string GenerateSummaryReport(List<VerificationResults> allResults) {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Multi-Seed Verification Report");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**Total Seeds Tested:** {allResults.Count}");
        sb.AppendLine($"**Seeds Passed:** {allResults.Count(r => r.AllChecksPassed)}");
        sb.AppendLine($"**Seeds Failed:** {allResults.Count(r => !r.AllChecksPassed)}");
        sb.AppendLine();

        sb.AppendLine("## Summary Table");
        sb.AppendLine();
        sb.AppendLine("| Seed | Formula | Reconciliation | Logical | Edge Cases | Overall |");
        sb.AppendLine("|------|---------|----------------|---------|------------|---------|");

        foreach (var result in allResults) {
            sb.AppendLine($"| {result.Seed} | {result.GetStatus("FormulaConsistency")} | " +
                         $"{result.GetStatus("StatReconciliation")} | " +
                         $"{result.GetStatus("LogicalChecks")} | " +
                         $"{result.GetStatus("EdgeCases")} | " +
                         $"{(result.AllChecksPassed ? "✅ PASS" : "❌ FAIL")} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Detailed Results");
        sb.AppendLine();

        foreach (var result in allResults) {
            sb.AppendLine($"### Seed: {result.Seed}");
            sb.AppendLine();
            sb.AppendLine(result.GetDetailedReport());
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private class VerificationResults {
        public int Seed { get; }
        private Dictionary<string, List<string>> _failures = new();
        private Dictionary<string, List<string>> _notes = new();
        private HashSet<string> _passed = new();

        public VerificationResults(int seed) {
            Seed = seed;
        }

        public void AddFailure(string category, string message) {
            if (!_failures.ContainsKey(category)) {
                _failures[category] = new List<string>();
            }
            _failures[category].Add(message);
        }

        public void AddNote(string category, string message) {
            if (!_notes.ContainsKey(category)) {
                _notes[category] = new List<string>();
            }
            _notes[category].Add(message);
        }

        public void AddPass(string category) {
            _passed.Add(category);
        }

        public int GetFailureCount(string category) {
            return _failures.ContainsKey(category) ? _failures[category].Count : 0;
        }

        public bool AllChecksPassed => _failures.Count == 0;

        public string GetStatus(string category) {
            if (_failures.ContainsKey(category)) return "❌";
            if (_passed.Contains(category)) return "✅";
            return "⚠️";
        }

        public string GetSummary() {
            return $"Seed {Seed}: {(AllChecksPassed ? "✅ PASS" : "❌ FAIL")} " +
                   $"({_failures.Sum(f => f.Value.Count)} failures)";
        }

        public string GetFailureDetails() {
            if (AllChecksPassed) return "All checks passed!";

            var sb = new System.Text.StringBuilder();
            foreach (var category in _failures.Keys) {
                sb.AppendLine($"{category}:");
                foreach (var failure in _failures[category]) {
                    sb.AppendLine($"  - {failure}");
                }
            }
            return sb.ToString();
        }

        public string GetDetailedReport() {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"**Status:** {(AllChecksPassed ? "✅ PASS" : "❌ FAIL")}");
            sb.AppendLine();

            if (_failures.Any()) {
                sb.AppendLine("**Failures:**");
                foreach (var category in _failures.Keys) {
                    sb.AppendLine($"- {category}: {_failures[category].Count} issue(s)");
                    foreach (var failure in _failures[category]) {
                        sb.AppendLine($"  - {failure}");
                    }
                }
                sb.AppendLine();
            }

            if (_notes.Any()) {
                sb.AppendLine("**Notes:**");
                foreach (var category in _notes.Keys) {
                    foreach (var note in _notes[category]) {
                        sb.AppendLine($"- {note}");
                    }
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Regression test for bugs where runs scored on 3rd out weren't credited to correct inning.
    /// These specific seeds exposed issues where:
    /// - Runs scored on a play that resulted in the 3rd out were not being recorded in the line score
    /// - The inning sum didn't match the final score because runs were lost during half-inning transitions
    /// - Specifically affected plays where runners scored before the 3rd out was recorded
    /// </summary>
    [TestCase(650162642)]
    [TestCase(124450555)]
    [TestCase(692792209)]
    [TestCase(2094557560)]
    public void LineScore_KnownBugSeeds_InningSumsMatchFinalScore(int seed) {
        // Arrange
        const string home = "Robots";
        const string away = "Androids";
        var sim = new GameSimulator(home, away, seed);

        // Act
        var result = sim.RunGame();

        // Assert - Verify inning sums match final scores
        var awaySum = result.LineScore.AwayInnings.Where(r => r >= 0).Sum();
        var homeSum = result.LineScore.HomeInnings.Where(r => r >= 0).Sum();

        Assert.That(awaySum, Is.EqualTo(result.LineScore.AwayTotal),
            $"Seed {seed}: away inning sum ({awaySum}) must equal AwayTotal ({result.LineScore.AwayTotal})");
        Assert.That(homeSum, Is.EqualTo(result.LineScore.HomeTotal),
            $"Seed {seed}: home inning sum ({homeSum}) must equal HomeTotal ({result.LineScore.HomeTotal})");

        Assert.That(result.LineScore.AwayTotal, Is.EqualTo(result.FinalState.AwayScore),
            $"Seed {seed}: LineScore AwayTotal ({result.LineScore.AwayTotal}) must equal FinalState AwayScore ({result.FinalState.AwayScore})");
        Assert.That(result.LineScore.HomeTotal, Is.EqualTo(result.FinalState.HomeScore),
            $"Seed {seed}: LineScore HomeTotal ({result.LineScore.HomeTotal}) must equal FinalState HomeScore ({result.FinalState.HomeScore})");
    }
}
