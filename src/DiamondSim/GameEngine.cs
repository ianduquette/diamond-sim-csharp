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
