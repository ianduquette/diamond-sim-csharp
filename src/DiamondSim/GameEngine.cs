namespace DiamondSim;

public sealed class GameEngine {
    private readonly IRandomSource _rng;

    public GameEngine(IRandomSource rng) {
        _rng = rng;
    }

    /// <summary>
    /// Returns true if the pitch results in contact (foul or ball-in-play),
    /// false if it's a whiff. Neutral 0–0 count for now.
    /// </summary>
    public bool SimulatePitchContact(Batter batter, Pitcher pitcher) {
        var p = Probabilities.ContactFromRatings(batter.Ratings, pitcher.Ratings);
        var roll = _rng.NextDouble();
        return roll < p;
    }

    /// <summary>
    /// Returns true if the pitch results in contact (foul or ball-in-play),
    /// false if it's a whiff. Applies count-based adjustment to contact probability.
    /// </summary>
    /// <param name="batter">The batter in the at-bat.</param>
    /// <param name="pitcher">The pitcher throwing the pitch.</param>
    /// <param name="balls">The number of balls in the count (0-3).</param>
    /// <param name="strikes">The number of strikes in the count (0-2).</param>
    /// <returns>True if contact is made; false if the batter whiffs.</returns>
    public bool SimulatePitchContact(Batter batter, Pitcher pitcher, int balls, int strikes) {
        var baseP = Probabilities.ContactFromRatings(batter.Ratings, pitcher.Ratings);
        var adjustment = Probabilities.CountContactAdjust(balls, strikes);
        var adjustedP = baseP + adjustment;
        // Clamp to [0, 1] range
        if (adjustedP < 0) adjustedP = 0;
        if (adjustedP > 1) adjustedP = 1;
        var roll = _rng.NextDouble();
        return roll < adjustedP;
    }

    /// <summary>
    /// Runs N independent pitch trials and returns aggregate contact rate.
    /// </summary>
    public AtBatOutcomes SimulateManyAtBats(Batter batter, Pitcher pitcher, int trials) {
        int contacts = 0;
        for (int i = 0; i < trials; i++) {
            if (SimulatePitchContact(batter, pitcher)) {
                contacts++;
            }
        }
        return new AtBatOutcomes(trials, contacts);
    }
}
