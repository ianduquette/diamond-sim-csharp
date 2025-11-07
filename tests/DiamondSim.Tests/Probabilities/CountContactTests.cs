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
        // Arrange - Establish baseline at 0-0 (neutral count)
        var rng = new SeededRandom(Seed);
        var resolver = new ContactResolver(rng);
        var baselineRate = ContactResolverTestHelper.MeasureContactRate(
            resolver, TestConfig.SIM_DEFAULT_N, balls: 0, strikes: 0);

        // Act - Measure contact rate at 0-2 (pitcher's count)
        var rate_0_2 = ContactResolverTestHelper.MeasureContactRate(
            resolver, TestConfig.SIM_DEFAULT_N, balls: 0, strikes: 2);

        // Assert - 0-2 should have lower contact rate than neutral
        Assert.That(rate_0_2, Is.LessThan(baselineRate),
            $"Contact rate at 0-2 ({rate_0_2:F4}) should be less than at 0-0 ({baselineRate:F4})");
        Assert.That(rate_0_2, Is.InRange(0.60, 0.80));
    }

    [Test]
    public void ContactRate_2_0_Count_FavorsHitter() {
        // Arrange - Establish baseline at 0-0 (neutral count)
        var rng = new SeededRandom(Seed);
        var resolver = new ContactResolver(rng);
        var baselineRate = ContactResolverTestHelper.MeasureContactRate(
            resolver, TestConfig.SIM_DEFAULT_N, balls: 0, strikes: 0);

        // Act - Measure contact rate at 2-0 (hitter's count)
        var rate_2_0 = ContactResolverTestHelper.MeasureContactRate(
            resolver, TestConfig.SIM_DEFAULT_N, balls: 2, strikes: 0);

        // Assert - 2-0 should have higher contact rate than neutral
        Assert.That(rate_2_0, Is.GreaterThan(baselineRate),
            $"Contact rate at 2-0 ({rate_2_0:F4}) should be greater than at 0-0 ({baselineRate:F4})");
        Assert.That(rate_2_0, Is.InRange(0.75, 0.90));
    }

    [Test]
    public void ContactRate_3_0_Count_FavorsHitter() {
        // Arrange - Establish baseline at 0-0 (neutral count)
        var rng = new SeededRandom(Seed);
        var resolver = new ContactResolver(rng);
        var baselineRate = ContactResolverTestHelper.MeasureContactRate(
            resolver, TestConfig.SIM_DEFAULT_N, balls: 0, strikes: 0);

        // Act - Measure contact rate at 3-0 (extreme hitter's count)
        var rate_3_0 = ContactResolverTestHelper.MeasureContactRate(
            resolver, TestConfig.SIM_DEFAULT_N, balls: 3, strikes: 0);

        // Assert - 3-0 should have higher contact rate than neutral
        Assert.That(rate_3_0, Is.GreaterThan(baselineRate),
            $"Contact rate at 3-0 ({rate_3_0:F4}) should be greater than at 0-0 ({baselineRate:F4})");
        Assert.That(rate_3_0, Is.InRange(0.80, 0.95));
    }

    [Test]
    public void ContactRate_3_2_FullCount_IsBalanced() {
        // Arrange - Establish baseline at 0-0 (neutral count)
        var rng = new SeededRandom(Seed);
        var resolver = new ContactResolver(rng);
        var baselineRate = ContactResolverTestHelper.MeasureContactRate(
            resolver, TestConfig.SIM_DEFAULT_N, balls: 0, strikes: 0);

        // Act - Measure contact rate at 3-2 (full count)
        var rate_3_2 = ContactResolverTestHelper.MeasureContactRate(
            resolver, TestConfig.SIM_DEFAULT_N, balls: 3, strikes: 2);

        // Assert - Full count should be close to neutral (within 5%)
        var difference = Math.Abs(rate_3_2 - baselineRate);
        Assert.That(difference, Is.LessThan(0.05),
            $"Contact rate at 3-2 ({rate_3_2:F4}) should be close to 0-0 ({baselineRate:F4}), difference: {difference:F4}");
        Assert.That(rate_3_2, Is.InRange(0.70, 0.85));
    }
}
