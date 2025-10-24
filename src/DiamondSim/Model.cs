namespace DiamondSim;

public sealed record BatterRatings(
    int Contact,  // 0–100
    int Power,
    int Patience,
    int Speed
) {
    public static BatterRatings Average => new(50, 50, 50, 50);
}

public sealed record PitcherRatings(
    int Control,  // 0–100
    int Stuff,
    int Stamina,
    int Speed
) {
    public static PitcherRatings Average => new(50, 50, 50, 50);
}

public sealed record Batter(string Name, BatterRatings Ratings);
public sealed record Pitcher(string Name, PitcherRatings Ratings);

public sealed record AtBatOutcomes(int Trials, int Contacts) {
    public double ContactRate => Trials == 0 ? 0.0 : (double)Contacts / Trials;
}
