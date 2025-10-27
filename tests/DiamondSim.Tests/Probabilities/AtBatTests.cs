namespace DiamondSim.Tests.Probabilities;

[TestFixture]
public class AtBatTests {
    [Test]
    public void AveragePitcherVsAverageBatter_RealisticContactRate() {
        // Arrange
        var rng = new SeededRandom(seed: 1337);
        var engine = new GameEngine(rng);
        var pitcher = new Pitcher("P Average", PitcherRatings.Average);
        var batter = new Batter("B Average", BatterRatings.Average);

        // Act
        var outcomes = engine.SimulateManyAtBats(batter, pitcher, trials: 10000);

        // Assert — MLB contact rate ~75–80%
        Assert.That(outcomes.ContactRate, Is.InRange(0.70, 0.85));
    }
}
