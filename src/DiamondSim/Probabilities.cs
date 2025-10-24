namespace DiamondSim;

public static class Probabilities {
    // Base contact chance for average vs. average at 0–0 count
    public const double BaseContact = 0.78; // MLB contact ~75–80%

    // Rating sensitivities relative to 50 = average
    private const double ContactPerBatterPoint = 0.0020; // +5 pts => +1% contact
    private const double ContactPerPitcherStuffPoint = -0.0020; // pitcher Stuff lowers contact

    public static double ContactFromRatings(BatterRatings bat, PitcherRatings pit) {
        var batterDelta = (bat.Contact - 50) * ContactPerBatterPoint;
        var pitcherDelta = (pit.Stuff - 50) * ContactPerPitcherStuffPoint;
        return Clamp01(BaseContact + batterDelta + pitcherDelta);
    }

    public static double CountContactAdjust(int balls, int strikes) {
        return (balls, strikes) switch {
            (0, 0) => 0.00,
            (0, 1) => -0.03,
            (0, 2) => -0.12,
            (1, 0) => +0.02,
            (2, 0) => +0.05,
            (3, 0) => +0.08,
            (3, 2) => -0.03,
            _ => 0.00
        };
    }

    private static double Clamp01(double p) {
        if (p < 0) return 0;
        if (p > 1) return 1;
        return p;
    }
}
