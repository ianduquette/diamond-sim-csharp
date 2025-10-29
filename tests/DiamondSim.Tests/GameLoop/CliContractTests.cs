using System.Diagnostics;

namespace DiamondSim.Tests.GameLoop;

/// <summary>
/// Tests for CLI contract - argument parsing, exit codes, and error handling.
/// </summary>
[TestFixture]
public class CliContractTests {
    private string _exePath = "";

    [SetUp]
    public void Setup() {
        // Determine the path to the DiamondSim executable
        // This assumes tests run from the test project's output directory
        var testDir = TestContext.CurrentContext.TestDirectory;
        var solutionDir = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", ".."));
        _exePath = Path.Combine(solutionDir, "src", "DiamondSim", "bin", "Debug", "net8.0", "DiamondSim.exe");

        // On non-Windows, use dll with dotnet
        if (!OperatingSystem.IsWindows()) {
            _exePath = Path.Combine(solutionDir, "src", "DiamondSim", "bin", "Debug", "net8.0", "DiamondSim.dll");
        }
    }

    [Test]
    public void Cli_WithValidArguments_ExitsZero() {
        // Arrange
        var args = "--home Sharks --away Comets --seed 42";

        // Act
        var (exitCode, stdout, stderr) = RunCli(args);

        // Assert
        Assert.That(exitCode, Is.EqualTo(0), $"Expected exit code 0, got {exitCode}. STDERR: {stderr}");
        Assert.That(stdout, Is.Not.Empty);
        Assert.That(stdout, Does.Contain("Sharks"));
        Assert.That(stdout, Does.Contain("Comets"));
        Assert.That(stdout, Does.Contain("Seed: 42"));
    }

    [Test]
    public void Cli_MissingHomeArgument_ExitsTwo() {
        // Arrange
        var args = "--away Comets --seed 42";

        // Act
        var (exitCode, stdout, stderr) = RunCli(args);

        // Assert
        Assert.That(exitCode, Is.EqualTo(2), $"Expected exit code 2 for missing --home");
        Assert.That(stderr, Does.Contain("Usage:"));
    }

    [Test]
    public void Cli_MissingAwayArgument_ExitsTwo() {
        // Arrange
        var args = "--home Sharks --seed 42";

        // Act
        var (exitCode, stdout, stderr) = RunCli(args);

        // Assert
        Assert.That(exitCode, Is.EqualTo(2), $"Expected exit code 2 for missing --away");
        Assert.That(stderr, Does.Contain("Usage:"));
    }

    [Test]
    public void Cli_UnknownFlag_ExitsTwo() {
        // Arrange
        var args = "--home Sharks --away Comets --unknown flag";

        // Act
        var (exitCode, stdout, stderr) = RunCli(args);

        // Assert
        Assert.That(exitCode, Is.EqualTo(2), $"Expected exit code 2 for unknown flag");
        Assert.That(stderr, Does.Contain("Usage:"));
    }

    [Test]
    public void Cli_InvalidSeedValue_ExitsTwo() {
        // Arrange
        var args = "--home Sharks --away Comets --seed notanumber";

        // Act
        var (exitCode, stdout, stderr) = RunCli(args);

        // Assert
        Assert.That(exitCode, Is.EqualTo(2), $"Expected exit code 2 for invalid seed");
        Assert.That(stderr, Does.Contain("Usage:"));
    }

    [Test]
    public void Cli_WithoutSeed_GeneratesRandomSeed() {
        // Arrange
        var args = "--home Sharks --away Comets";

        // Act
        var (exitCode, stdout, stderr) = RunCli(args);

        // Assert
        Assert.That(exitCode, Is.EqualTo(0));
        Assert.That(stdout, Does.Contain("Seed:"));

        // Extract seed value - should be a number
        var lines = stdout.Split('\n');
        var seedLine = lines.FirstOrDefault(l => l.Contains("Seed:"));
        Assert.That(seedLine, Is.Not.Null);

        var seedMatch = System.Text.RegularExpressions.Regex.Match(seedLine!, @"Seed:\s*(-?\d+)");
        Assert.That(seedMatch.Success, Is.True, "Generated seed should be a valid integer");
    }

    [Test]
    public void Cli_UsageMessage_ContainsRequiredInfo() {
        // Arrange
        var args = ""; // No arguments

        // Act
        var (exitCode, stdout, stderr) = RunCli(args);

        // Assert
        Assert.That(exitCode, Is.EqualTo(2));
        Assert.That(stderr, Does.Contain("Usage:"));
        Assert.That(stderr, Does.Contain("--home"));
        Assert.That(stderr, Does.Contain("--away"));
        Assert.That(stderr, Does.Contain("--seed"));
        Assert.That(stderr, Does.Contain("DH: ON | Extras: OFF"));
    }

    [Test]
    public void Cli_TwoRunsWithSameSeed_ProduceIdenticalOutput() {
        // Arrange
        var args = "--home Sharks --away Comets --seed 42";

        // Act
        var (exitCode1, stdout1, stderr1) = RunCli(args);
        var (exitCode2, stdout2, stderr2) = RunCli(args);

        // Assert
        Assert.That(exitCode1, Is.EqualTo(0));
        Assert.That(exitCode2, Is.EqualTo(0));

        // Normalize output (remove timestamp line which is non-deterministic)
        var normalized1 = NormalizeOutput(stdout1);
        var normalized2 = NormalizeOutput(stdout2);

        Assert.That(normalized2, Is.EqualTo(normalized1),
            "Same seed should produce identical output (excluding timestamp)");
    }

    private (int exitCode, string stdout, string stderr) RunCli(string arguments) {
        var startInfo = new ProcessStartInfo {
            FileName = OperatingSystem.IsWindows() ? _exePath : "dotnet",
            Arguments = OperatingSystem.IsWindows() ? arguments : $"{_exePath} {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();

        process.WaitForExit();

        return (process.ExitCode, stdout, stderr);
    }

    private string NormalizeOutput(string output) {
        // Remove timestamp line (second line typically)
        var lines = output.Split('\n').ToList();
        if (lines.Count > 1) {
            // Remove line containing date/time (format: YYYY-MM-DD HH:MM)
            lines.RemoveAll(l => System.Text.RegularExpressions.Regex.IsMatch(l, @"\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}"));
        }
        return string.Join('\n', lines);
    }
}
