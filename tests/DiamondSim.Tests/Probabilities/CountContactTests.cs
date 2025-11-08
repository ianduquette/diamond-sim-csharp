using DiamondSim.Tests.TestHelpers;

namespace DiamondSim.Tests.Probabilities;

/// <summary>
/// Tests for count-conditioned contact probability adjustments.
/// Validates that contact rates vary monotonically across different counts.
/// Tests the ContactResolver directly to ensure it applies count-based adjustments correctly.
/// </summary>
[TestFixture]
public class CountContactTests {
    private const int Seed = 1337;

    [Test]
    public void ContactRate_0_2_Count_FavorsPitcher() {
        // Arrange
        var batter = TestFactory.CreateAverageBatter();
        var pitcher = TestFactory.CreateAveragePitcher();
        var baselineRate = ExecuteSut(batter, pitcher, balls: 0, strikes: 0);

        // Act
        var rate_0_2 = ExecuteSut(batter, pitcher, balls: 0, strikes: 2);

        // Assert - 0-2 should have lower contact rate than neutral
        Assert.That(rate_0_2, Is.LessThan(baselineRate),
            $"Contact rate at 0-2 ({rate_0_2:F4}) should be less than at 0-0 ({baselineRate:F4})");
        Assert.That(rate_0_2, Is.InRange(0.60, 0.80));
    }

    [Test]
    public void ContactRate_2_0_Count_FavorsHitter() {
        // Arrange
        var batter = TestFactory.CreateAverageBatter();
        var pitcher = TestFactory.CreateAveragePitcher();
        var baselineRate = ExecuteSut(batter, pitcher, balls: 0, strikes: 0);

        // Act
        var rate_2_0 = ExecuteSut(batter, pitcher, balls: 2, strikes: 0);

        // Assert - 2-0 should have higher contact rate than neutral
        Assert.That(rate_2_0, Is.GreaterThan(baselineRate),
            $"Contact rate at 2-0 ({rate_2_0:F4}) should be greater than at 0-0 ({baselineRate:F4})");
        Assert.That(rate_2_0, Is.InRange(0.75, 0.90));
    }

    [Test]
    public void ContactRate_3_0_Count_FavorsHitter() {
        // Arrange
        var batter = TestFactory.CreateAverageBatter();
        var pitcher = TestFactory.CreateAveragePitcher();
        var baselineRate = ExecuteSut(batter, pitcher, balls: 0, strikes: 0);

        // Act
        var rate_3_0 = ExecuteSut(batter, pitcher, balls: 3, strikes: 0);

        // Assert - 3-0 should have higher contact rate than neutral
        Assert.That(rate_3_0, Is.GreaterThan(baselineRate),
            $"Contact rate at 3-0 ({rate_3_0:F4}) should be greater than at 0-0 ({baselineRate:F4})");
        Assert.That(rate_3_0, Is.InRange(0.80, 0.95));
    }

    [Test]
    public void ContactRate_3_2_FullCount_IsBalanced() {
        // Arrange
        var batter = TestFactory.CreateAverageBatter();
        var pitcher = TestFactory.CreateAveragePitcher();
        var baselineRate = ExecuteSut(batter, pitcher, balls: 0, strikes: 0);

        // Act
        var rate_3_2 = ExecuteSut(batter, pitcher, balls: 3, strikes: 2);

        // Assert - Full count should be close to neutral (within 5%)
        var difference = Math.Abs(rate_3_2 - baselineRate);
        Assert.That(difference, Is.LessThan(0.05),
            $"Contact rate at 3-2 ({rate_3_2:F4}) should be close to 0-0 ({baselineRate:F4}), difference: {difference:F4}");
        Assert.That(rate_3_2, Is.InRange(0.70, 0.85));
    }

    /// <summary>
    /// Executes the System Under Test (SUT) - measures contact rate at a specific count.
    /// </summary>
    private static double ExecuteSut(Batter batter, Pitcher pitcher, int balls, int strikes) {
        var rng = new SeededRandom(seed: Seed);
        var resolver = new ContactResolver(rng);
        var trials = TestConfig.SIM_DEFAULT_N;
        var contacts = ContactResolverTestHelper.CountContacts(resolver, batter, pitcher, trials, balls, strikes);
        return (double)contacts / trials;
    }
}
