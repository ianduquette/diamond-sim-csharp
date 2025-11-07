namespace DiamondSim.Tests.TestHelpers;

public static class TestConfig {

    // Default number of simulations for probability tests
    public static readonly int SIM_DEFAULT_N =
        int.TryParse(Environment.GetEnvironmentVariable("DIAMONDSIM_SIM_N"), out var n)
            ? n
            : 25_000; // baseline default for local runs
}
