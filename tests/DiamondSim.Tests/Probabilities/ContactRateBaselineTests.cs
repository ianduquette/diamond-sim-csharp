using DiamondSim.Tests.TestHelpers;


namespace DiamondSim.Tests.Probabilities;

/// <summary>
/// Tests for baseline contact rate validation.
/// Validates that pitch-level contact rates fall within expected MLB ranges.
/// Tests the ContactResolver directly to ensure baseline contact probability is realistic.
/// </summary>
[TestFixture]
public class ContactRateBaselineTests {
    private const int Seed = 1337;

    /// <summary>
    /// Tests the baseline pitch-level contact physics to ensure
    /// average pitcher/batter matchups align with observed MLB per-swing Contact% (~75–77%).
    ///
    /// Contact% = (Swings that make contact) ÷ (Total swings)
    /// Sources:
    /// • The Dynasty Dugout – "Statcast 101: Plate Discipline Metrics"
    ///   https://www.thedynastydugout.com/p/statcast-101-plate-discipline-metrics
    /// • FanGraphs Glossary – "Contact%"
    ///   https://www.fangraphs.com/library/offense/plate-discipline/#Contact
    /// </summary>
    [Test]
    public void AveragePitcherVsAverageBatter_RealisticContactRate() {
        // Arrange
        var pitcher = TestFactory.CreateAveragePitcher();
        var batter = TestFactory.CreateAverageBatter();

        // Act
        var contactRate = ExecuteSut(batter, pitcher);

        // Assert
        Assert.That(contactRate, Is.InRange(0.70, 0.85));
    }

    /// <summary>
    /// Tests that an elite batter (Contact: 70) makes contact at expected rate.
    /// Expected: ~82% contact rate (base 78% + 4% from +20 rating points × 0.002 per point).
    /// Calculation: 0.78 + (70-50) × 0.002 = 0.78 + 0.04 = 0.82
    /// </summary>
    [Test]
    public void EliteBatter_ExpectedContactRate() {
        // Arrange
        var pitcher = TestFactory.CreateAveragePitcher();
        var eliteBatter = TestFactory.CreateEliteBatter();

        // Act
        var contactRate = ExecuteSut(eliteBatter, pitcher);

        // Assert
        Assert.That(contactRate, Is.InRange(0.78, 0.86),
            $"Elite batter (Contact: 70) contact rate ({contactRate:F4}) should be ~82%");
    }

    /// <summary>
    /// Tests that a poor batter (Contact: 30) makes contact at expected rate.
    /// Expected: ~74% contact rate (base 78% - 4% from -20 rating points × 0.002 per point).
    /// Calculation: 0.78 + (30-50) × 0.002 = 0.78 - 0.04 = 0.74
    /// </summary>
    [Test]
    public void PoorBatter_ExpectedContactRate() {
        // Arrange
        var pitcher = TestFactory.CreateAveragePitcher();
        var poorBatter = TestFactory.CreatePoorBatter();

        // Act
        var contactRate = ExecuteSut(poorBatter, pitcher);

        // Assert
        Assert.That(contactRate, Is.InRange(0.70, 0.78),
            $"Poor batter (Contact: 30) contact rate ({contactRate:F4}) should be ~74%");
    }

    /// <summary>
    /// Tests that an elite pitcher (Stuff: 70) induces contact at expected rate.
    /// Expected: ~74% contact rate (base 78% - 4% from +20 stuff rating × -0.002 per point).
    /// Calculation: 0.78 + (70-50) × -0.002 = 0.78 - 0.04 = 0.74
    /// </summary>
    [Test]
    public void ElitePitcher_ExpectedContactRate() {
        // Arrange
        var batter = TestFactory.CreateAverageBatter();
        var elitePitcher = TestFactory.CreateElitePitcher();

        // Act
        var contactRate = ExecuteSut(batter, elitePitcher);

        // Assert
        Assert.That(contactRate, Is.InRange(0.70, 0.78),
            $"Elite pitcher (Stuff: 70) contact rate ({contactRate:F4}) should be ~74%");
    }

    /// <summary>
    /// Tests that a poor pitcher (Stuff: 30) allows contact at expected rate.
    /// Expected: ~82% contact rate (base 78% + 4% from -20 stuff rating × -0.002 per point).
    /// Calculation: 0.78 + (30-50) × -0.002 = 0.78 + 0.04 = 0.82
    /// </summary>
    [Test]
    public void PoorPitcher_ExpectedContactRate() {
        // Arrange
        var batter = TestFactory.CreateAverageBatter();
        var poorPitcher = TestFactory.CreatePoorPitcher();

        // Act
        var contactRate = ExecuteSut(batter, poorPitcher);

        // Assert
        Assert.That(contactRate, Is.InRange(0.78, 0.86),
            $"Poor pitcher (Stuff: 30) contact rate ({contactRate:F4}) should be ~82%");
    }

    /// <summary>
    /// Tests that elite vs elite matchup returns to baseline.
    /// Elite batter (+4%) vs Elite pitcher (-4%) should cancel out to ~78%.
    /// Calculation: 0.78 + (70-50) × 0.002 + (70-50) × -0.002 = 0.78 + 0.04 - 0.04 = 0.78
    /// </summary>
    [Test]
    public void EliteBatterVsElitePitcher_BaselineContactRate() {
        // Arrange
        var eliteBatter = TestFactory.CreateEliteBatter();
        var elitePitcher = TestFactory.CreateElitePitcher();

        // Act
        var contactRate = ExecuteSut(eliteBatter, elitePitcher);

        // Assert
        Assert.That(contactRate, Is.InRange(0.70, 0.85),
            $"Elite vs elite contact rate ({contactRate:F4}) should be ~78% (effects cancel)");
    }

    /// <summary>
    /// Tests extreme mismatch: elite batter vs poor pitcher.
    /// Expected: ~86% contact rate (base 78% + 4% batter + 4% pitcher).
    /// Calculation: 0.78 + (70-50) × 0.002 + (30-50) × -0.002 = 0.78 + 0.04 + 0.04 = 0.86
    /// </summary>
    [Test]
    public void EliteBatterVsPoorPitcher_ExpectedContactRate() {
        // Arrange
        var eliteBatter = TestFactory.CreateEliteBatter();
        var poorPitcher = TestFactory.CreatePoorPitcher();

        // Act
        var contactRate = ExecuteSut(eliteBatter, poorPitcher);

        // Assert
        Assert.That(contactRate, Is.InRange(0.82, 0.90),
            $"Elite batter vs poor pitcher contact rate ({contactRate:F4}) should be ~86%");
    }

    /// <summary>
    /// Tests extreme mismatch: poor batter vs elite pitcher.
    /// Expected: ~70% contact rate (base 78% - 4% batter - 4% pitcher).
    /// Calculation: 0.78 + (30-50) × 0.002 + (70-50) × -0.002 = 0.78 - 0.04 - 0.04 = 0.70
    /// </summary>
    [Test]
    public void PoorBatterVsElitePitcher_ExpectedContactRate() {
        // Arrange
        var poorBatter = TestFactory.CreatePoorBatter();
        var elitePitcher = TestFactory.CreateElitePitcher();

        // Act
        var contactRate = ExecuteSut(poorBatter, elitePitcher);

        // Assert
        Assert.That(contactRate, Is.InRange(0.66, 0.74),
            $"Poor batter vs elite pitcher contact rate ({contactRate:F4}) should be ~70%");
    }

    /// <summary>
    /// Executes the System Under Test (SUT) - measures contact rate for given batter/pitcher matchup.
    /// </summary>
    private static double ExecuteSut(Batter batter, Pitcher pitcher) {
        var rng = new SeededRandom(seed: Seed);
        var resolver = new ContactResolver(rng);
        var trials = TestConfig.SIM_DEFAULT_N;
        var contacts = ContactResolverTestHelper.CountContacts(resolver, batter, pitcher, trials);
        return (double)contacts / trials;
    }

}
