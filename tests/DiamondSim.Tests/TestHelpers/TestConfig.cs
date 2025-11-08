namespace DiamondSim.Tests.TestHelpers;

public static class TestConfig {

    // Default number of simulations for probability tests
    public static readonly int SIM_DEFAULT_N =
        int.TryParse(Environment.GetEnvironmentVariable("DIAMONDSIM_SIM_N"), out var n)
            ? n
            : 25_000; // baseline default for local runs

    // MLB Statistical Bands for Ball-in-Play (BIP) Outcomes
    // These ranges represent typical MLB statistics for average vs. average matchups
    // and are used to validate that simulated distributions are realistic.
    //
    // Note: All percentages are of BIP (balls in play), HR included in the partition;
    // BABIP excludes HR by definition.

    // Home Run percentage of all BIP (3-5%)
    public const double MlbBipHrMin = 0.03;
    public const double MlbBipHrMax = 0.05;

    // Double percentage of all BIP (5-7%)
    public const double MlbBipDoubleMin = 0.05;
    public const double MlbBipDoubleMax = 0.07;

    // Triple percentage of all BIP (0.3-0.5%, ~14 triples per 162-game season)
    public const double MlbBipTripleMin = 0.003;
    public const double MlbBipTripleMax = 0.005;

    // Single percentage of all BIP (18-21%)
    public const double MlbBipSingleMin = 0.18;
    public const double MlbBipSingleMax = 0.21;

    // BABIP (Batting Average on Balls In Play) - excludes HRs (0.26-0.32, typically 0.29 ± 0.03)
    public const double MlbBabipMin = 0.26;
    public const double MlbBabipMax = 0.32;
}
