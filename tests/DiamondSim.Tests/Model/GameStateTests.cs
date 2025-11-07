using DiamondSim.Tests.TestHelpers;

namespace DiamondSim.Tests.Model;

/// <summary>
/// Tests for GameState model validation and behavior.
/// Validates count ranges, terminal state detection, and equality operations.
/// </summary>
[TestFixture]
public class GameStateTests {

    [Test]
    public void Constructor_ValidCounts_CreatesGameState() {
        // Act
        var state = new GameState(balls: 2, strikes: 1);

        // Assert
        Assert.That(state.Balls, Is.EqualTo(2));
        Assert.That(state.Strikes, Is.EqualTo(1));
    }

    [Test]
    public void Constructor_TerminalWalkState_IsValid() {
        // Act
        var walkState = new GameState(balls: 4, strikes: 0);

        // Assert
        Assert.That(walkState.Balls, Is.EqualTo(4));
        Assert.That(walkState.Strikes, Is.EqualTo(0));
    }

    [Test]
    public void Constructor_TerminalStrikeoutState_IsValid() {
        // Act
        var strikeoutState = new GameState(balls: 0, strikes: 3);

        // Assert
        Assert.That(strikeoutState.Balls, Is.EqualTo(0));
        Assert.That(strikeoutState.Strikes, Is.EqualTo(3));
    }

    [Test]
    public void Constructor_NegativeBalls_ThrowsArgumentOutOfRangeException() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameState(balls: -1, strikes: 0));
    }

    [Test]
    public void Constructor_TooManyBalls_ThrowsArgumentOutOfRangeException() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameState(balls: 5, strikes: 0));
    }

    [Test]
    public void Constructor_NegativeStrikes_ThrowsArgumentOutOfRangeException() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameState(balls: 0, strikes: -1));
    }

    [Test]
    public void Constructor_TooManyStrikes_ThrowsArgumentOutOfRangeException() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameState(balls: 0, strikes: 4));
    }

    [Test]
    public void IsComplete_NeutralCount_ReturnsFalse() {
        // Arrange
        var state = new GameState(0, 0);

        // Act
        var result = state.IsComplete();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsComplete_MidCount_ReturnsFalse() {
        // Arrange
        var state = new GameState(2, 1);

        // Act
        var result = state.IsComplete();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsComplete_FullCount_ReturnsFalse() {
        // Arrange
        var state = new GameState(3, 2);

        // Act
        var result = state.IsComplete();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsComplete_Walk_ReturnsTrue() {
        // Arrange
        var state = new GameState(4, 0);

        // Act
        var result = state.IsComplete();

        // Assert
        Assert.That(result, Is.True, "4 balls (walk) should be complete");
    }

    [Test]
    public void IsComplete_Strikeout_ReturnsTrue() {
        // Arrange
        var state = new GameState(0, 3);

        // Act
        var result = state.IsComplete();

        // Assert
        Assert.That(result, Is.True, "3 strikes (strikeout) should be complete");
    }

    [Test]
    public void IsComplete_WalkWithStrikes_ReturnsTrue() {
        // Arrange
        var state = new GameState(4, 2);

        // Act
        var result = state.IsComplete();

        // Assert
        Assert.That(result, Is.True, "4 balls with 2 strikes should be complete");
    }

    [Test]
    public void ToString_NeutralCount_FormatsCorrectly() {
        // Arrange
        var state = new GameState(0, 0);

        // Act
        var result = state.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("0-0"));
    }

    [Test]
    public void ToString_MidCount_FormatsCorrectly() {
        // Arrange
        var state = new GameState(2, 1);

        // Act
        var result = state.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("2-1"));
    }

    [Test]
    public void ToString_FullCount_FormatsCorrectly() {
        // Arrange
        var state = new GameState(3, 2);

        // Act
        var result = state.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("3-2"));
    }

    [Test]
    public void Equals_SameValues_ReturnsTrue() {
        // Arrange
        var state1 = new GameState(2, 1);
        var state2 = new GameState(2, 1);

        // Act
        var result = state1.Equals(state2);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(state1, Is.EqualTo(state2));
    }

    [Test]
    public void Equals_DifferentValues_ReturnsFalse() {
        // Arrange
        var state1 = new GameState(2, 1);
        var state2 = new GameState(1, 2);

        // Act
        var result = state1.Equals(state2);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(state1, Is.Not.EqualTo(state2));
    }

    [Test]
    public void Equals_Null_ReturnsFalse() {
        // Arrange
        var state = new GameState(2, 1);

        // Act
        var result = state.Equals(null);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Equals_DifferentType_ReturnsFalse() {
        // Arrange
        var state = new GameState(2, 1);

        // Act
        var result = state.Equals("not a GameState");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void GetHashCode_SameValues_ReturnsSameHash() {
        // Arrange
        var state1 = new GameState(2, 1);
        var state2 = new GameState(2, 1);

        // Act
        var hash1 = state1.GetHashCode();
        var hash2 = state2.GetHashCode();

        // Assert
        Assert.That(hash1, Is.EqualTo(hash2));
    }

    [Test]
    public void GetHashCode_DifferentValues_ReturnsDifferentHash() {
        // Arrange
        var state1 = new GameState(2, 1);
        var state2 = new GameState(1, 2);

        // Act
        var hash1 = state1.GetHashCode();
        var hash2 = state2.GetHashCode();

        // Assert
        Assert.That(hash1, Is.Not.EqualTo(hash2));
    }
}
