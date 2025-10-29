using System.Diagnostics;

namespace DiamondSim;

/// <summary>
/// CLI entry point for the single-game simulator.
/// Parses --home, --away, --seed arguments and runs the game.
/// Exit codes: 0=success, 2=bad args, 1=unexpected error.
/// </summary>
public static class Program {
    public static int Main(string[] args) {
        try {
            var parsedArgs = ParseArguments(args);

            if (!parsedArgs.IsValid) {
                PrintUsage();
                return 2;
            }

            // Generate seed if not provided
            int seed = parsedArgs.Seed ?? GenerateRandomSeed();

            // Run the simulation
            var simulator = new GameSimulator(
                parsedArgs.HomeTeam!,
                parsedArgs.AwayTeam!,
                seed
            );

            var report = simulator.RunGame();

            // Print to STDOUT
            Console.WriteLine(report);

            return 0;
        }
        catch (Exception ex) {
            Console.Error.WriteLine($"Unexpected error: {ex.Message}");
            return 1;
        }
    }

    private static ParsedArguments ParseArguments(string[] args) {
        string? homeTeam = null;
        string? awayTeam = null;
        int? seed = null;

        for (int i = 0; i < args.Length; i++) {
            switch (args[i]) {
                case "--home":
                    if (i + 1 < args.Length) {
                        homeTeam = args[++i];
                    }
                    else {
                        return new ParsedArguments { IsValid = false };
                    }
                    break;

                case "--away":
                    if (i + 1 < args.Length) {
                        awayTeam = args[++i];
                    }
                    else {
                        return new ParsedArguments { IsValid = false };
                    }
                    break;

                case "--seed":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int parsedSeed)) {
                        seed = parsedSeed;
                    }
                    else {
                        return new ParsedArguments { IsValid = false };
                    }
                    break;

                default:
                    // Unknown flag
                    return new ParsedArguments { IsValid = false };
            }
        }

        // Validate required arguments
        bool isValid = !string.IsNullOrWhiteSpace(homeTeam) &&
                       !string.IsNullOrWhiteSpace(awayTeam);

        return new ParsedArguments {
            IsValid = isValid,
            HomeTeam = homeTeam,
            AwayTeam = awayTeam,
            Seed = seed
        };
    }

    private static int GenerateRandomSeed() {
        // Use a combination of timestamp and random for better entropy
        return Environment.TickCount ^ Random.Shared.Next();
    }

    private static void PrintUsage() {
        Console.Error.WriteLine("Usage: DiamondSim --home <name> --away <name> [--seed <int>]");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Required arguments:");
        Console.Error.WriteLine("  --home <name>    Home team display name");
        Console.Error.WriteLine("  --away <name>    Away team display name");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Optional arguments:");
        Console.Error.WriteLine("  --seed <int>     RNG seed for deterministic simulation");
        Console.Error.WriteLine("                   If omitted, a random seed will be generated");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Game settings:");
        Console.Error.WriteLine("  DH: ON | Extras: OFF (tie allowed)");
    }

    private sealed class ParsedArguments {
        public bool IsValid { get; init; }
        public string? HomeTeam { get; init; }
        public string? AwayTeam { get; init; }
        public int? Seed { get; init; }
    }
}
