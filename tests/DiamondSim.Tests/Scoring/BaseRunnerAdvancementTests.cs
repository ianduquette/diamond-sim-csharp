namespace DiamondSim.Tests.Scoring;

/// <summary>
/// Tests for BaseRunnerAdvancement logic, particularly around double plays
/// and other runner advancement scenarios.
/// </summary>
[TestFixture]
public class BaseRunnerAdvancementTests {
    /// <summary>
    /// Tests that double plays can only occur on ground ball outs, not fly balls or line drives.
    /// </summary>
    [Test]
    public void DoublePlay_Should_Only_Happen_On_Grounders() {
        // Arrange
        const int trials = 25_000;
        const int seed = 99999;
        var rng = new SeededRandom(seed);
        var advancement = new BaseRunnerAdvancement();

        // Situation: Runner on first, less than 2 outs (DP eligible)
        var basesWithR1 = new BaseState(OnFirst: true, OnSecond: false, OnThird: false);
        int currentOuts = 1;

        int dpOnGrounders = 0;
        int dpOnFlyBalls = 0;
        int dpOnLineDrives = 0;

        // Act: Test ground balls (should allow DP)
        for (int i = 0; i < trials; i++) {
            var resolution = advancement.Resolve(
                AtBatTerminal.BallInPlay,
                BipOutcome.Out,
                BipType.GroundBall,
                basesWithR1,
                currentOuts,
                rng
            );

            bool isDP = resolution.Flags?.IsDoublePlay ?? false;
            if (isDP) {
                dpOnGrounders++;
            }
        }

        // Act: Test fly balls (should NOT allow DP)
        for (int i = 0; i < trials; i++) {
            var resolution = advancement.Resolve(
                AtBatTerminal.BallInPlay,
                BipOutcome.Out,
                BipType.FlyBall,
                basesWithR1,
                currentOuts,
                rng
            );

            bool isDP = resolution.Flags?.IsDoublePlay ?? false;
            if (isDP) {
                dpOnFlyBalls++;
            }
        }

        // Act: Test line drives (should NOT allow DP)
        for (int i = 0; i < trials; i++) {
            var resolution = advancement.Resolve(
                AtBatTerminal.BallInPlay,
                BipOutcome.Out,
                BipType.LineDrive,
                basesWithR1,
                currentOuts,
                rng
            );

            bool isDP = resolution.Flags?.IsDoublePlay ?? false;
            if (isDP) {
                dpOnLineDrives++;
            }
        }

        // Assert: DPs should only occur on ground balls
        Assert.That(dpOnGrounders, Is.GreaterThan(0), "DPs should occur on ground balls");
        Assert.That(dpOnFlyBalls, Is.EqualTo(0), "DPs should NEVER occur on fly balls");
        Assert.That(dpOnLineDrives, Is.EqualTo(0), "DPs should NEVER occur on line drives");

        TestContext.Out.WriteLine($"DPs on ground balls: {dpOnGrounders}/{trials} ({(double)dpOnGrounders / trials:P1})");
        TestContext.Out.WriteLine($"DPs on fly balls: {dpOnFlyBalls}/{trials} (should be 0)");
        TestContext.Out.WriteLine($"DPs on line drives: {dpOnLineDrives}/{trials} (should be 0)");
    }
}
