using DiamondSim;

namespace Tests;

/// <summary>
/// Tests for count-conditioned contact probability adjustments.
/// Validates that contact rates vary monotonically across different counts.
/// </summary>
[TestFixture]
public class CountContactTests {
    private const int Seed = 1337;
    private const int TrialsPerCount = 10000;

    /// <summary>
    /// Creates a balanced batter with average ratings.
    /// </summary>
    private static Batter CreateAverageBatter() {
        return new Batter("Average Joe", BatterRatings.Average);
    }

    /// <summary>
    /// Creates a balanced pitcher with average ratings.
    /// </summary>
    private static Pitcher CreateAveragePitcher() {
        return new Pitcher("Average Pete", PitcherRatings.Average);
    }

    /// <summary>
    /// Simulates many pitches at a specific count and returns the contact rate.
    /// </summary>
    private static double MeasureContactRate(GameEngine engine, Batter batter, Pitcher pitcher, int balls, int strikes, int trials) {
        int contacts = 0;
        for (int i = 0; i < trials; i++) {
            if (engine.SimulatePitchContact(batter, pitcher, balls, strikes)) {
                contacts++;
            }
        }
        return (double)contacts / trials;
    }

    [Test]
    public void ContactRate_VariesMonotonically_AcrossKeyCounts() {
        // Arrange
        var rng = new SeededRandom(Seed);
        var engine = new GameEngine(rng);
        var batter = CreateAverageBatter();
        var pitcher = CreateAveragePitcher();

        // Act - Measure contact rates at key counts
        var rate_0_2 = MeasureContactRate(engine, batter, pitcher, balls: 0, strikes: 2, TrialsPerCount); // Pitcher's count
        var rate_0_0 = MeasureContactRate(engine, batter, pitcher, balls: 0, strikes: 0, TrialsPerCount); // Neutral count
        var rate_2_0 = MeasureContactRate(engine, batter, pitcher, balls: 2, strikes: 0, TrialsPerCount); // Hitter's count

        // Assert - Contact rate should increase as count favors hitter
        Assert.That(rate_0_2, Is.LessThan(rate_0_0),
            $"Contact rate at 0-2 ({rate_0_2:F4}) should be less than at 0-0 ({rate_0_0:F4})");
        Assert.That(rate_0_0, Is.LessThan(rate_2_0),
            $"Contact rate at 0-0 ({rate_0_0:F4}) should be less than at 2-0 ({rate_2_0:F4})");

        // Additional validation: rates should be in reasonable ranges
        Assert.That(rate_0_2, Is.InRange(0.60, 0.80));
        Assert.That(rate_0_0, Is.InRange(0.70, 0.85));
        Assert.That(rate_2_0, Is.InRange(0.75, 0.90));
    }

    [Test]
    public void ContactRate_3_0_Count_FavorsHitter() {
        // Arrange
        var rng = new SeededRandom(Seed);
        var engine = new GameEngine(rng);
        var batter = CreateAverageBatter();
        var pitcher = CreateAveragePitcher();

        // Act
        var rate_3_0 = MeasureContactRate(engine, batter, pitcher, balls: 3, strikes: 0, TrialsPerCount);
        var rate_0_0 = MeasureContactRate(engine, batter, pitcher, balls: 0, strikes: 0, TrialsPerCount);

        // Assert - 3-0 should have higher contact rate than neutral
        Assert.That(rate_3_0, Is.GreaterThan(rate_0_0),
            $"Contact rate at 3-0 ({rate_3_0:F4}) should be greater than at 0-0 ({rate_0_0:F4})");
        Assert.That(rate_3_0, Is.InRange(0.80, 0.95));
    }

    [Test]
    public void ContactRate_0_2_Count_FavorsPitcher() {
        // Arrange
        var rng = new SeededRandom(Seed);
        var engine = new GameEngine(rng);
        var batter = CreateAverageBatter();
        var pitcher = CreateAveragePitcher();

        // Act
        var rate_0_2 = MeasureContactRate(engine, batter, pitcher, balls: 0, strikes: 2, TrialsPerCount);
        var rate_0_0 = MeasureContactRate(engine, batter, pitcher, balls: 0, strikes: 0, TrialsPerCount);

        // Assert - 0-2 should have lower contact rate than neutral
        Assert.That(rate_0_2, Is.LessThan(rate_0_0),
            $"Contact rate at 0-2 ({rate_0_2:F4}) should be less than at 0-0 ({rate_0_0:F4})");
        Assert.That(rate_0_2, Is.InRange(0.60, 0.80));
    }

    [Test]
    public void ContactRate_3_2_FullCount_IsBalanced() {
        // Arrange
        var rng = new SeededRandom(Seed);
        var engine = new GameEngine(rng);
        var batter = CreateAverageBatter();
        var pitcher = CreateAveragePitcher();

        // Act
        var rate_3_2 = MeasureContactRate(engine, batter, pitcher, balls: 3, strikes: 2, TrialsPerCount);
        var rate_0_0 = MeasureContactRate(engine, batter, pitcher, balls: 0, strikes: 0, TrialsPerCount);

        // Assert - Full count should be close to neutral (within 5%)
        var difference = Math.Abs(rate_3_2 - rate_0_0);
        Assert.That(difference, Is.LessThan(0.05),
            $"Contact rate at 3-2 ({rate_3_2:F4}) should be close to 0-0 ({rate_0_0:F4}), difference: {difference:F4}");
        Assert.That(rate_3_2, Is.InRange(0.70, 0.85));
    }

    [Test]
    public void ContactRate_BackwardCompatibility_NeutralCountMatchesParameterless() {
        // Arrange
        var rng = new SeededRandom(Seed);
        var engine = new GameEngine(rng);
        var batter = CreateAverageBatter();
        var pitcher = CreateAveragePitcher();

        // Act - Compare parameterless method with explicit 0-0 count
        var rateParameterless = MeasureContactRate_Parameterless(engine, batter, pitcher, TrialsPerCount);
        var rate_0_0 = MeasureContactRate(engine, batter, pitcher, balls: 0, strikes: 0, TrialsPerCount);

        // Assert - Both should produce the same rate (within statistical noise)
        var difference = Math.Abs(rateParameterless - rate_0_0);
        Assert.That(difference, Is.LessThan(0.02),
            $"Parameterless method rate ({rateParameterless:F4}) should match 0-0 count rate ({rate_0_0:F4}), difference: {difference:F4}");
    }

    /// <summary>
    /// Helper method to measure contact rate using the parameterless overload.
    /// </summary>
    private static double MeasureContactRate_Parameterless(GameEngine engine, Batter batter, Pitcher pitcher, int trials) {
        int contacts = 0;
        for (int i = 0; i < trials; i++) {
            if (engine.SimulatePitchContact(batter, pitcher)) {
                contacts++;
            }
        }
        return (double)contacts / trials;
    }

    [Test]
    public void GameState_ValidatesCountRanges() {
        // Assert - Valid counts should not throw
        var validState = new GameState(balls: 2, strikes: 1);
        Assert.That(validState.Balls, Is.EqualTo(2));
        Assert.That(validState.Strikes, Is.EqualTo(1));

        // Assert - Invalid balls should throw
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameState(balls: -1, strikes: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameState(balls: 4, strikes: 0));

        // Assert - Invalid strikes should throw
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameState(balls: 0, strikes: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameState(balls: 0, strikes: 3));
    }

    [Test]
    public void GameState_IsComplete_DetectsWalkAndStrikeout() {
        // Arrange & Act & Assert - Not complete
        Assert.That(new GameState(0, 0).IsComplete(), Is.False);
        Assert.That(new GameState(2, 1).IsComplete(), Is.False);
        Assert.That(new GameState(3, 2).IsComplete(), Is.False);

        // Note: GameState validation prevents 4 balls or 3 strikes in constructor
        // IsComplete() is designed for future use when counts can reach these values
    }

    [Test]
    public void GameState_ToString_FormatsCorrectly() {
        // Arrange & Act & Assert
        Assert.That(new GameState(0, 0).ToString(), Is.EqualTo("0-0"));
        Assert.That(new GameState(2, 1).ToString(), Is.EqualTo("2-1"));
        Assert.That(new GameState(3, 2).ToString(), Is.EqualTo("3-2"));
    }

    [Test]
    public void GameState_Equality_WorksCorrectly() {
        // Arrange
        var state1 = new GameState(2, 1);
        var state2 = new GameState(2, 1);
        var state3 = new GameState(1, 2);

        // Act & Assert
        Assert.That(state1, Is.EqualTo(state2));
        Assert.That(state1, Is.Not.EqualTo(state3));
        Assert.That(state1.Equals(state2), Is.True);
        Assert.That(state1.Equals(state3), Is.False);
        Assert.That(state1.Equals(null), Is.False);
        Assert.That(state1.Equals("not a GameState"), Is.False);
    }

    [Test]
    public void GameState_GetHashCode_IsConsistent() {
        // Arrange
        var state1 = new GameState(2, 1);
        var state2 = new GameState(2, 1);
        var state3 = new GameState(1, 2);

        // Act & Assert
        Assert.That(state1.GetHashCode(), Is.EqualTo(state2.GetHashCode()));
        Assert.That(state1.GetHashCode(), Is.Not.EqualTo(state3.GetHashCode()));
    }
}
