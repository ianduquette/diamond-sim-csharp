namespace DiamondSim;

/// <summary>
/// Default lineup generator that creates 9 average batters and shuffles them.
/// This maintains the original GameSimulator behavior.
/// </summary>
public class DefaultLineupGenerator : ILineupGenerator {
    /// <summary>
    /// Generates a lineup of 9 batters with average ratings, shuffled randomly.
    /// </summary>
    public List<Batter> GenerateLineup(string teamName, IRandomSource rng) {
        var batters = new List<Batter>();
        for (var i = 1; i <= 9; i++) {
            batters.Add(new Batter($"{teamName} {i}", BatterRatings.Average));
        }

        // Randomize order using Fisher-Yates shuffle
        Shuffle(batters, rng);

        return batters;
    }

    /// <summary>
    /// Fisher-Yates shuffle using the provided RNG for determinism.
    /// </summary>
    private void Shuffle<T>(List<T> list, IRandomSource rng) {
        var n = list.Count;
        for (var i = n - 1; i > 0; i--) {
            var j = (int)(rng.NextDouble() * (i + 1));
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
