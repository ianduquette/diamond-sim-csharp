namespace DiamondSim.Tests.TestHelpers;

/// <summary>
/// Tests for TestSnapshot helper to ensure it correctly captures game state
/// and avoids false positives from GameState.Equals() which only compares balls/strikes.
/// </summary>
[TestFixture]
public class TestSnapshotTests {
    [Test]
    public void TestSnapshot_CapturesAllRelevantFields() {
        // Arrange
        var state = new GameState(
            balls: 2, strikes: 1,
            inning: 7, half: InningHalf.Bottom, outs: 2,
            onFirst: true, onSecond: false, onThird: true,
            awayScore: 4, homeScore: 5,
            awayBattingOrderIndex: 3, homeBattingOrderIndex: 6,
            offense: Team.Home, defense: Team.Away,
            isFinal: false
        );

        // Act
        var snapshot = state.ToTestSnapshot();

        // Assert
        Assert.Multiple(() => {
            Assert.That(snapshot.Inning, Is.EqualTo(7), "Inning captured");
            Assert.That(snapshot.Half, Is.EqualTo(InningHalf.Bottom), "Half captured");
            Assert.That(snapshot.Outs, Is.EqualTo(2), "Outs captured");
            Assert.That(snapshot.OnFirst, Is.True, "OnFirst captured");
            Assert.That(snapshot.OnSecond, Is.False, "OnSecond captured");
            Assert.That(snapshot.OnThird, Is.True, "OnThird captured");
            Assert.That(snapshot.AwayScore, Is.EqualTo(4), "AwayScore captured");
            Assert.That(snapshot.HomeScore, Is.EqualTo(5), "HomeScore captured");
            Assert.That(snapshot.Offense, Is.EqualTo(Team.Home), "Offense captured");
            Assert.That(snapshot.Defense, Is.EqualTo(Team.Away), "Defense captured");
            Assert.That(snapshot.IsFinal, Is.False, "IsFinal captured");
        });
    }

    [Test]
    public void TestSnapshot_IgnoresBallsStrikes() {
        // Arrange: Two states with different counts but same game situation
        var state1 = new GameState(
            balls: 0, strikes: 0,
            inning: 5, half: InningHalf.Top, outs: 1,
            onFirst: false, onSecond: true, onThird: false,
            awayScore: 2, homeScore: 3,
            awayBattingOrderIndex: 4, homeBattingOrderIndex: 0,
            offense: Team.Away, defense: Team.Home
        );

        var state2 = new GameState(
            balls: 3, strikes: 2,  // Different count
            inning: 5, half: InningHalf.Top, outs: 1,
            onFirst: false, onSecond: true, onThird: false,
            awayScore: 2, homeScore: 3,
            awayBattingOrderIndex: 4, homeBattingOrderIndex: 0,
            offense: Team.Away, defense: Team.Home
        );

        // Act
        var snapshot1 = state1.ToTestSnapshot();
        var snapshot2 = state2.ToTestSnapshot();

        // Assert: Snapshots should be equal despite different counts
        Assert.That(snapshot1, Is.EqualTo(snapshot2),
            "TestSnapshot ignores balls/strikes differences");

        // But GameState.Equals would show them as different
        Assert.That(state1.Equals(state2), Is.False,
            "GameState.Equals only compares count, showing difference");
    }

    [Test]
    public void TestSnapshot_DetectsDifferences() {
        // Arrange: Two states with different game situations
        var state1 = new GameState(
            balls: 2, strikes: 1,
            inning: 5, half: InningHalf.Top, outs: 1,
            onFirst: false, onSecond: true, onThird: false,
            awayScore: 2, homeScore: 3,
            awayBattingOrderIndex: 4, homeBattingOrderIndex: 0,
            offense: Team.Away, defense: Team.Home
        );

        var state2 = new GameState(
            balls: 2, strikes: 1,  // Same count
            inning: 5, half: InningHalf.Top, outs: 2,  // Different outs
            onFirst: false, onSecond: true, onThird: false,
            awayScore: 2, homeScore: 3,
            awayBattingOrderIndex: 4, homeBattingOrderIndex: 0,
            offense: Team.Away, defense: Team.Home
        );

        // Act
        var snapshot1 = state1.ToTestSnapshot();
        var snapshot2 = state2.ToTestSnapshot();

        // Assert: Snapshots should detect the difference
        Assert.That(snapshot1, Is.Not.EqualTo(snapshot2),
            "TestSnapshot detects outs difference");

        // GameState.Equals would show them as equal (same count)
        Assert.That(state1.Equals(state2), Is.True,
            "GameState.Equals only compares count, missing the difference");
    }

    [Test]
    public void TestSnapshot_DetectsScoreDifference() {
        // Arrange
        var state1 = new GameState(
            balls: 1, strikes: 1,
            inning: 9, half: InningHalf.Bottom, outs: 2,
            onFirst: false, onSecond: false, onThird: true,
            awayScore: 3, homeScore: 3,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 5,
            offense: Team.Home, defense: Team.Away
        );

        var state2 = new GameState(
            balls: 1, strikes: 1,
            inning: 9, half: InningHalf.Bottom, outs: 2,
            onFirst: false, onSecond: false, onThird: true,
            awayScore: 3, homeScore: 4,  // Different score
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 5,
            offense: Team.Home, defense: Team.Away
        );

        // Act
        var snapshot1 = state1.ToTestSnapshot();
        var snapshot2 = state2.ToTestSnapshot();

        // Assert
        Assert.That(snapshot1, Is.Not.EqualTo(snapshot2),
            "TestSnapshot detects score difference");
    }

    [Test]
    public void TestSnapshot_DetectsIsFinalDifference() {
        // Arrange
        var state1 = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Bottom, outs: 0,
            onFirst: false, onSecond: false, onThird: false,
            awayScore: 5, homeScore: 6,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 3,
            offense: Team.Home, defense: Team.Away,
            isFinal: false
        );

        var state2 = new GameState(
            balls: 0, strikes: 0,
            inning: 9, half: InningHalf.Bottom, outs: 0,
            onFirst: false, onSecond: false, onThird: false,
            awayScore: 5, homeScore: 6,
            awayBattingOrderIndex: 0, homeBattingOrderIndex: 3,
            offense: Team.Home, defense: Team.Away,
            isFinal: true  // Different IsFinal
        );

        // Act
        var snapshot1 = state1.ToTestSnapshot();
        var snapshot2 = state2.ToTestSnapshot();

        // Assert
        Assert.That(snapshot1, Is.Not.EqualTo(snapshot2),
            "TestSnapshot detects IsFinal difference");
    }
}
