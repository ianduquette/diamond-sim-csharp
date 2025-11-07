using DiamondSim.Tests.TestHelpers;


namespace DiamondSim.Tests.Probabilities;

/// <summary>
/// Tests for baseline contact rate validation.
/// Validates that pitch-level contact rates fall within expected MLB ranges.
/// Tests the ContactResolver directly to ensure baseline contact probability is realistic.
/// </summary>
[TestFixture]
public class ContactRateBaselineTests {

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
        var rng = new SeededRandom(seed: 1337);
        var resolver = new ContactResolver(rng);
        var pitcher = TestFactory.CreateAveragePitcher();
        var batter = TestFactory.CreateAverageBatter();
        var trials = TestConfig.SIM_DEFAULT_N;

        // Act - Test at neutral 0-0 count
        int contacts = ContactResolverTestHelper.CountContacts(resolver, batter, pitcher, trials, balls: 0, strikes: 0);

        // Assert
        var contactRate = (double)contacts / trials;
        Assert.That(contactRate, Is.InRange(0.70, 0.85));
    }
}
