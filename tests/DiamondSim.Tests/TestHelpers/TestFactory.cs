namespace DiamondSim.Tests.TestHelpers;

/// <summary>
/// Factory methods for creating common test fixtures.
/// </summary>
public static class TestFactory {
    /// <summary>
    /// Creates a balanced batter with average ratings (50/50/50/50).
    /// </summary>
    public static Batter CreateAverageBatter() {
        return new Batter("Average Joe", BatterRatings.Average);
    }

    /// <summary>
    /// Creates a balanced pitcher with average ratings (50/50/50/50).
    /// </summary>
    public static Pitcher CreateAveragePitcher() {
        return new Pitcher("Average Pete", PitcherRatings.Average);
    }

    /// <summary>
    /// Creates an elite batter with high contact rating (70/50/50/50).
    /// Expected to make contact ~4% more often than average.
    /// </summary>
    public static Batter CreateEliteBatter() {
        return new Batter("Elite Eddie", BatterRatings.Elite);
    }

    /// <summary>
    /// Creates a poor batter with low contact rating (30/50/50/50).
    /// Expected to make contact ~4% less often than average.
    /// </summary>
    public static Batter CreatePoorBatter() {
        return new Batter("Poor Pete", BatterRatings.Poor);
    }

    /// <summary>
    /// Creates an elite pitcher with high stuff rating (50/70/50/50).
    /// Expected to induce ~4% less contact than average.
    /// </summary>
    public static Pitcher CreateElitePitcher() {
        return new Pitcher("Elite Ace", PitcherRatings.Elite);
    }

    /// <summary>
    /// Creates a poor pitcher with low stuff rating (50/30/50/50).
    /// Expected to allow ~4% more contact than average.
    /// </summary>
    public static Pitcher CreatePoorPitcher() {
        return new Pitcher("Poor Paul", PitcherRatings.Poor);
    }
}
