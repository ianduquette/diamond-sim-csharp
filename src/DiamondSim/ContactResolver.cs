namespace DiamondSim;

/// <summary>
/// Default implementation of contact resolution using rating-based probabilities
/// with count-based adjustments.
/// </summary>
public class ContactResolver : IContactResolver {
    private readonly IRandomSource _random;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContactResolver"/> class.
    /// </summary>
    /// <param name="random">The random number generator to use for probabilistic decisions.</param>
    public ContactResolver(IRandomSource random) {
        _random = random;
    }

    /// <summary>
    /// Determines whether the batter makes contact on a swing based on ratings and count.
    /// </summary>
    /// <param name="batter">The batter's ratings.</param>
    /// <param name="pitcher">The pitcher's ratings.</param>
    /// <param name="balls">The number of balls in the count (0-3).</param>
    /// <param name="strikes">The number of strikes in the count (0-2).</param>
    /// <returns>True if contact is made; false if the batter whiffs.</returns>
    public bool MakesContact(BatterRatings batter, PitcherRatings pitcher, int balls, int strikes) {
        var baseContact = Probabilities.ContactFromRatings(batter, pitcher);
        var countAdjust = Probabilities.CountContactAdjust(balls, strikes);
        var contactProb = Clamp01(baseContact + countAdjust);

        return _random.NextDouble() < contactProb;
    }

    /// <summary>
    /// Clamps a probability value to the range [0, 1].
    /// </summary>
    private static double Clamp01(double value) {
        if (value < 0) return 0;
        if (value > 1) return 1;
        return value;
    }
}
