namespace DiamondSim.Tests.Probabilities;

/// <summary>
/// Tests for the complete at-bat simulation loop, validating that K%, BB%, and BIP%
/// distributions fall within expected ranges for various matchups.
/// </summary>
[TestFixture]
public class AtBatLoopTests {

    /// <summary>
    /// Tests that average vs. average matchups produce realistic outcome distributions.
    /// Expected ranges based on typical MLB statistics:
    /// - K% (Strikeout): 18-28%
    /// - BB% (Walk): 7-12%
    /// - BIP% (Ball In Play): 55-70%
    ///
    /// Uses 10,000 trials with seeded RNG for deterministic, reproducible results.
    /// </summary>
    [Test]
    public void AtBatOutcomes_AverageVsAverage_ProducesRealisticDistributions() {
        // Arrange
        const int trials = 10_000;
        const int seed = 12345;
        var random = new SeededRandom(seed);
        var simulator = new AtBatSimulator(random);

        var pitcher = PitcherRatings.Average;
        var batter = BatterRatings.Average;

        int strikeouts = 0;
        int walks = 0;
        int ballsInPlay = 0;
        int hitByPitch = 0;

        // Act
        for (int i = 0; i < trials; i++) {
            var result = simulator.SimulateAtBat(pitcher, batter);

            switch (result.Terminal) {
                case AtBatTerminal.Strikeout:
                    strikeouts++;
                    break;
                case AtBatTerminal.Walk:
                    walks++;
                    break;
                case AtBatTerminal.BallInPlay:
                    ballsInPlay++;
                    break;
                case AtBatTerminal.HitByPitch:
                    hitByPitch++;
                    break;
            }
        }

        // Calculate percentages
        double kRate = (double)strikeouts / trials;
        double bbRate = (double)walks / trials;
        double bipRate = (double)ballsInPlay / trials;
        double hbpRate = (double)hitByPitch / trials;

        // Assert: Verify distributions fall within expected ranges
        Assert.That(kRate, Is.InRange(0.18, 0.28),
            $"K% should be 18-28%, got {kRate:P1}");
        Assert.That(bbRate, Is.InRange(0.06, 0.12),
            $"BB% should be 6-12%, got {bbRate:P1}");
        Assert.That(bipRate, Is.InRange(0.55, 0.70),
            $"BIP% should be 55-70%, got {bipRate:P1}");

        // Assert: Verify all outcomes sum to 100%
        double total = kRate + bbRate + bipRate + hbpRate;
        Assert.That(total, Is.EqualTo(1.0).Within(0.0001),
            $"K% + BB% + BIP% + HBP% should equal 100%, got {total:P1}");

        // Output actual distributions for reference
        TestContext.Out.WriteLine($"Strikeout Rate: {kRate:P2} ({strikeouts}/{trials})");
        TestContext.Out.WriteLine($"Walk Rate: {bbRate:P2} ({walks}/{trials})");
        TestContext.Out.WriteLine($"Ball-In-Play Rate: {bipRate:P2} ({ballsInPlay}/{trials})");
        TestContext.Out.WriteLine($"Hit-By-Pitch Rate: {hbpRate:P2} ({hitByPitch}/{trials})");
    }

    /// <summary>
    /// Tests that high Control pitchers produce fewer walks.
    /// Expected: BB% should be lower than average, K% and BIP% should adjust accordingly.
    /// </summary>
    [Test]
    public void AtBatOutcomes_HighControlPitcher_ProducesFewerWalks() {
        // Arrange
        const int trials = 5_000;
        const int seed = 23456;
        var random = new SeededRandom(seed);
        var simulator = new AtBatSimulator(random);

        var highControlPitcher = new PitcherRatings(Control: 70, Stuff: 50, Stamina: 50, Speed: 50);
        var batter = BatterRatings.Average;

        int walks = 0;

        // Act
        for (int i = 0; i < trials; i++) {
            var result = simulator.SimulateAtBat(highControlPitcher, batter);
            if (result.Terminal == AtBatTerminal.Walk) {
                walks++;
            }
        }

        double bbRate = (double)walks / trials;

        // Assert: High Control should produce BB% at or below the lower end of normal range
        Assert.That(bbRate, Is.LessThanOrEqualTo(0.10),
            $"High Control pitcher should have BB% ≤ 10%, got {bbRate:P1}");

        TestContext.Out.WriteLine($"High Control Pitcher Walk Rate: {bbRate:P2} ({walks}/{trials})");
    }

    /// <summary>
    /// Tests that high Patience batters produce more walks and fewer strikeouts.
    /// Expected: BB% should be higher, K% should be lower than average.
    /// </summary>
    [Test]
    public void AtBatOutcomes_HighPatienceBatter_ProducesMoreWalksFewerStrikeouts() {
        // Arrange
        const int trials = 5_000;
        const int seed = 34567;
        var random = new SeededRandom(seed);
        var simulator = new AtBatSimulator(random);

        var pitcher = PitcherRatings.Average;
        var highPatienceBatter = new BatterRatings(Contact: 50, Power: 50, Patience: 70, Speed: 50);

        int strikeouts = 0;
        int walks = 0;

        // Act
        for (int i = 0; i < trials; i++) {
            var result = simulator.SimulateAtBat(pitcher, highPatienceBatter);

            if (result.Terminal == AtBatTerminal.Strikeout) {
                strikeouts++;
            }
            else if (result.Terminal == AtBatTerminal.Walk) {
                walks++;
            }
        }

        double kRate = (double)strikeouts / trials;
        double bbRate = (double)walks / trials;

        // Assert: High Patience should produce higher BB% and lower K%
        Assert.That(bbRate, Is.GreaterThanOrEqualTo(0.09),
            $"High Patience batter should have BB% ≥ 9%, got {bbRate:P1}");
        Assert.That(kRate, Is.LessThanOrEqualTo(0.25),
            $"High Patience batter should have K% ≤ 25%, got {kRate:P1}");

        TestContext.Out.WriteLine($"High Patience Batter K%: {kRate:P2}, BB%: {bbRate:P2}");
    }

    /// <summary>
    /// Tests that low Contact batters produce more strikeouts.
    /// Expected: K% should be higher than average.
    /// </summary>
    [Test]
    public void AtBatOutcomes_LowContactBatter_ProducesMoreStrikeouts() {
        // Arrange
        const int trials = 5_000;
        const int seed = 45678;
        var random = new SeededRandom(seed);
        var simulator = new AtBatSimulator(random);

        var pitcher = PitcherRatings.Average;
        var lowContactBatter = new BatterRatings(Contact: 30, Power: 50, Patience: 50, Speed: 50);

        int strikeouts = 0;

        // Act
        for (int i = 0; i < trials; i++) {
            var result = simulator.SimulateAtBat(pitcher, lowContactBatter);
            if (result.Terminal == AtBatTerminal.Strikeout) {
                strikeouts++;
            }
        }

        double kRate = (double)strikeouts / trials;

        // Assert: Low Contact should produce K% at or above the upper end of normal range
        Assert.That(kRate, Is.GreaterThanOrEqualTo(0.25),
            $"Low Contact batter should have K% ≥ 25%, got {kRate:P1}");

        TestContext.Out.WriteLine($"Low Contact Batter Strikeout Rate: {kRate:P2} ({strikeouts}/{trials})");
    }

    /// <summary>
    /// Tests that all at-bats reach a terminal outcome (no infinite loops).
    /// </summary>
    [Test]
    public void AtBatOutcomes_AllAtBats_ReachTerminalOutcome() {
        // Arrange
        const int trials = 1_000;
        const int seed = 56789;
        var random = new SeededRandom(seed);
        var simulator = new AtBatSimulator(random);

        var pitcher = PitcherRatings.Average;
        var batter = BatterRatings.Average;

        // Act & Assert
        for (int i = 0; i < trials; i++) {
            var result = simulator.SimulateAtBat(pitcher, batter);

            // Verify we got a valid terminal outcome
            Assert.That(result.Terminal, Is.AnyOf(
                AtBatTerminal.Strikeout,
                AtBatTerminal.Walk,
                AtBatTerminal.BallInPlay,
                AtBatTerminal.HitByPitch
            ));

            // Verify pitch count is reasonable (not stuck in infinite loop)
            Assert.That(result.PitchCount, Is.GreaterThan(0).And.LessThan(50),
                $"Pitch count should be reasonable, got {result.PitchCount}");
        }
    }

    /// <summary>
    /// Tests that foul balls with 2 strikes don't create a third strike.
    /// This test simulates many at-bats and verifies that some reach high pitch counts
    /// (indicating foul balls at 2 strikes), and that these eventually terminate properly.
    /// </summary>
    [Test]
    public void AtBatOutcomes_FoulBallsWithTwoStrikes_DoNotCreateThirdStrike() {
        // Arrange
        const int trials = 10_000;
        const int seed = 67890;
        var random = new SeededRandom(seed);
        var simulator = new AtBatSimulator(random);

        var pitcher = PitcherRatings.Average;
        var batter = BatterRatings.Average;

        int highPitchCountAtBats = 0;
        int maxPitchCount = 0;

        // Act
        for (int i = 0; i < trials; i++) {
            var result = simulator.SimulateAtBat(pitcher, batter);

            if (result.PitchCount > 10) {
                highPitchCountAtBats++;
            }

            if (result.PitchCount > maxPitchCount) {
                maxPitchCount = result.PitchCount;
            }

            // All at-bats should still terminate properly
            Assert.That(result.Terminal, Is.AnyOf(
                AtBatTerminal.Strikeout,
                AtBatTerminal.Walk,
                AtBatTerminal.BallInPlay,
                AtBatTerminal.HitByPitch
            ));
        }

        // Assert: We should see some at-bats with high pitch counts (indicating foul balls at 2 strikes)
        Assert.That(highPitchCountAtBats, Is.GreaterThan(0),
            "Should have some at-bats with >10 pitches (foul balls at 2 strikes)");

        TestContext.Out.WriteLine($"At-bats with >10 pitches: {highPitchCountAtBats}/{trials}");
        TestContext.Out.WriteLine($"Maximum pitch count observed: {maxPitchCount}");
    }

    /// <summary>
    /// Tests that the AtBatResult contains accurate information.
    /// </summary>
    [Test]
    public void AtBatResult_ContainsAccurateInformation() {
        // Arrange
        const int seed = 78901;
        var random = new SeededRandom(seed);
        var simulator = new AtBatSimulator(random);

        var pitcher = PitcherRatings.Average;
        var batter = BatterRatings.Average;

        // Act
        var result = simulator.SimulateAtBat(pitcher, batter);

        // Assert
        Assert.That(result.Terminal, Is.AnyOf(
            AtBatTerminal.Strikeout,
            AtBatTerminal.Walk,
            AtBatTerminal.BallInPlay,
            AtBatTerminal.HitByPitch
        ));
        Assert.That(result.FinalCount, Is.Not.Null.And.Not.Empty);
        Assert.That(result.PitchCount, Is.GreaterThan(0));

        // Verify final count makes sense for the terminal outcome
        if (result.Terminal == AtBatTerminal.Strikeout) {
            Assert.That(result.FinalCount, Does.EndWith("-3"),
                $"Strikeout should end with 3 strikes, got {result.FinalCount}");
        }
        else if (result.Terminal == AtBatTerminal.Walk) {
            Assert.That(result.FinalCount, Does.StartWith("4-"),
                $"Walk should start with 4 balls, got {result.FinalCount}");
        }

        TestContext.Out.WriteLine($"Terminal: {result.Terminal}, Count: {result.FinalCount}, Pitches: {result.PitchCount}");
    }

    /// <summary>
    /// Tests that HBP rate is realistic (~1-2% per PA, not per pitch).
    /// With the current bug (1% per pitch), HBP rate explodes to several percent per PA.
    /// This test should FAIL initially, then PASS after the fix.
    /// Uses multiple seeds to ensure statistical stability.
    /// </summary>
    [Test]
    public void AtBatOutcomes_HitByPitch_RateIsRealisticPerPA_MultiSeed() {
        const int trialsPerSeed = 20000;
        int[] seeds = { 111, 222, 333, 444 };

        double totalRate = 0;
        foreach (var seed in seeds) {
            var rng = new SeededRandom(seed);
            var sim = new AtBatSimulator(rng);
            var pitcher = PitcherRatings.Average;
            var batter = BatterRatings.Average;

            int hbp = 0;
            for (int i = 0; i < trialsPerSeed; i++) {
                var res = sim.SimulateAtBat(pitcher, batter);
                if (res.Terminal == AtBatTerminal.HitByPitch) hbp++;
            }

            double rate = (double)hbp / trialsPerSeed;
            totalRate += rate;
            TestContext.Out.WriteLine($"Seed {seed}: HBP {hbp}/{trialsPerSeed} = {rate:P2}");
        }

        double avgRate = totalRate / seeds.Length;

        // Start generous; tighten to 0.008–0.020 once you tune ratings
        Assert.That(avgRate, Is.InRange(0.008, 0.020), $"Avg HBP rate was {avgRate:P2}");
    }
}
