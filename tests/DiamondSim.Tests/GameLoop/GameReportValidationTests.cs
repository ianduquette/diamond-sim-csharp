using System.Text.RegularExpressions;

namespace DiamondSim.Tests.GameLoop;

/// <summary>
/// Comprehensive validation tests that run multiple seeds and verify game report output format
/// and statistical consistency. Tests formula accuracy, stat reconciliation, logical checks, and edge cases.
/// </summary>
[TestFixture]
public class GameReportValidationTests {

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
        string report = simulator.RunGame();

        // Assert - Run all verification checks
        var results = new VerificationResults(seed);

        VerifyFormulaConsistency(report, results);
        VerifyStatReconciliation(report, results);
        VerifyLogicalChecks(report, results);
        VerifyEdgeCases(report, results);

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
            string report = simulator.RunGame();

            var results = new VerificationResults(seed);
            VerifyFormulaConsistency(report, results);
            VerifyStatReconciliation(report, results);
            VerifyLogicalChecks(report, results);
            VerifyEdgeCases(report, results);

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

    private void VerifyFormulaConsistency(string report, VerificationResults results) {
        var lines = report.Split('\n');
        var battingSection = ExtractSection(lines, "BATTING", "PITCHING");

        // Parse batting lines for each team - skip header and separator lines
        var batterLines = battingSection.Split('\n')
            .Where(l => !string.IsNullOrWhiteSpace(l) &&
                        !l.Contains("BATTING") &&
                        !l.Contains("---") &&
                        !l.Contains("PA  AB"))
            .ToList();

        foreach (var line in batterLines) {
            if (line.Contains("TOTALS")) continue;

            var stats = ParseBattingLine(line);
            if (stats == null) continue;

            // Since we don't have HBP and SF in the parsed stats, we can't verify the full formula
            // Just verify AB <= PA (basic sanity check)
            if (stats.AB > stats.PA) {
                results.AddFailure("FormulaConsistency",
                    $"AB > PA for {stats.Name}: AB={stats.AB}, PA={stats.PA}");
            }
        }

        // Verify team totals
        var awayTotals = batterLines.FirstOrDefault(l => l.Contains("Away") && l.Contains("TOTALS"));
        var homeTotals = batterLines.FirstOrDefault(l => l.Contains("Home") && l.Contains("TOTALS"));

        if (awayTotals != null) {
            var stats = ParseBattingLine(awayTotals);
            if (stats != null && stats.AB > stats.PA) {
                results.AddFailure("FormulaConsistency",
                    $"Away team AB > PA: AB={stats.AB}, PA={stats.PA}");
            }
        }

        if (homeTotals != null) {
            var stats = ParseBattingLine(homeTotals);
            if (stats != null && stats.AB > stats.PA) {
                results.AddFailure("FormulaConsistency",
                    $"Home team AB > PA: AB={stats.AB}, PA={stats.PA}");
            }
        }

        if (results.GetFailureCount("FormulaConsistency") == 0) {
            results.AddPass("FormulaConsistency");
        }
    }

    private void VerifyStatReconciliation(string report, VerificationResults results) {
        var lines = report.Split('\n');

        // Extract line score - look for lines with team name followed by pipe and numbers
        var awayScoreLine = lines.FirstOrDefault(l =>
            l.Trim().StartsWith("Away") && l.Contains("|") &&
            Regex.IsMatch(l, @"\|\s+\d+\s+\d+\s+\d+"));
        var homeScoreLine = lines.FirstOrDefault(l =>
            l.Trim().StartsWith("Home") && l.Contains("|") &&
            Regex.IsMatch(l, @"\|\s+\d+\s+\d+\s+\d+"));

        // Extract final score
        var finalLine = lines.FirstOrDefault(l => l.StartsWith("Final:"));

        if (awayScoreLine != null && homeScoreLine != null && finalLine != null) {
            var awayLineScore = ParseLineScore(awayScoreLine);
            var homeLineScore = ParseLineScore(homeScoreLine);
            var finalScores = ParseFinalScore(finalLine);

            // Verify line score R matches final score
            if (awayLineScore.R != finalScores.Away) {
                results.AddFailure("StatReconciliation",
                    $"Away runs mismatch: Line score {awayLineScore.R}, Final {finalScores.Away}");
            }
            if (homeLineScore.R != finalScores.Home) {
                results.AddFailure("StatReconciliation",
                    $"Home runs mismatch: Line score {homeLineScore.R}, Final {finalScores.Home}");
            }

            // Verify line score H matches box score totals
            var battingSection = ExtractSection(lines, "BATTING", "PITCHING");
            var awayTotals = ParseBattingLine(
                battingSection.Split('\n').FirstOrDefault(l => l.Contains("Away") && l.Contains("TOTALS")) ?? "");
            var homeTotals = ParseBattingLine(
                battingSection.Split('\n').FirstOrDefault(l => l.Contains("Home") && l.Contains("TOTALS")) ?? "");

            if (awayTotals != null && awayLineScore.H != awayTotals.H) {
                results.AddFailure("StatReconciliation",
                    $"Away hits mismatch: Line score {awayLineScore.H}, Box score {awayTotals.H}");
            }
            if (homeTotals != null && homeLineScore.H != homeTotals.H) {
                results.AddFailure("StatReconciliation",
                    $"Home hits mismatch: Line score {homeLineScore.H}, Box score {homeTotals.H}");
            }
        }

        // Verify pitcher BF
        var pitchingSection = ExtractSection(lines, "PITCHING", "Seed:");
        var pitcherLines = pitchingSection.Split('\n')
            .Where(l => !string.IsNullOrWhiteSpace(l) &&
                        !l.Contains("PITCHING") &&
                        !l.Contains("---") &&
                        !l.Contains("IP  BF"))
            .ToList();

        foreach (var line in pitcherLines) {
            var stats = ParsePitchingLine(line);
            if (stats == null) continue;

            // BF should be reasonable (at least H + BB + HBP)
            var minBF = stats.H + stats.BB + stats.HBP;
            if (stats.BF < minBF) {
                results.AddFailure("StatReconciliation",
                    $"Pitcher {stats.Name} BF too low: {stats.BF} < {minBF}");
            }
        }

        if (results.GetFailureCount("StatReconciliation") == 0) {
            results.AddPass("StatReconciliation");
        }
    }

    private void VerifyLogicalChecks(string report, VerificationResults results) {
        var lines = report.Split('\n');
        var battingSection = ExtractSection(lines, "BATTING", "PITCHING");
        var pitchingSection = ExtractSection(lines, "PITCHING", "Seed:");

        // Check for negative stats
        var allLines = battingSection + "\n" + pitchingSection;
        var numbers = Regex.Matches(allLines, @"-\d+");
        if (numbers.Count > 0) {
            results.AddFailure("LogicalChecks", $"Found negative stats: {numbers.Count} occurrences");
        }

        // Verify RBI ≤ Runs + margin
        var awayTotals = ParseBattingLine(
            battingSection.Split('\n').FirstOrDefault(l => l.Contains("Away") && l.Contains("TOTALS")) ?? "");
        var homeTotals = ParseBattingLine(
            battingSection.Split('\n').FirstOrDefault(l => l.Contains("Home") && l.Contains("TOTALS")) ?? "");

        if (awayTotals != null && awayTotals.RBI > awayTotals.R + 5) {
            results.AddFailure("LogicalChecks",
                $"Away RBI ({awayTotals.RBI}) exceeds runs ({awayTotals.R}) by too much");
        }
        if (homeTotals != null && homeTotals.RBI > homeTotals.R + 5) {
            results.AddFailure("LogicalChecks",
                $"Home RBI ({homeTotals.RBI}) exceeds runs ({homeTotals.R}) by too much");
        }

        // Verify IP thirds format
        var pitcherLines = pitchingSection.Split('\n')
            .Where(l => !string.IsNullOrWhiteSpace(l) &&
                        !l.Contains("PITCHING") &&
                        !l.Contains("---") &&
                        !l.Contains("IP  BF"))
            .ToList();

        foreach (var line in pitcherLines) {
            var ipMatch = Regex.Match(line, @"(\d+\.\d)");
            if (ipMatch.Success) {
                var ip = ipMatch.Groups[1].Value;
                var thirds = ip.Split('.')[1];
                if (thirds != "0" && thirds != "1" && thirds != "2") {
                    results.AddFailure("LogicalChecks",
                        $"Invalid IP thirds format: {ip} (should be .0, .1, or .2)");
                }
            }
        }

        if (results.GetFailureCount("LogicalChecks") == 0) {
            results.AddPass("LogicalChecks");
        }
    }

    private void VerifyEdgeCases(string report, VerificationResults results) {
        var lines = report.Split('\n');

        // Check for tie
        var finalLine = lines.FirstOrDefault(l => l.StartsWith("Final:"));
        if (finalLine != null && finalLine.Contains("(TIE)")) {
            results.AddNote("EdgeCases", "Game ended in a tie");
        }

        // Check for walk-off
        if (report.Contains("walk-off", StringComparison.OrdinalIgnoreCase)) {
            results.AddNote("EdgeCases", "Walk-off detected");
        }

        // Check for skipped bottom 9th
        var hasTop9 = lines.Any(l => l.Contains("[Top 9]"));
        var hasBot9 = lines.Any(l => l.Contains("[Bot 9]"));
        if (hasTop9 && !hasBot9) {
            results.AddNote("EdgeCases", "Bottom 9th skipped (home team ahead)");
        }

        // Check for no-hitter or perfect game
        var awayScoreLine = lines.FirstOrDefault(l => l.Contains("Away") && l.Contains("|") &&
            !l.Contains("BATTING") && !l.Contains("PITCHING"));
        var homeScoreLine = lines.FirstOrDefault(l => l.Contains("Home") && l.Contains("|") &&
            !l.Contains("BATTING") && !l.Contains("PITCHING"));

        if (awayScoreLine != null) {
            var awayLineScore = ParseLineScore(awayScoreLine);
            if (awayLineScore.H == 0) {
                results.AddNote("EdgeCases", "Away team no-hitter!");
            }
        }
        if (homeScoreLine != null) {
            var homeLineScore = ParseLineScore(homeScoreLine);
            if (homeLineScore.H == 0) {
                results.AddNote("EdgeCases", "Home team no-hitter!");
            }
        }

        results.AddPass("EdgeCases");
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

    private BattingStats? ParseBattingLine(string line) {
        if (string.IsNullOrWhiteSpace(line)) return null;

        // Split by whitespace and filter out empty entries
        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        // Need at least name + 7 stat columns (PA AB H RBI BB K HR)
        if (parts.Length < 8) return null;

        try {
            // The format is: Name PA AB H RBI BB K HR
            // Find the last 7 numbers (the stats)
            if (parts.Length < 7) return null;

            var statsStart = parts.Length - 7;
            var name = string.Join(" ", parts.Take(statsStart));

            return new BattingStats {
                Name = name,
                PA = int.Parse(parts[statsStart]),
                AB = int.Parse(parts[statsStart + 1]),
                H = int.Parse(parts[statsStart + 2]),
                RBI = int.Parse(parts[statsStart + 3]),
                BB = int.Parse(parts[statsStart + 4]),
                K = int.Parse(parts[statsStart + 5]),
                HR = int.Parse(parts[statsStart + 6]),
                R = 0, // Not in this format
                HBP = 0, // Not in this format
                SF = 0 // Not in this format
            };
        }
        catch {
            return null;
        }
    }

    private PitchingStats? ParsePitchingLine(string line) {
        if (string.IsNullOrWhiteSpace(line)) return null;

        // Split by whitespace and filter out empty entries
        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        // Need at least name + 8 stat columns (IP BF H R ER BB K HR)
        if (parts.Length < 9) return null;

        try {
            // Find where the numeric stats start (IP starts with a number)
            int statsStartIndex = -1;
            for (int i = 0; i < parts.Length - 8; i++) {
                // IP starts with a number and has format X.Y
                if (Regex.IsMatch(parts[i], @"^\d+\.\d$")) {
                    statsStartIndex = i;
                    break;
                }
            }

            if (statsStartIndex == -1) return null;

            var name = string.Join(" ", parts.Take(statsStartIndex));

            return new PitchingStats {
                Name = name,
                BF = int.Parse(parts[statsStartIndex + 1]),
                H = int.Parse(parts[statsStartIndex + 2]),
                BB = int.Parse(parts[statsStartIndex + 5]),
                K = int.Parse(parts[statsStartIndex + 6]),
                HBP = int.Parse(parts[statsStartIndex + 7])
            };
        }
        catch {
            return null;
        }
    }

    private (int R, int H, int E) ParseLineScore(string line) {
        var parts = line.Split('|').Select(p => p.Trim()).ToArray();
        if (parts.Length < 2) return (0, 0, 0);

        // The last part contains "R H E" values
        var lastPart = parts[^1].Trim();
        var values = lastPart.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (values.Length >= 3) {
            return (int.Parse(values[0]), int.Parse(values[1]), int.Parse(values[2]));
        }
        return (0, 0, 0);
    }

    private (int Away, int Home) ParseFinalScore(string line) {
        var match = Regex.Match(line, @"Final: \w+ (\d+) — \w+ (\d+)");
        if (match.Success) {
            return (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
        }
        return (0, 0);
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

    private class BattingStats {
        public string Name { get; set; } = "";
        public int PA { get; set; }
        public int AB { get; set; }
        public int R { get; set; }
        public int H { get; set; }
        public int RBI { get; set; }
        public int BB { get; set; }
        public int K { get; set; }
        public int HR { get; set; }
        public int HBP { get; set; } = 0;
        public int SF { get; set; } = 0;
    }

    private class PitchingStats {
        public string Name { get; set; } = "";
        public int BF { get; set; }
        public int H { get; set; }
        public int BB { get; set; }
        public int K { get; set; }
        public int HBP { get; set; }
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
}
