using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace DiamondSim.Tests.Scoring {
    [TestFixture]
    [Category("Scoring")]
    public class InningSummaryRegressionTests {
        /// <summary>
        /// Unit repro: 2 outs, R2+R3. Single plates both, batter is thrown out stretching for the 3rd out.
        /// Expect the two runs to land in the inning cell before the half flips.
        /// </summary>
        [Test]
        public void ThirdOutPlay_ThatScores_RunsAreCreditedToInningBeforeTransition() {
            // Arrange
            var scorekeeper = new DiamondSim.InningScorekeeper();

            var state = new DiamondSim.GameState(0, 0) {
                Inning = 1,
                Half = DiamondSim.InningHalf.Top,
                Outs = 2,
                OnFirst = false,
                OnSecond = true,
                OnThird = true,
                AwayScore = 0,
                HomeScore = 0,
                Offense = DiamondSim.Team.Away,
                Defense = DiamondSim.Team.Home
            };

            // Batter singles: both runners score; batter out at 2B for the 3rd out.
            var resolution = new DiamondSim.PaResolution(
                OutsAdded: 1,
                RunsScored: 2,
                NewBases: new DiamondSim.BaseState(false, false, false),
                Type: DiamondSim.PaType.Single,
                Tag: DiamondSim.OutcomeTag.Single
            );

            // Act
            var result = scorekeeper.ApplyPlateAppearance(state, resolution);
            var newState = result.StateAfter;

            // Assert – team score
            Assert.That(newState.AwayScore, Is.EqualTo(2));
            Assert.That(newState.HomeScore, Is.EqualTo(0));

            // Assert – half flipped to Bot 1st with 0 outs
            Assert.That(newState.Half, Is.EqualTo(DiamondSim.InningHalf.Bottom));
            Assert.That(newState.Inning, Is.EqualTo(1));
            Assert.That(newState.Outs, Is.EqualTo(0));

            // Assert – line score captured the 2 runs in Away 1st
            Assert.That(scorekeeper.LineScore.GetInningRuns(DiamondSim.Team.Away, 1), Is.EqualTo(2));
            Assert.That(scorekeeper.LineScore.AwayTotal, Is.EqualTo(newState.AwayScore));
            Assert.That(scorekeeper.LineScore.HomeTotal, Is.EqualTo(newState.HomeScore));
        }

        /// <summary>
        /// E2E guard: the four known-problem seeds must have inning sums == R in the printed line score.
        /// </summary>
        [TestCase(650162642)]
        [TestCase(124450555)]
        [TestCase(692792209)]
        [TestCase(2094557560)]
        public void Report_InningSums_Match_FinalScore_ForKnownSeeds(int seed) {
            // Arrange: pass (home, away) so away row prints first in the line score
            const string home = "Robots";
            const string away = "Androids";
            var sim = new DiamondSim.GameSimulator(home, away, seed);

            // Act
            string report = sim.RunGame();

            // Assert
            var parsed = ParseLineScoreFromReport(report, home, away);

            int awaySum = parsed.AwayInnings.Sum();
            int homeSum = parsed.HomeInnings.Sum();

            Assert.That(awaySum, Is.EqualTo(parsed.AwayR),
                $"Seed {seed}: away inning sums ({awaySum}) must equal R ({parsed.AwayR}).");
            Assert.That(homeSum, Is.EqualTo(parsed.HomeR),
                $"Seed {seed}: home inning sums ({homeSum}) must equal R ({parsed.HomeR}).");
        }

        // ---------------- helpers ----------------

        private static LineScoreSnapshot ParseLineScoreFromReport(string report, string homeName, string awayName) {
            // We’re parsing the table produced by GameReportFormatter.FormatLineScore():
            //
            //                | 1 2 3 4 5 6 7 8 9 |  R  H  E
            // ---------------|-------------------|---------
            // <awayName>     | x x x x x x x x x |  R  H  E
            // <homeName>     | x x x x x x x x x |  R  H  E
            //
            // Where cells can be digits or 'X' (skipped bottom 9th).

            var lines = report.Split('\n')
                              .Select(l => l.TrimEnd('\r'))
                              .ToList();

            // Find the header ruler line, then the two team rows right after it
            int sepIdx = lines.FindIndex(l => l.StartsWith("---------------|"));
            Assert.That(sepIdx, Is.GreaterThanOrEqualTo(0), "Could not find line score separator.");

            // Away row should start with awayName + '|'
            var awayLine = lines.Skip(sepIdx + 1).First(l => l.StartsWith(awayName));
            var homeLine = lines.Skip(sepIdx + 2).First(l => l.StartsWith(homeName));

            static (int[] innings, int R) ParseRow(string row) {
                // Split on '|' into [team, innings, tail]
                var parts = row.Split('|');
                Assert.That(parts.Length, Is.GreaterThanOrEqualTo(3), $"Unexpected line score row: '{row}'");

                var inningsCells = Regex.Split(parts[1].Trim(), @"\s+")
                                        .Where(s => !string.IsNullOrWhiteSpace(s))
                                        .ToArray();

                // Last chunk has " R  H  E" aligned; we only need R
                var tailCells = Regex.Split(parts[2].Trim(), @"\s+")
                                     .Where(s => !string.IsNullOrWhiteSpace(s))
                                     .ToArray();

                Assert.That(tailCells.Length, Is.GreaterThanOrEqualTo(3), $"Missing R/H/E in: '{row}'");

                int r = int.Parse(tailCells[0]); // first is R
                var innings = inningsCells.Select(c => c == "X" ? 0 : int.Parse(c)).ToArray();
                return (innings, r);
            }

            var (awayInnings, awayR) = ParseRow(awayLine);
            var (homeInnings, homeR) = ParseRow(homeLine);

            return new LineScoreSnapshot(awayInnings, homeInnings, awayR, homeR);
        }

        private sealed record LineScoreSnapshot(int[] AwayInnings, int[] HomeInnings, int AwayR, int HomeR);
    }
}
