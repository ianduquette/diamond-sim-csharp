namespace DiamondSim;

public sealed record BatterRatings(
    int Contact,  // 0–100
    int Power,
    int Patience,
    int Speed
) {
    public static BatterRatings Average => new(50, 50, 50, 50);
    public static BatterRatings Elite => new(70, 50, 50, 50);
    public static BatterRatings Poor => new(30, 50, 50, 50);
}

public sealed record PitcherRatings(
    int Control,  // 0–100
    int Stuff,
    int Stamina,
    int Speed
) {
    public static PitcherRatings Average => new(50, 50, 50, 50);
    public static PitcherRatings Elite => new(50, 70, 50, 50);
    public static PitcherRatings Poor => new(50, 30, 50, 50);
}

public sealed record Batter(string Name, BatterRatings Ratings);
public sealed record Pitcher(string Name, PitcherRatings Ratings);
