namespace DiamondSim.Tests.TestHelpers;

/// <summary>
/// Factory methods for creating common test fixtures.
/// </summary>
public static class TestFactory {
    /// <summary>
    /// Creates a balanced batter with average ratings.
    /// </summary>
    public static Batter CreateAverageBatter() {
        return new Batter("Average Joe", BatterRatings.Average);
    }

    /// <summary>
    /// Creates a balanced pitcher with average ratings.
    /// </summary>
    public static Pitcher CreateAveragePitcher() {
        return new Pitcher("Average Pete", PitcherRatings.Average);
    }
}
