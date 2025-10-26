using NUnit.Framework;

namespace DiamondSim.Tests;

/// <summary>
/// Tests for ball-in-play outcome resolution and distribution validation.
/// </summary>
[TestFixture]
public class BallInPlayTests {
    private const int TrialCount = 10_000;
    private const int Seed = 12345;

    /// <summary>
    /// Validates that average vs. average matchup produces realistic distributions.
    /// Expected ranges based on typical MLB statistics:
    /// - HR%: 3-5% of all BIP
    /// - 2B%: 5-7% of all BIP (16-22% of hits)
    /// - 3B%: 0.3-0.5% of all BIP (~14 triples per season)
    /// - Singles%: 18-21% of all BIP (60-70% of hits)
    /// - Outs%: ~70-71% of all BIP
    /// - BABIP: 0.26-0.32 (0.29 ± 0.03)
    /// </summary>
    [Test]
    public void BipOutcomes_AverageVsAverage_ProducesRealisticDistributions() {
        // Arrange
        var random = new SeededRandom(Seed);
        int power = 50;  // Average
        int stuff = 50;  // Average

        int outs = 0, singles = 0, doubles = 0, triples = 0, homeRuns = 0;

        // Act - Simulate 10,000 balls in play
        for (int i = 0; i < TrialCount; i++) {
            var outcome = BallInPlayResolver.ResolveBallInPlay(power, stuff, random);
            switch (outcome) {
                case BipOutcome.Out: outs++; break;
                case BipOutcome.Single: singles++; break;
                case BipOutcome.Double: doubles++; break;
                case BipOutcome.Triple: triples++; break;
                case BipOutcome.HomeRun: homeRuns++; break;
            }
        }

        // Calculate percentages
        double hrPct = (double)homeRuns / TrialCount;
        double doublePct = (double)doubles / TrialCount;
        double triplePct = (double)triples / TrialCount;
        double singlePct = (double)singles / TrialCount;
        double outPct = (double)outs / TrialCount;

        // Calculate BABIP: (Singles + Doubles + Triples) / (BIP - HomeRuns)
        double babip = (double)(singles + doubles + triples) / (TrialCount - homeRuns);

        // Assert - Verify all percentages sum to 100%
        double totalPct = hrPct + doublePct + triplePct + singlePct + outPct;
        Assert.That(totalPct, Is.EqualTo(1.0).Within(0.0001),
            "All percentages should sum to 100%");

        // Assert - HR% should be 3-5%
        Assert.That(hrPct, Is.InRange(0.03, 0.05),
            $"HR% should be 3-5%, got {hrPct:P2}");

        // Assert - 2B% should be 5-7% of BIP
        Assert.That(doublePct, Is.InRange(0.05, 0.07),
            $"2B% should be 5-7% of BIP, got {doublePct:P2}");

        // Assert - 3B% should be 0.3-0.5% of BIP (~14 triples per season)
        Assert.That(triplePct, Is.InRange(0.003, 0.005),
            $"3B% should be 0.3-0.5% of BIP, got {triplePct:P2}");

        // Assert - Singles% should be 18-21% of BIP
        Assert.That(singlePct, Is.InRange(0.18, 0.21),
            $"Singles% should be 18-21% of BIP, got {singlePct:P2}");

        // Assert - BABIP should be 0.26-0.32
        Assert.That(babip, Is.InRange(0.26, 0.32),
            $"BABIP should be 0.26-0.32, got {babip:F3}");

        // Output for debugging
        TestContext.Out.WriteLine($"Distribution for Average vs. Average (Power={power}, Stuff={stuff}):");
        TestContext.Out.WriteLine($"  Outs:     {outs,5} ({outPct:P2})");
        TestContext.Out.WriteLine($"  Singles:  {singles,5} ({singlePct:P2})");
        TestContext.Out.WriteLine($"  Doubles:  {doubles,5} ({doublePct:P2})");
        TestContext.Out.WriteLine($"  Triples:  {triples,5} ({triplePct:P2})");
        TestContext.Out.WriteLine($"  HomeRuns: {homeRuns,5} ({hrPct:P2})");
        TestContext.Out.WriteLine($"  BABIP:    {babip:F3}");
    }

    /// <summary>
    /// Validates that high Power increases extra-base hits (HR and 2B).
    /// </summary>
    [Test]
    public void BipOutcomes_HighPower_IncreasesExtraBaseHits() {
        // Arrange
        var random = new SeededRandom(Seed);
        int stuff = 50;  // Average pitcher

        // Simulate with average Power
        var avgResults = SimulateBallsInPlay(power: 50, stuff, random, TrialCount);

        // Reset random for fair comparison
        random = new SeededRandom(Seed);

        // Simulate with high Power
        var highResults = SimulateBallsInPlay(power: 80, stuff, random, TrialCount);

        // Assert - High Power should produce more home runs
        Assert.That(highResults.HrPct, Is.GreaterThan(avgResults.HrPct),
            $"High Power should increase HR% (avg: {avgResults.HrPct:P2}, high: {highResults.HrPct:P2})");

        // Assert - High Power should produce more doubles
        Assert.That(highResults.DoublePct, Is.GreaterThan(avgResults.DoublePct),
            $"High Power should increase 2B% (avg: {avgResults.DoublePct:P2}, high: {highResults.DoublePct:P2})");

        // Assert - High Power should produce fewer outs
        Assert.That(highResults.OutPct, Is.LessThan(avgResults.OutPct),
            $"High Power should decrease Outs% (avg: {avgResults.OutPct:P2}, high: {highResults.OutPct:P2})");

        // Output for debugging
        TestContext.Out.WriteLine($"Average Power (50): HR={avgResults.HrPct:P2}, 2B={avgResults.DoublePct:P2}, Outs={avgResults.OutPct:P2}");
        TestContext.Out.WriteLine($"High Power (80):    HR={highResults.HrPct:P2}, 2B={highResults.DoublePct:P2}, Outs={highResults.OutPct:P2}");
    }

    /// <summary>
    /// Validates that high Stuff increases outs and lowers BABIP.
    /// </summary>
    [Test]
    public void BipOutcomes_HighStuff_IncreasesOuts() {
        // Arrange
        var random = new SeededRandom(Seed);
        int power = 50;  // Average batter

        // Simulate with average Stuff
        var avgResults = SimulateBallsInPlay(power, stuff: 50, random, TrialCount);

        // Reset random for fair comparison
        random = new SeededRandom(Seed);

        // Simulate with high Stuff
        var highResults = SimulateBallsInPlay(power, stuff: 80, random, TrialCount);

        // Assert - High Stuff should produce more outs
        Assert.That(highResults.OutPct, Is.GreaterThan(avgResults.OutPct),
            $"High Stuff should increase Outs% (avg: {avgResults.OutPct:P2}, high: {highResults.OutPct:P2})");

        // Assert - High Stuff should produce lower BABIP
        Assert.That(highResults.Babip, Is.LessThan(avgResults.Babip),
            $"High Stuff should decrease BABIP (avg: {avgResults.Babip:F3}, high: {highResults.Babip:F3})");

        // Output for debugging
        TestContext.Out.WriteLine($"Average Stuff (50): Outs={avgResults.OutPct:P2}, BABIP={avgResults.Babip:F3}");
        TestContext.Out.WriteLine($"High Stuff (80):    Outs={highResults.OutPct:P2}, BABIP={highResults.Babip:F3}");
    }

    /// <summary>
    /// Validates that low Power favors contact hits (singles) over extra-base hits.
    /// </summary>
    [Test]
    public void BipOutcomes_LowPower_FavorsContactHits() {
        // Arrange
        var random = new SeededRandom(Seed);
        int stuff = 50;  // Average pitcher

        // Simulate with average Power
        var avgResults = SimulateBallsInPlay(power: 50, stuff, random, TrialCount);

        // Reset random for fair comparison
        random = new SeededRandom(Seed);

        // Simulate with low Power
        var lowResults = SimulateBallsInPlay(power: 20, stuff, random, TrialCount);

        // Assert - Low Power should produce fewer home runs
        Assert.That(lowResults.HrPct, Is.LessThan(avgResults.HrPct),
            $"Low Power should decrease HR% (avg: {avgResults.HrPct:P2}, low: {lowResults.HrPct:P2})");

        // Assert - Low Power should produce more singles (as percentage of hits)
        double avgSinglePctOfHits = avgResults.SinglePct / (1.0 - avgResults.OutPct);
        double lowSinglePctOfHits = lowResults.SinglePct / (1.0 - lowResults.OutPct);
        Assert.That(lowSinglePctOfHits, Is.GreaterThan(avgSinglePctOfHits),
            $"Low Power should increase Singles% of hits (avg: {avgSinglePctOfHits:P2}, low: {lowSinglePctOfHits:P2})");

        // Output for debugging
        TestContext.Out.WriteLine($"Average Power (50): HR={avgResults.HrPct:P2}, 1B%ofHits={avgSinglePctOfHits:P2}");
        TestContext.Out.WriteLine($"Low Power (20):     HR={lowResults.HrPct:P2}, 1B%ofHits={lowSinglePctOfHits:P2}");
    }

    /// <summary>
    /// Validates that low Stuff decreases outs and increases BABIP.
    /// </summary>
    [Test]
    public void BipOutcomes_LowStuff_DecreasesOuts() {
        // Arrange
        var random = new SeededRandom(Seed);
        int power = 50;  // Average batter

        // Simulate with average Stuff
        var avgResults = SimulateBallsInPlay(power, stuff: 50, random, TrialCount);

        // Reset random for fair comparison
        random = new SeededRandom(Seed);

        // Simulate with low Stuff
        var lowResults = SimulateBallsInPlay(power, stuff: 20, random, TrialCount);

        // Assert - Low Stuff should produce fewer outs
        Assert.That(lowResults.OutPct, Is.LessThan(avgResults.OutPct),
            $"Low Stuff should decrease Outs% (avg: {avgResults.OutPct:P2}, low: {lowResults.OutPct:P2})");

        // Assert - Low Stuff should produce higher BABIP
        Assert.That(lowResults.Babip, Is.GreaterThan(avgResults.Babip),
            $"Low Stuff should increase BABIP (avg: {avgResults.Babip:F3}, low: {lowResults.Babip:F3})");

        // Output for debugging
        TestContext.Out.WriteLine($"Average Stuff (50): Outs={avgResults.OutPct:P2}, BABIP={avgResults.Babip:F3}");
        TestContext.Out.WriteLine($"Low Stuff (20):     Outs={lowResults.OutPct:P2}, BABIP={lowResults.Babip:F3}");
    }

    /// <summary>
    /// Validates that extreme attribute combinations produce valid distributions.
    /// </summary>
    [Test]
    public void BipOutcomes_ExtremeAttributes_ProduceValidDistributions() {
        // Arrange
        var random = new SeededRandom(Seed);

        // Test extreme combinations
        var combinations = new[] {
            (power: 0, stuff: 0, name: "Min Power, Min Stuff"),
            (power: 100, stuff: 100, name: "Max Power, Max Stuff"),
            (power: 0, stuff: 100, name: "Min Power, Max Stuff"),
            (power: 100, stuff: 0, name: "Max Power, Min Stuff")
        };

        foreach (var (power, stuff, name) in combinations) {
            // Reset random for each test
            random = new SeededRandom(Seed);

            var results = SimulateBallsInPlay(power, stuff, random, TrialCount);

            // Assert - All percentages should sum to 1.0
            double totalPct = results.OutPct + results.SinglePct + results.DoublePct +
                            results.TriplePct + results.HrPct;
            Assert.That(totalPct, Is.EqualTo(1.0).Within(0.0001),
                $"{name}: All percentages should sum to 100%");

            // Assert - All percentages should be non-negative
            Assert.That(results.OutPct, Is.GreaterThanOrEqualTo(0.0),
                $"{name}: Out% should be non-negative");
            Assert.That(results.SinglePct, Is.GreaterThanOrEqualTo(0.0),
                $"{name}: Single% should be non-negative");
            Assert.That(results.DoublePct, Is.GreaterThanOrEqualTo(0.0),
                $"{name}: Double% should be non-negative");
            Assert.That(results.TriplePct, Is.GreaterThanOrEqualTo(0.0),
                $"{name}: Triple% should be non-negative");
            Assert.That(results.HrPct, Is.GreaterThanOrEqualTo(0.0),
                $"{name}: HR% should be non-negative");

            // Assert - BABIP should be in reasonable range (0.0-1.0)
            Assert.That(results.Babip, Is.InRange(0.0, 1.0),
                $"{name}: BABIP should be between 0.0 and 1.0");

            // Output for debugging
            TestContext.Out.WriteLine($"{name}: Outs={results.OutPct:P2}, BABIP={results.Babip:F3}, HR={results.HrPct:P2}");
        }
    }

    /// <summary>
    /// Helper method to simulate multiple balls in play and return aggregated results.
    /// </summary>
    private BipDistribution SimulateBallsInPlay(int power, int stuff, IRandomSource random, int trials) {
        int outs = 0, singles = 0, doubles = 0, triples = 0, homeRuns = 0;

        for (int i = 0; i < trials; i++) {
            var outcome = BallInPlayResolver.ResolveBallInPlay(power, stuff, random);
            switch (outcome) {
                case BipOutcome.Out: outs++; break;
                case BipOutcome.Single: singles++; break;
                case BipOutcome.Double: doubles++; break;
                case BipOutcome.Triple: triples++; break;
                case BipOutcome.HomeRun: homeRuns++; break;
            }
        }

        return new BipDistribution {
            Outs = outs,
            Singles = singles,
            Doubles = doubles,
            Triples = triples,
            HomeRuns = homeRuns,
            Trials = trials
        };
    }

    /// <summary>
    /// Helper structure to hold BIP distribution results.
    /// </summary>
    private struct BipDistribution {
        public int Outs;
        public int Singles;
        public int Doubles;
        public int Triples;
        public int HomeRuns;
        public int Trials;

        public double OutPct => (double)Outs / Trials;
        public double SinglePct => (double)Singles / Trials;
        public double DoublePct => (double)Doubles / Trials;
        public double TriplePct => (double)Triples / Trials;
        public double HrPct => (double)HomeRuns / Trials;

        /// <summary>
        /// BABIP = (Singles + Doubles + Triples) / (BIP - HomeRuns)
        /// </summary>
        public double Babip => (double)(Singles + Doubles + Triples) / (Trials - HomeRuns);
    }
}
