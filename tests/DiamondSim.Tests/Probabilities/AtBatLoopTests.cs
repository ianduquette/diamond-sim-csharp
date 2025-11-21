using DiamondSim.Tests.TestHelpers;

namespace DiamondSim.Tests.Probabilities;

/// <summary>
/// Tests for the complete at-bat simulation loop, validating that K%, BB%, and BIP%
/// distributions fall within expected ranges for various matchups.
/// </summary>
[TestFixture]
public class AtBatLoopTests {
    private const int Seed = 12345;
    private const int HighRating = 70;
    private const int LowRating = 30;

    /// <summary>
    /// Tests that average vs. average matchups produce realistic outcome distributions.
    /// Expected ranges based on typical MLB statistics:
    /// - K% (Strikeout): 18-28%
    /// - BB% (Walk): 6-12%
    /// - BIP% (Ball In Play): 55-70%
    /// </summary>
    [Test]
    public void AverageVsAverage_ProducesRealisticDistributions() {
        // Arrange
        var pitcher = PitcherRatings.Average;
        var batter = BatterRatings.Average;

        // Act
        var distribution = ExecuteSut(pitcher, batter);

        // Assert
        distribution.AssertTotalPercentageIsOne();

        Assert.That(distribution.KRate, Is.InRange(0.18, 0.28),
            $"K% should be 18-28%, got {distribution.KRate:P1}");

        Assert.That(distribution.BbRate, Is.InRange(0.06, 0.12),
            $"BB% should be 6-12%, got {distribution.BbRate:P1}");

        Assert.That(distribution.BipRate, Is.InRange(0.55, 0.70),
            $"BIP% should be 55-70%, got {distribution.BipRate:P1}");

        // Output for debugging
        TestContext.Out.WriteLine($"Distribution for Average vs. Average:");
        TestContext.Out.WriteLine($"  Strikeouts: {distribution.Strikeouts,5} ({distribution.KRate:P2})");
        TestContext.Out.WriteLine($"  Walks:      {distribution.Walks,5} ({distribution.BbRate:P2})");
        TestContext.Out.WriteLine($"  BIP:        {distribution.BallsInPlay,5} ({distribution.BipRate:P2})");
        TestContext.Out.WriteLine($"  HBP:        {distribution.HitByPitch,5} ({distribution.HbpRate:P2})");
    }

    /// <summary>
    /// Tests that high Control pitchers produce fewer walks compared to average.
    /// </summary>
    [Test]
    public void HighControlPitcher_ProducesFewerWalks() {
        // Arrange
        var avgPitcher = PitcherRatings.Average;
        var batter = BatterRatings.Average;
        var avgDistribution = ExecuteSut(avgPitcher, batter);

        // Act
        var highControlPitcher = new PitcherRatings(Control: HighRating, Stuff: 50, Stamina: 50, Speed: 50);
        var highDistribution = ExecuteSut(highControlPitcher, batter);

        // Assert
        Assert.That(highDistribution.BbRate, Is.LessThan(avgDistribution.BbRate),
            $"High Control pitcher BB% ({highDistribution.BbRate:P2}) should be less than average ({avgDistribution.BbRate:P2})");

        Assert.That(highDistribution.BbRate, Is.LessThanOrEqualTo(0.10),
            $"High Control pitcher should have BB% ≤ 10%, got {highDistribution.BbRate:P1}");

        // Output for debugging
        TestContext.Out.WriteLine($"Average Control: BB={avgDistribution.BbRate:P2}");
        TestContext.Out.WriteLine($"High Control:    BB={highDistribution.BbRate:P2}");
    }

    /// <summary>
    /// Tests that high Patience batters produce more walks compared to average.
    /// </summary>
    [Test]
    public void HighPatienceBatter_ProducesMoreWalks() {
        // Arrange
        var pitcher = PitcherRatings.Average;
        var avgBatter = BatterRatings.Average;
        var avgDistribution = ExecuteSut(pitcher, avgBatter);

        // Act
        var highPatienceBatter = new BatterRatings(Contact: 50, Power: 50, Patience: HighRating, Speed: 50);
        var highDistribution = ExecuteSut(pitcher, highPatienceBatter);

        // Assert
        Assert.That(highDistribution.BbRate, Is.GreaterThan(avgDistribution.BbRate),
            $"High Patience batter BB% ({highDistribution.BbRate:P2}) should be greater than average ({avgDistribution.BbRate:P2})");

        Assert.That(highDistribution.BbRate, Is.GreaterThanOrEqualTo(0.09),
            $"High Patience batter should have BB% ≥ 9%, got {highDistribution.BbRate:P1}");

        // Output for debugging
        TestContext.Out.WriteLine($"Average Patience: BB={avgDistribution.BbRate:P2}");
        TestContext.Out.WriteLine($"High Patience:    BB={highDistribution.BbRate:P2}");
    }

    /// <summary>
    /// Tests that high Patience batters produce fewer strikeouts compared to average.
    /// </summary>
    [Test]
    public void HighPatienceBatter_ProducesFewerStrikeouts() {
        // Arrange
        var pitcher = PitcherRatings.Average;
        var avgBatter = BatterRatings.Average;
        var avgDistribution = ExecuteSut(pitcher, avgBatter);

        // Act
        var highPatienceBatter = new BatterRatings(Contact: 50, Power: 50, Patience: HighRating, Speed: 50);
        var highDistribution = ExecuteSut(pitcher, highPatienceBatter);

        // Assert
        Assert.That(highDistribution.KRate, Is.LessThan(avgDistribution.KRate),
            $"High Patience batter K% ({highDistribution.KRate:P2}) should be less than average ({avgDistribution.KRate:P2})");

        Assert.That(highDistribution.KRate, Is.LessThanOrEqualTo(0.25),
            $"High Patience batter should have K% ≤ 25%, got {highDistribution.KRate:P1}");

        // Output for debugging
        TestContext.Out.WriteLine($"Average Patience: K={avgDistribution.KRate:P2}");
        TestContext.Out.WriteLine($"High Patience:    K={highDistribution.KRate:P2}");
    }

    /// <summary>
    /// Tests that low Contact batters produce more strikeouts compared to average.
    /// </summary>
    [Test]
    public void LowContactBatter_ProducesMoreStrikeouts() {
        // Arrange
        var pitcher = PitcherRatings.Average;
        var avgBatter = BatterRatings.Average;
        var avgDistribution = ExecuteSut(pitcher, avgBatter);

        // Act
        var lowContactBatter = new BatterRatings(Contact: LowRating, Power: 50, Patience: 50, Speed: 50);
        var lowDistribution = ExecuteSut(pitcher, lowContactBatter);

        // Assert
        Assert.That(lowDistribution.KRate, Is.GreaterThan(avgDistribution.KRate),
            $"Low Contact batter K% ({lowDistribution.KRate:P2}) should be greater than average ({avgDistribution.KRate:P2})");

        Assert.That(lowDistribution.KRate, Is.GreaterThanOrEqualTo(0.25),
            $"Low Contact batter should have K% ≥ 25%, got {lowDistribution.KRate:P1}");

        // Output for debugging
        TestContext.Out.WriteLine($"Average Contact: K={avgDistribution.KRate:P2}");
        TestContext.Out.WriteLine($"Low Contact:     K={lowDistribution.KRate:P2}");
    }

    /// <summary>
    /// Tests that all at-bats reach a terminal outcome (no infinite loops).
    /// Validates that pitch counts are reasonable.
    /// </summary>
    [Test]
    public void AllAtBats_ReachTerminalOutcome() {
        // Arrange
        var pitcher = PitcherRatings.Average;
        var batter = BatterRatings.Average;

        // Act
        var distribution = ExecuteSut(pitcher, batter);

        // Assert
        distribution.AssertAllOutcomesValid();
        distribution.AssertAllPitchCountsReasonable();

        TestContext.Out.WriteLine($"All {distribution.Trials} at-bats reached terminal outcomes");
        TestContext.Out.WriteLine($"Max pitch count: {distribution.MaxPitchCount}");
    }

    /// <summary>
    /// Tests that foul balls with 2 strikes don't create a third strike.
    /// Verifies that some at-bats reach high pitch counts (indicating foul balls at 2 strikes),
    /// and that these eventually terminate properly.
    /// </summary>
    [Test]
    public void FoulBallsWithTwoStrikes_DoNotCreateThirdStrike() {
        // Arrange
        var pitcher = PitcherRatings.Average;
        var batter = BatterRatings.Average;

        // Act
        var distribution = ExecuteSut(pitcher, batter);

        // Assert
        distribution.AssertAllOutcomesValid();

        Assert.That(distribution.HighPitchCountAtBats, Is.GreaterThan(0),
            "Should have some at-bats with >10 pitches (foul balls at 2 strikes)");

        // Output for debugging
        TestContext.Out.WriteLine($"At-bats with >10 pitches: {distribution.HighPitchCountAtBats}/{distribution.Trials}");
        TestContext.Out.WriteLine($"Maximum pitch count observed: {distribution.MaxPitchCount}");
    }

    /// <summary>
    /// Tests that the AtBatResult contains accurate information for strikeouts.
    /// </summary>
    [Test]
    public void AtBatResult_Strikeout_HasCorrectFinalCount() {
        // Arrange
        var pitcher = PitcherRatings.Average;
        var batter = BatterRatings.Average;

        // Act
        var distribution = ExecuteSut(pitcher, batter);

        // Assert
        Assert.That(distribution.Strikeouts, Is.GreaterThan(0),
            "Should have at least one strikeout to validate");

        distribution.AssertStrikeoutCountsValid();

        TestContext.Out.WriteLine($"Validated {distribution.Strikeouts} strikeouts with correct final counts");
    }

    /// <summary>
    /// Tests that the AtBatResult contains accurate information for walks.
    /// </summary>
    [Test]
    public void AtBatResult_Walk_HasCorrectFinalCount() {
        // Arrange
        var pitcher = PitcherRatings.Average;
        var batter = BatterRatings.Average;

        // Act
        var distribution = ExecuteSut(pitcher, batter);

        // Assert
        Assert.That(distribution.Walks, Is.GreaterThan(0),
            "Should have at least one walk to validate");

        distribution.AssertWalkCountsValid();

        TestContext.Out.WriteLine($"Validated {distribution.Walks} walks with correct final counts");
    }

    /// <summary>
    /// Tests that HBP rate is realistic (~0.8-2.0% per PA).
    /// </summary>
    [Test]
    public void HitByPitch_RateIsRealisticPerPA() {
        // Arrange
        var pitcher = PitcherRatings.Average;
        var batter = BatterRatings.Average;

        // Act
        var distribution = ExecuteSut(pitcher, batter);

        // Assert
        Assert.That(distribution.HbpRate, Is.InRange(0.008, 0.020),
            $"HBP rate should be 0.8-2.0%, got {distribution.HbpRate:P2}");

        // Output for debugging
        TestContext.Out.WriteLine($"HBP: {distribution.HitByPitch}/{distribution.Trials} = {distribution.HbpRate:P2}");
    }

    /// <summary>
    /// Executes the System Under Test (SUT) - simulates at-bats and returns distribution.
    /// </summary>
    private static AtBatDistribution ExecuteSut(PitcherRatings pitcher, BatterRatings batter, int? seed = null) {
        var random = new SeededRandom(seed ?? Seed);
        var simulator = new AtBatSimulator(random);
        var trials = TestConfig.SIM_DEFAULT_N;
        var distribution = new AtBatDistribution(trials);

        for (int i = 0; i < trials; i++) {
            var result = simulator.SimulateAtBat(pitcher, batter);
            distribution.RecordOutcome(result);
        }

        return distribution;
    }

    /// <summary>
    /// Helper class to hold at-bat distribution results and validation logic.
    /// </summary>
    private class AtBatDistribution {
        public int Strikeouts { get; private set; }
        public int Walks { get; private set; }
        public int BallsInPlay { get; private set; }
        public int HitByPitch { get; private set; }
        public int HighPitchCountAtBats { get; private set; }
        public int MaxPitchCount { get; private set; }
        public int Trials { get; }

        private readonly List<AtBatResult> _results = new();

        public AtBatDistribution(int trials) {
            Trials = trials;
        }

        /// <summary>
        /// Records an at-bat outcome, updating internal counts.
        /// </summary>
        public void RecordOutcome(AtBatResult result) {
            _results.Add(result);

            switch (result.Terminal) {
                case AtBatTerminal.Strikeout:
                    Strikeouts++;
                    break;
                case AtBatTerminal.Walk:
                    Walks++;
                    break;
                case AtBatTerminal.BallInPlay:
                    BallsInPlay++;
                    break;
                case AtBatTerminal.HitByPitch:
                    HitByPitch++;
                    break;
            }

            if (result.PitchCount > 10) {
                HighPitchCountAtBats++;
            }

            if (result.PitchCount > MaxPitchCount) {
                MaxPitchCount = result.PitchCount;
            }
        }

        public double KRate => (double)Strikeouts / Trials;
        public double BbRate => (double)Walks / Trials;
        public double BipRate => (double)BallsInPlay / Trials;
        public double HbpRate => (double)HitByPitch / Trials;

        /// <summary>
        /// Sum of all outcome percentages (should equal 1.0).
        /// </summary>
        private double TotalPct => KRate + BbRate + BipRate + HbpRate;

        /// <summary>
        /// Asserts that the total percentage equals 1.0 (100%).
        /// </summary>
        public void AssertTotalPercentageIsOne() {
            Assert.That(TotalPct, Is.EqualTo(1.0).Within(0.0001),
                $"K% + BB% + BIP% + HBP% should equal 100%, got {TotalPct:P1}");
        }

        /// <summary>
        /// Asserts that all outcomes are valid terminal states.
        /// </summary>
        public void AssertAllOutcomesValid() {
            foreach (var result in _results) {
                Assert.That(result.Terminal, Is.AnyOf(
                    AtBatTerminal.Strikeout,
                    AtBatTerminal.Walk,
                    AtBatTerminal.BallInPlay,
                    AtBatTerminal.HitByPitch
                ), $"Invalid terminal outcome: {result.Terminal}");
            }
        }

        /// <summary>
        /// Asserts that all pitch counts are reasonable (not stuck in infinite loop).
        /// </summary>
        public void AssertAllPitchCountsReasonable() {
            foreach (var result in _results) {
                Assert.That(result.PitchCount, Is.GreaterThan(0).And.LessThan(50),
                    $"Pitch count should be reasonable, got {result.PitchCount}");
            }
        }

        /// <summary>
        /// Asserts that all strikeouts have correct final counts (ending with 3 strikes).
        /// </summary>
        public void AssertStrikeoutCountsValid() {
            var strikeouts = _results.Where(r => r.Terminal == AtBatTerminal.Strikeout);
            foreach (var result in strikeouts) {
                Assert.That(result.FinalCount, Does.EndWith("-3"),
                    $"Strikeout should end with 3 strikes, got {result.FinalCount}");
            }
        }

        /// <summary>
        /// Asserts that all walks have correct final counts (starting with 4 balls).
        /// </summary>
        public void AssertWalkCountsValid() {
            var walks = _results.Where(r => r.Terminal == AtBatTerminal.Walk);
            foreach (var result in walks) {
                Assert.That(result.FinalCount, Does.StartWith("4-"),
                    $"Walk should start with 4 balls, got {result.FinalCount}");
            }
        }
    }
}
