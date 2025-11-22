using DiamondSim.Tests.TestHelpers;

namespace DiamondSim.Tests.Probabilities;

/// <summary>
/// Tests for BaseRunnerAdvancement logic using probabilistic sampling.
/// Validates that runner advancement rules follow MLB rules for various scenarios.
/// </summary>
[TestFixture]
public class BaseRunnerAdvancementTests {
    private const int Seed = 99999;
    private static readonly int Trials = TestConfig.SIM_DEFAULT_N;

    #region Double Play Tests

    /// <summary>
    /// Validates that double plays occur on ground balls with runner on first.
    /// Expected DP rate: 10-30% based on MLB statistics (15% in code).
    /// </summary>
    [Test]
    public void DoublePlay_OccursOnGroundBalls() {
        // Arrange
        var bases = new BaseState(OnFirst: true, OnSecond: false, OnThird: false);

        // Act
        var results = ExecuteSut(AtBatTerminal.BallInPlay, BipOutcome.Out, BipType.GroundBall, bases);

        // Assert
        Assert.That(results.DoublePlayCount, Is.GreaterThan(0), "DPs should occur on ground balls");
        Assert.That(results.DoublePlayRate, Is.InRange(0.10, 0.30),
            $"DP rate should be 10-30% on ground balls with R1, got {results.DoublePlayRate:P2}");

        // Output for debugging
        TestContext.Out.WriteLine($"DPs on ground balls: {results.DoublePlayCount}/{Trials} ({results.DoublePlayRate:P2})");
    }

    /// <summary>
    /// Validates that double plays NEVER occur on fly balls.
    /// MLB Rule: Fly ball outs cannot result in double plays via force plays.
    /// </summary>
    [Test]
    public void DoublePlay_NeverOccursOnFlyBalls() {
        // Arrange
        var bases = new BaseState(OnFirst: true, OnSecond: false, OnThird: false);

        // Act
        var results = ExecuteSut(AtBatTerminal.BallInPlay, BipOutcome.Out, BipType.FlyBall, bases);

        // Assert
        Assert.That(results.DoublePlayCount, Is.EqualTo(0), "DPs should NEVER occur on fly balls");

        // Output for debugging
        TestContext.Out.WriteLine($"DPs on fly balls: {results.DoublePlayCount}/{Trials} (should be 0)");
    }

    /// <summary>
    /// Validates that double plays NEVER occur on line drives.
    /// MLB Rule: Line drive outs cannot result in double plays via force plays.
    /// </summary>
    [Test]
    public void DoublePlay_NeverOccursOnLineDrives() {
        // Arrange
        var bases = new BaseState(OnFirst: true, OnSecond: false, OnThird: false);

        // Act
        var results = ExecuteSut(AtBatTerminal.BallInPlay, BipOutcome.Out, BipType.LineDrive, bases);

        // Assert
        Assert.That(results.DoublePlayCount, Is.EqualTo(0), "DPs should NEVER occur on line drives");

        // Output for debugging
        TestContext.Out.WriteLine($"DPs on line drives: {results.DoublePlayCount}/{Trials} (should be 0)");
    }

    #endregion

    #region Sacrifice Fly Tests

    /// <summary>
    /// Validates that sacrifice flies occur with runner on third and less than 2 outs.
    /// Expected SF rate: ~30% of flyouts with R3 (per code).
    /// </summary>
    [Test]
    public void SacrificeFly_OccursWithRunnerOnThird() {
        // Arrange
        var bases = new BaseState(OnFirst: false, OnSecond: false, OnThird: true);

        // Act
        var results = ExecuteSut(AtBatTerminal.BallInPlay, BipOutcome.Out, BipType.FlyBall, bases);

        // Assert
        Assert.That(results.SacrificeFlyCount, Is.GreaterThan(0), "Sacrifice flies should occur with R3");
        Assert.That(results.SacrificeFlyRate, Is.InRange(0.20, 0.40),
            $"SF rate should be 20-40% of flyouts with R3, got {results.SacrificeFlyRate:P2}");

        // Output for debugging
        TestContext.Out.WriteLine($"Sacrifice flies: {results.SacrificeFlyCount}/{Trials} ({results.SacrificeFlyRate:P2})");
    }

    /// <summary>
    /// Validates that runner on third scores on sacrifice fly.
    /// MLB Rule: R3 must tag up and can advance home on flyout.
    /// </summary>
    [Test]
    public void SacrificeFly_RunnerOnThird_Scores() {
        // Arrange
        var bases = new BaseState(OnFirst: false, OnSecond: false, OnThird: true);

        // Act
        var results = ExecuteSut(AtBatTerminal.BallInPlay, BipOutcome.Out, BipType.FlyBall, bases);

        // Assert
        Assert.That(results.SacrificeFlyCount, Is.GreaterThan(0), "Should have sacrifice flies");
        Assert.That(results.SacrificeFlyRuns, Is.EqualTo(results.SacrificeFlyCount),
            "Every sacrifice fly should score exactly 1 run (R3)");

        // Output for debugging
        TestContext.Out.WriteLine($"Sacrifice flies: {results.SacrificeFlyCount}, Runs scored: {results.SacrificeFlyRuns}");
    }

    /// <summary>
    /// Validates that sacrifice fly awards 1 RBI to the batter.
    /// MLB Rule: SF counts as 1 RBI.
    /// </summary>
    [Test]
    public void SacrificeFly_Awards_OneRbi() {
        // Arrange
        var bases = new BaseState(OnFirst: false, OnSecond: false, OnThird: true);

        // Act
        var results = ExecuteSut(AtBatTerminal.BallInPlay, BipOutcome.Out, BipType.FlyBall, bases);

        // Assert
        Assert.That(results.SacrificeFlyCount, Is.GreaterThan(0), "Should have sacrifice flies");
        Assert.That(results.SacrificeFlyRbi, Is.EqualTo(results.SacrificeFlyCount),
            "Every sacrifice fly should award exactly 1 RBI");

        // Output for debugging
        TestContext.Out.WriteLine($"Sacrifice flies: {results.SacrificeFlyCount}, Total RBI: {results.SacrificeFlyRbi}");
    }

    #endregion

    #region Single Advancement Tests

    /// <summary>
    /// Validates that runner on third always scores on a single.
    /// MLB Rule: R3 scores on any hit.
    /// </summary>
    [Test]
    public void Single_RunnerOnThird_AlwaysScores() {
        // Arrange
        var bases = new BaseState(OnFirst: false, OnSecond: false, OnThird: true);

        // Act
        var results = ExecuteSut(AtBatTerminal.BallInPlay, BipOutcome.Single, BipType.GroundBall, bases);

        // Assert
        Assert.That(results.TotalRuns, Is.EqualTo(Trials),
            "R3 should score on every single");

        // Output for debugging
        TestContext.Out.WriteLine($"Singles: {Trials}, R3 scored: {results.TotalRuns}");
    }

    /// <summary>
    /// Validates that runner on second advances to third on a single.
    /// MLB Rule: R2 advances to third on single (simplified v1 logic).
    /// </summary>
    [Test]
    public void Single_RunnerOnSecond_AdvancesToThird() {
        // Arrange
        var bases = new BaseState(OnFirst: false, OnSecond: true, OnThird: false);

        // Act
        var results = ExecuteSut(AtBatTerminal.BallInPlay, BipOutcome.Single, BipType.GroundBall, bases);

        // Assert
        Assert.That(results.RunnerOnThirdCount, Is.EqualTo(Trials),
            "R2 should advance to third on every single (v1 logic)");

        // Output for debugging
        TestContext.Out.WriteLine($"Singles: {Trials}, R2 to third: {results.RunnerOnThirdCount}");
    }

    /// <summary>
    /// Validates that runner on first advances to second on a single.
    /// MLB Rule: R1 advances to second on single.
    /// </summary>
    [Test]
    public void Single_RunnerOnFirst_AdvancesToSecond() {
        // Arrange
        var bases = new BaseState(OnFirst: true, OnSecond: false, OnThird: false);

        // Act
        var results = ExecuteSut(AtBatTerminal.BallInPlay, BipOutcome.Single, BipType.GroundBall, bases);

        // Assert
        Assert.That(results.RunnerOnSecondCount, Is.EqualTo(Trials),
            "R1 should advance to second on every single");

        // Output for debugging
        TestContext.Out.WriteLine($"Singles: {Trials}, R1 to second: {results.RunnerOnSecondCount}");
    }

    #endregion

    #region Double Advancement Tests

    /// <summary>
    /// Validates that runner on third scores on a double.
    /// MLB Rule: R3 scores on any extra-base hit.
    /// </summary>
    [Test]
    public void Double_RunnerOnThird_Scores() {
        // Arrange
        var bases = new BaseState(OnFirst: false, OnSecond: false, OnThird: true);

        // Act
        var results = ExecuteSut(AtBatTerminal.BallInPlay, BipOutcome.Double, BipType.GroundBall, bases);

        // Assert
        Assert.That(results.TotalRuns, Is.EqualTo(Trials),
            "R3 should score on every double");

        // Output for debugging
        TestContext.Out.WriteLine($"Doubles: {Trials}, R3 scored: {results.TotalRuns}");
    }

    /// <summary>
    /// Validates that runner on second scores on a double.
    /// MLB Rule: R2 typically scores on doubles.
    /// </summary>
    [Test]
    public void Double_RunnerOnSecond_Scores() {
        // Arrange
        var bases = new BaseState(OnFirst: false, OnSecond: true, OnThird: false);

        // Act
        var results = ExecuteSut(AtBatTerminal.BallInPlay, BipOutcome.Double, BipType.GroundBall, bases);

        // Assert
        Assert.That(results.TotalRuns, Is.EqualTo(Trials),
            "R2 should score on every double (v1 logic)");

        // Output for debugging
        TestContext.Out.WriteLine($"Doubles: {Trials}, R2 scored: {results.TotalRuns}");
    }

    /// <summary>
    /// Validates that runner on first advances to third on a double.
    /// MLB Rule: R1 advances to third on double.
    /// </summary>
    [Test]
    public void Double_RunnerOnFirst_AdvancesToThird() {
        // Arrange
        var bases = new BaseState(OnFirst: true, OnSecond: false, OnThird: false);

        // Act
        var results = ExecuteSut(AtBatTerminal.BallInPlay, BipOutcome.Double, BipType.GroundBall, bases);

        // Assert
        Assert.That(results.RunnerOnThirdCount, Is.EqualTo(Trials),
            "R1 should advance to third on every double");

        // Output for debugging
        TestContext.Out.WriteLine($"Doubles: {Trials}, R1 to third: {results.RunnerOnThirdCount}");
    }

    #endregion

    #region Triple Advancement Tests

    /// <summary>
    /// Validates that all runners score on a triple.
    /// MLB Rule: All runners score on triples.
    /// </summary>
    [Test]
    public void Triple_AllRunnersScore() {
        // Arrange
        var bases = new BaseState(OnFirst: true, OnSecond: true, OnThird: true);

        // Act
        var results = ExecuteSut(AtBatTerminal.BallInPlay, BipOutcome.Triple, BipType.GroundBall, bases);

        // Assert
        var expectedRuns = Trials * 3;
        Assert.That(results.TotalRuns, Is.EqualTo(expectedRuns),
            "All 3 runners should score on every triple with bases loaded");

        // Output for debugging
        TestContext.Out.WriteLine($"Triples: {Trials}, Total runs: {results.TotalRuns} (expected: {expectedRuns})");
    }

    #endregion

    #region Home Run Tests

    /// <summary>
    /// Validates that home run clears all bases and scores all runners plus batter.
    /// MLB Rule: HR scores batter + all runners.
    /// </summary>
    [Test]
    public void HomeRun_ScoresAllRunnersAndBatter() {
        // Arrange
        var bases = new BaseState(OnFirst: true, OnSecond: true, OnThird: true);

        // Act
        var results = ExecuteSut(AtBatTerminal.BallInPlay, BipOutcome.HomeRun, BipType.FlyBall, bases);

        // Assert
        var expectedRuns = Trials * 4;
        Assert.That(results.TotalRuns, Is.EqualTo(expectedRuns),
            "Grand slam should score 4 runs (batter + 3 runners)");

        // Output for debugging
        TestContext.Out.WriteLine($"Home runs: {Trials}, Total runs: {results.TotalRuns} (expected: {expectedRuns})");
    }

    /// <summary>
    /// Validates that home run leaves bases empty.
    /// MLB Rule: HR clears all bases.
    /// </summary>
    [Test]
    public void HomeRun_ClearsBases() {
        // Arrange
        var bases = new BaseState(OnFirst: true, OnSecond: true, OnThird: true);

        // Act
        var results = ExecuteSut(AtBatTerminal.BallInPlay, BipOutcome.HomeRun, BipType.FlyBall, bases);

        // Assert
        Assert.That(results.BasesEmptyCount, Is.EqualTo(Trials),
            "Every HR should leave bases empty");

        // Output for debugging
        TestContext.Out.WriteLine($"Home runs with bases empty after: {results.BasesEmptyCount}/{Trials}");
    }

    #endregion

    #region Reach On Error Tests

    /// <summary>
    /// Validates that reach on error (ROE) occurs at expected rate.
    /// Expected ROE rate: ~5% of outs (per code).
    /// </summary>
    [Test]
    public void ReachOnError_OccursAtExpectedRate() {
        // Arrange
        var bases = new BaseState(OnFirst: false, OnSecond: false, OnThird: false);

        // Act
        var results = ExecuteSut(AtBatTerminal.BallInPlay, BipOutcome.Out, BipType.GroundBall, bases);

        // Assert
        Assert.That(results.ReachOnErrorCount, Is.GreaterThan(0), "ROE should occur");
        Assert.That(results.ReachOnErrorRate, Is.InRange(0.03, 0.07),
            $"ROE rate should be 3-7% of outs, got {results.ReachOnErrorRate:P2}");

        // Output for debugging
        TestContext.Out.WriteLine($"Reach on error: {results.ReachOnErrorCount}/{Trials} ({results.ReachOnErrorRate:P2})");
    }

    /// <summary>
    /// Validates that ROE advances all runners one base.
    /// MLB Rule: Error allows batter to reach and runners to advance.
    /// </summary>
    [Test]
    public void ReachOnError_AdvancesAllRunners() {
        // Arrange
        var bases = new BaseState(OnFirst: false, OnSecond: false, OnThird: true);

        // Act
        var results = ExecuteSut(AtBatTerminal.BallInPlay, BipOutcome.Out, BipType.GroundBall, bases);

        // Assert
        Assert.That(results.ReachOnErrorCount, Is.GreaterThan(0), "Should have ROE");
        Assert.That(results.ReachOnErrorRuns, Is.EqualTo(results.ReachOnErrorCount),
            "R3 should score on every ROE (v1 logic: runners advance one base)");

        // Output for debugging
        TestContext.Out.WriteLine($"ROE: {results.ReachOnErrorCount}, R3 scored: {results.ReachOnErrorRuns}");
    }

    /// <summary>
    /// Validates that ROE awards 0 RBI.
    /// MLB Rule: Errors do not count as RBI.
    /// </summary>
    [Test]
    public void ReachOnError_AwardsZeroRbi() {
        // Arrange
        var bases = new BaseState(OnFirst: false, OnSecond: false, OnThird: true);

        // Act
        var results = ExecuteSut(AtBatTerminal.BallInPlay, BipOutcome.Out, BipType.GroundBall, bases);

        // Assert
        Assert.That(results.ReachOnErrorCount, Is.GreaterThan(0), "Should have ROE");
        Assert.That(results.ReachOnErrorRbi, Is.EqualTo(0),
            "ROE should never award RBI, even if runs score");

        // Output for debugging
        TestContext.Out.WriteLine($"ROE: {results.ReachOnErrorCount}, Total RBI: {results.ReachOnErrorRbi} (should be 0)");
    }

    #endregion

    #region Bases Loaded Scenarios

    /// <summary>
    /// Validates that bases-loaded walk forces in a run.
    /// MLB Rule: Walk with bases loaded forces R3 home.
    /// </summary>
    [Test]
    public void BasesLoaded_Walk_ForcesRunHome() {
        // Arrange
        var bases = new BaseState(OnFirst: true, OnSecond: true, OnThird: true);

        // Act
        var results = ExecuteSut(AtBatTerminal.Walk, null, null, bases);

        // Assert
        Assert.That(results.TotalRuns, Is.EqualTo(Trials),
            "Every bases-loaded walk should force in exactly 1 run");

        // Output for debugging
        TestContext.Out.WriteLine($"Bases-loaded walks: {Trials}, Runs scored: {results.TotalRuns}");
    }

    /// <summary>
    /// Validates that bases-loaded HBP forces in a run.
    /// MLB Rule: HBP with bases loaded forces R3 home.
    /// </summary>
    [Test]
    public void BasesLoaded_HitByPitch_ForcesRunHome() {
        // Arrange
        var bases = new BaseState(OnFirst: true, OnSecond: true, OnThird: true);

        // Act
        var results = ExecuteSut(AtBatTerminal.HitByPitch, null, null, bases);

        // Assert
        Assert.That(results.TotalRuns, Is.EqualTo(Trials),
            "Every bases-loaded HBP should force in exactly 1 run");

        // Output for debugging
        TestContext.Out.WriteLine($"Bases-loaded HBPs: {Trials}, Runs scored: {results.TotalRuns}");
    }

    #endregion

    /// <summary>
    /// Executes the System Under Test (SUT) - runs trials and collects comprehensive results.
    /// </summary>
    private static AdvancementResults ExecuteSut(
        AtBatTerminal terminal,
        BipOutcome? bipOutcome,
        BipType? bipType,
        BaseState bases) {

        var results = new AdvancementResults();
        var rng = new SeededRandom(Seed);
        var advancement = new BaseRunnerAdvancement();
        const int currentOuts = 1;

        for (var i = 0; i < Trials; i++) {
            var resolution = advancement.Resolve(terminal, bipOutcome, bipType, bases, currentOuts, rng);
            results.Record(resolution);
        }

        return results;
    }

    /// <summary>
    /// Helper class to hold advancement test results.
    /// </summary>
    private class AdvancementResults {
        public int DoublePlayCount { get; private set; }
        public int SacrificeFlyCount { get; private set; }
        public int SacrificeFlyRuns { get; private set; }
        public int SacrificeFlyRbi { get; private set; }
        public int ReachOnErrorCount { get; private set; }
        public int ReachOnErrorRuns { get; private set; }
        public int ReachOnErrorRbi { get; private set; }
        public int TotalRuns { get; private set; }
        public int RunnerOnSecondCount { get; private set; }
        public int RunnerOnThirdCount { get; private set; }
        public int BasesEmptyCount { get; private set; }

        public double DoublePlayRate => (double)DoublePlayCount / Trials;
        public double SacrificeFlyRate => (double)SacrificeFlyCount / Trials;
        public double ReachOnErrorRate => (double)ReachOnErrorCount / Trials;

        /// <summary>
        /// Records a plate appearance resolution, updating internal counts.
        /// </summary>
        public void Record(PaResolution resolution) {
            // Track double plays
            if (resolution.Flags?.IsDoublePlay ?? false) {
                DoublePlayCount++;
            }

            // Track sacrifice flies
            if (resolution.Flags?.IsSacFly ?? false) {
                SacrificeFlyCount++;
                SacrificeFlyRuns += resolution.RunsScored;
                SacrificeFlyRbi += resolution.RbiForBatter;
            }

            // Track reach on error
            if (resolution.HadError) {
                ReachOnErrorCount++;
                ReachOnErrorRuns += resolution.RunsScored;
                ReachOnErrorRbi += resolution.RbiForBatter;
            }

            // Track runs
            TotalRuns += resolution.RunsScored;

            // Track base states
            if (resolution.NewBases.OnSecond) {
                RunnerOnSecondCount++;
            }
            if (resolution.NewBases.OnThird) {
                RunnerOnThirdCount++;
            }
            if (resolution.NewBases is { OnFirst: false, OnSecond: false } && !resolution.NewBases.OnThird) {
                BasesEmptyCount++;
            }
        }
    }
}
