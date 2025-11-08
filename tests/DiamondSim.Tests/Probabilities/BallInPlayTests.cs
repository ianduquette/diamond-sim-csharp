using DiamondSim.Tests.TestHelpers;

namespace DiamondSim.Tests.Probabilities;

/// <summary>
/// Tests for ball-in-play outcome resolution and distribution validation.
/// Validates that BallInPlayResolver produces realistic MLB-like distributions.
/// </summary>
[TestFixture]
public class BallInPlayTests {
    private const int Seed = 12345;
    private const int AveragePower = 50;
    private const int AverageStuff = 50;
    private const int HighPower = 80;
    private const int HighStuff = 80;
    private const int LowPower = 20;
    private const int LowStuff = 20;

    /// <summary>
    /// Validates that average vs. average matchup produces realistic distributions.
    /// Expected ranges based on typical MLB statistics (see TestConfig for band definitions).
    /// </summary>
    [Test]
    public void AverageVsAverage_ProducesRealisticDistributions() {
        // Act
        var distribution = ExecuteSut(AveragePower, AverageStuff);

        // Assert
        distribution.AssertTotalPercentageIsOne();
        distribution.AssertAllPercentagesNonNegative();

        Assert.That(distribution.HrPct, Is.InRange(TestConfig.MlbBipHrMin, TestConfig.MlbBipHrMax),
          $"HR% should be {TestConfig.MlbBipHrMin:P1}-{TestConfig.MlbBipHrMax:P1}, got {distribution.HrPct:P2}");

        Assert.That(distribution.DoublePct, Is.InRange(TestConfig.MlbBipDoubleMin, TestConfig.MlbBipDoubleMax),
           $"2B% should be {TestConfig.MlbBipDoubleMin:P1}-{TestConfig.MlbBipDoubleMax:P1} of BIP, got {distribution.DoublePct:P2}");

        Assert.That(distribution.TriplePct, Is.InRange(TestConfig.MlbBipTripleMin, TestConfig.MlbBipTripleMax),
            $"3B% should be {TestConfig.MlbBipTripleMin:P1}-{TestConfig.MlbBipTripleMax:P1} of BIP, got {distribution.TriplePct:P2}");

        Assert.That(distribution.SinglePct, Is.InRange(TestConfig.MlbBipSingleMin, TestConfig.MlbBipSingleMax),
            $"Singles% should be {TestConfig.MlbBipSingleMin:P1}-{TestConfig.MlbBipSingleMax:P1} of BIP, got {distribution.SinglePct:P2}");

        Assert.That(distribution.Babip, Is.InRange(TestConfig.MlbBabipMin, TestConfig.MlbBabipMax),
            $"BABIP should be {TestConfig.MlbBabipMin:F2}-{TestConfig.MlbBabipMax:F2}, got {distribution.Babip:F3}");

        // Output for debugging
        TestContext.Out.WriteLine($"Distribution for Average vs. Average (Power={AveragePower}, Stuff={AverageStuff}):");
        TestContext.Out.WriteLine($"  Outs:     {distribution.Outs,5} ({distribution.OutPct:P2})");
        TestContext.Out.WriteLine($"  Singles:  {distribution.Singles,5} ({distribution.SinglePct:P2})");
        TestContext.Out.WriteLine($"  Doubles:  {distribution.Doubles,5} ({distribution.DoublePct:P2})");
        TestContext.Out.WriteLine($"  Triples:  {distribution.Triples,5} ({distribution.TriplePct:P2})");
        TestContext.Out.WriteLine($"  HomeRuns: {distribution.HomeRuns,5} ({distribution.HrPct:P2})");
        TestContext.Out.WriteLine($"  BABIP:    {distribution.Babip:F3}");
    }

    /// <summary>
    /// Validates that high Power increases home runs compared to average.
    /// </summary>
    [Test]
    public void HighPower_IncreasesHomeRuns() {
        // Arrange
        var avgDistribution = ExecuteSut(AveragePower, AverageStuff);

        // Act
        var highDistribution = ExecuteSut(HighPower, AverageStuff);

        // Assert
        Assert.That(highDistribution.HrPct, Is.GreaterThan(avgDistribution.HrPct),
            $"High Power should increase HR% (avg: {avgDistribution.HrPct:P2}, high: {highDistribution.HrPct:P2})");

        // Output for debugging
        TestContext.Out.WriteLine($"Average Power (50): HR={avgDistribution.HrPct:P2}");
        TestContext.Out.WriteLine($"High Power (80):    HR={highDistribution.HrPct:P2}");
    }

    /// <summary>
    /// Validates that high Power increases doubles compared to average.
    /// </summary>
    [Test]
    public void HighPower_IncreasesDoubles() {
        // Arrange
        var avgDistribution = ExecuteSut(AveragePower, AverageStuff);

        // Act
        var highDistribution = ExecuteSut(HighPower, AverageStuff);

        // Assert
        Assert.That(highDistribution.DoublePct, Is.GreaterThan(avgDistribution.DoublePct),
            $"High Power should increase 2B% (avg: {avgDistribution.DoublePct:P2}, high: {highDistribution.DoublePct:P2})");

        // Output for debugging
        TestContext.Out.WriteLine($"Average Power (50): 2B={avgDistribution.DoublePct:P2}");
        TestContext.Out.WriteLine($"High Power (80):    2B={highDistribution.DoublePct:P2}");
    }

    /// <summary>
    /// Validates that high Power decreases outs compared to average.
    /// </summary>
    [Test]
    public void HighPower_DecreasesOuts() {
        // Arrange
        var avgDistribution = ExecuteSut(AveragePower, AverageStuff);

        // Act
        var highDistribution = ExecuteSut(HighPower, AverageStuff);

        // Assert
        Assert.That(highDistribution.OutPct, Is.LessThan(avgDistribution.OutPct),
            $"High Power should decrease Outs% (avg: {avgDistribution.OutPct:P2}, high: {highDistribution.OutPct:P2})");

        // Output for debugging
        TestContext.Out.WriteLine($"Average Power (50): Outs={avgDistribution.OutPct:P2}");
        TestContext.Out.WriteLine($"High Power (80):    Outs={highDistribution.OutPct:P2}");
    }

    /// <summary>
    /// Validates that high Stuff increases outs compared to average.
    /// </summary>
    [Test]
    public void HighStuff_IncreasesOuts() {
        // Arrange
        var avgDistribution = ExecuteSut(AveragePower, AverageStuff);

        // Act
        var highDistribution = ExecuteSut(AveragePower, HighStuff);

        // Assert
        Assert.That(highDistribution.OutPct, Is.GreaterThan(avgDistribution.OutPct),
            $"High Stuff should increase Outs% (avg: {avgDistribution.OutPct:P2}, high: {highDistribution.OutPct:P2})");

        // Output for debugging
        TestContext.Out.WriteLine($"Average Stuff (50): Outs={avgDistribution.OutPct:P2}");
        TestContext.Out.WriteLine($"High Stuff (80):    Outs={highDistribution.OutPct:P2}");
    }

    /// <summary>
    /// Validates that high Stuff lowers BABIP compared to average.
    /// </summary>
    [Test]
    public void HighStuff_LowersBabip() {
        // Arrange
        var avgDistribution = ExecuteSut(AveragePower, AverageStuff);

        // Act
        var highDistribution = ExecuteSut(AveragePower, HighStuff);

        // Assert
        Assert.That(highDistribution.Babip, Is.LessThan(avgDistribution.Babip),
            $"High Stuff should decrease BABIP (avg: {avgDistribution.Babip:F3}, high: {highDistribution.Babip:F3})");

        // Output for debugging
        TestContext.Out.WriteLine($"Average Stuff (50): BABIP={avgDistribution.Babip:F3}");
        TestContext.Out.WriteLine($"High Stuff (80):    BABIP={highDistribution.Babip:F3}");
    }

    /// <summary>
    /// Validates that low Power decreases home runs compared to average.
    /// </summary>
    [Test]
    public void LowPower_DecreasesHomeRuns() {
        // Arrange
        var avgDistribution = ExecuteSut(AveragePower, AverageStuff);

        // Act
        var lowDistribution = ExecuteSut(LowPower, AverageStuff);

        // Assert
        Assert.That(lowDistribution.HrPct, Is.LessThan(avgDistribution.HrPct),
            $"Low Power should decrease HR% (avg: {avgDistribution.HrPct:P2}, low: {lowDistribution.HrPct:P2})");

        // Output for debugging
        TestContext.Out.WriteLine($"Average Power (50): HR={avgDistribution.HrPct:P2}");
        TestContext.Out.WriteLine($"Low Power (20):     HR={lowDistribution.HrPct:P2}");
    }

    /// <summary>
    /// Validates that low Power increases singles as a percentage of all hits (including HRs).
    /// </summary>
    [Test]
    public void LowPower_IncreasesSinglesAsShareOfAllHits() {
        // Arrange
        var avgDistribution = ExecuteSut(AveragePower, AverageStuff);
        double avgSinglePctOfHits = avgDistribution.SinglePct / (1.0 - avgDistribution.OutPct);

        // Act
        var lowDistribution = ExecuteSut(LowPower, AverageStuff);
        double lowSinglePctOfHits = lowDistribution.SinglePct / (1.0 - lowDistribution.OutPct);

        // Assert
        Assert.That(lowSinglePctOfHits, Is.GreaterThan(avgSinglePctOfHits),
            $"Low Power should increase Singles% of all hits (avg: {avgSinglePctOfHits:P2}, low: {lowSinglePctOfHits:P2})");

        // Output for debugging
        TestContext.Out.WriteLine($"Average Power (50): 1B%ofAllHits={avgSinglePctOfHits:P2}");
        TestContext.Out.WriteLine($"Low Power (20):     1B%ofAllHits={lowSinglePctOfHits:P2}");
    }

    /// <summary>
    /// Validates that low Power increases singles as a percentage of contact hits (excluding HRs).
    /// </summary>
    [Test]
    public void LowPower_IncreasesSinglesAsShareOfContactHits() {
        // Arrange
        var avgDistribution = ExecuteSut(AveragePower, AverageStuff);
        double avgSinglesOfContactHits = avgDistribution.SinglePct / (1.0 - avgDistribution.OutPct - avgDistribution.HrPct);

        // Act
        var lowDistribution = ExecuteSut(LowPower, AverageStuff);
        double lowSinglesOfContactHits = lowDistribution.SinglePct / (1.0 - lowDistribution.OutPct - lowDistribution.HrPct);

        // Assert
        Assert.That(lowSinglesOfContactHits, Is.GreaterThan(avgSinglesOfContactHits),
            $"Low Power should increase Singles% of contact hits (avg: {avgSinglesOfContactHits:P2}, low: {lowSinglesOfContactHits:P2})");

        // Output for debugging
        TestContext.Out.WriteLine($"Average Power (50): 1B%ofContactHits={avgSinglesOfContactHits:P2}");
        TestContext.Out.WriteLine($"Low Power (20):     1B%ofContactHits={lowSinglesOfContactHits:P2}");
    }

    /// <summary>
    /// Validates that low Stuff decreases outs compared to average.
    /// </summary>
    [Test]
    public void LowStuff_DecreasesOuts() {
        // Arrange
        var avgDistribution = ExecuteSut(AveragePower, AverageStuff);

        // Act
        var lowDistribution = ExecuteSut(AveragePower, LowStuff);

        // Assert
        Assert.That(lowDistribution.OutPct, Is.LessThan(avgDistribution.OutPct),
            $"Low Stuff should decrease Outs% (avg: {avgDistribution.OutPct:P2}, low: {lowDistribution.OutPct:P2})");

        // Output for debugging
        TestContext.Out.WriteLine($"Average Stuff (50): Outs={avgDistribution.OutPct:P2}");
        TestContext.Out.WriteLine($"Low Stuff (20):     Outs={lowDistribution.OutPct:P2}");
    }

    /// <summary>
    /// Validates that low Stuff increases BABIP compared to average.
    /// </summary>
    [Test]
    public void LowStuff_IncreasesBabip() {
        // Arrange
        var avgDistribution = ExecuteSut(AveragePower, AverageStuff);

        // Act
        var lowDistribution = ExecuteSut(AveragePower, LowStuff);

        // Assert
        Assert.That(lowDistribution.Babip, Is.GreaterThan(avgDistribution.Babip),
            $"Low Stuff should increase BABIP (avg: {avgDistribution.Babip:F3}, low: {lowDistribution.Babip:F3})");

        // Output for debugging
        TestContext.Out.WriteLine($"Average Stuff (50): BABIP={avgDistribution.Babip:F3}");
        TestContext.Out.WriteLine($"Low Stuff (20):     BABIP={lowDistribution.Babip:F3}");
    }

    /// <summary>
    /// Validates that extreme parameter combinations (min/max Power and Stuff) produce valid distributions.
    /// Tests boundary conditions to ensure the system handles edge cases correctly:
    /// - All percentages sum to 100%
    /// - No negative percentages
    /// - BABIP remains in valid range [0.0, 1.0]
    /// </summary>
    [TestCase(0, 0, TestName = "MinPower_MinStuff")]
    [TestCase(100, 100, TestName = "MaxPower_MaxStuff")]
    [TestCase(0, 100, TestName = "MinPower_MaxStuff")]
    [TestCase(100, 0, TestName = "MaxPower_MinStuff")]
    public void ExtremeParams_ProduceValidDistributions(int power, int stuff) {
        // Act
        var distribution = ExecuteSut(power, stuff);

        // Assert
        distribution.AssertTotalPercentageIsOne();
        distribution.AssertAllPercentagesNonNegative();

        Assert.That(distribution.Babip, Is.InRange(0.0, 1.0),
            $"BABIP should be in valid range [0.0, 1.0] for Power={power}, Stuff={stuff}");

        // Output for debugging
        TestContext.Out.WriteLine($"Power={power}, Stuff={stuff}: Outs={distribution.OutPct:P2}, BABIP={distribution.Babip:F3}, HR={distribution.HrPct:P2}");
    }

    /// <summary>
    /// Executes the System Under Test (SUT) - simulates ball-in-play outcomes and returns distribution.
    /// </summary>
    private static BipDistribution ExecuteSut(int power, int stuff) {
        var random = new SeededRandom(Seed);
        var trials = TestConfig.SIM_DEFAULT_N;
        var distribution = new BipDistribution(trials);

        for (int i = 0; i < trials; i++) {
            var outcome = BallInPlayResolver.ResolveBallInPlay(power, stuff, random);
            distribution.RecordOutcome(outcome);
        }

        return distribution;
    }

    /// <summary>
    /// Helper class to hold BIP distribution results.
    /// </summary>
    private class BipDistribution {
        public int Outs { get; private set; }
        public int Singles { get; private set; }
        public int Doubles { get; private set; }
        public int Triples { get; private set; }
        public int HomeRuns { get; private set; }
        private int Trials { get; }

        public BipDistribution(int trials) {
            Trials = trials;
        }

        /// <summary>
        /// Records a ball-in-play outcome, updating internal counts.
        /// </summary>
        public void RecordOutcome(BipOutcome outcome) {
            switch (outcome) {
                case BipOutcome.Out: Outs++; break;
                case BipOutcome.Single: Singles++; break;
                case BipOutcome.Double: Doubles++; break;
                case BipOutcome.Triple: Triples++; break;
                case BipOutcome.HomeRun: HomeRuns++; break;
            }
        }

        public double OutPct => (double)Outs / Trials;
        public double SinglePct => (double)Singles / Trials;
        public double DoublePct => (double)Doubles / Trials;
        public double TriplePct => (double)Triples / Trials;
        public double HrPct => (double)HomeRuns / Trials;

        /// <summary>
        /// Sum of all outcome percentages (should equal 1.0).
        /// </summary>
        public double TotalPct => OutPct + SinglePct + DoublePct + TriplePct + HrPct;

        /// <summary>
        /// BABIP = (Singles + Doubles + Triples) / (BIP - HomeRuns)
        /// Returns 0.0 if all BIP are home runs (edge case safety guard).
        /// </summary>
        public double Babip => Trials == HomeRuns
            ? 0.0
            : (double)(Singles + Doubles + Triples) / (Trials - HomeRuns);

        /// <summary>
        /// Asserts that the total percentage equals 1.0 (100%).
        /// </summary>
        public void AssertTotalPercentageIsOne() {
            Assert.That(TotalPct, Is.EqualTo(1.0).Within(0.0001),
                "All percentages should sum to 100%");
        }

        /// <summary>
        /// Asserts that all outcome percentages are non-negative.
        /// </summary>
        public void AssertAllPercentagesNonNegative() {
            Assert.That(OutPct, Is.GreaterThanOrEqualTo(0.0), "Out% should be non-negative");
            Assert.That(SinglePct, Is.GreaterThanOrEqualTo(0.0), "Single% should be non-negative");
            Assert.That(DoublePct, Is.GreaterThanOrEqualTo(0.0), "Double% should be non-negative");
            Assert.That(TriplePct, Is.GreaterThanOrEqualTo(0.0), "Triple% should be non-negative");
            Assert.That(HrPct, Is.GreaterThanOrEqualTo(0.0), "HR% should be non-negative");
        }
    }
}
