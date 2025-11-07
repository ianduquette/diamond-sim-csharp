namespace DiamondSim;

/// <summary>
/// Resolves whether a batter makes contact with a pitch based on ratings and count.
/// </summary>
public interface IContactResolver {
    /// <summary>
    /// Determines whether the batter makes contact on a swing.
    /// </summary>
    /// <param name="batter">The batter's ratings.</param>
    /// <param name="pitcher">The pitcher's ratings.</param>
    /// <param name="balls">The number of balls in the count (0-3).</param>
    /// <param name="strikes">The number of strikes in the count (0-2).</param>
    /// <returns>True if contact is made; false if the batter whiffs.</returns>
    bool MakesContact(BatterRatings batter, PitcherRatings pitcher, int balls, int strikes);
}
