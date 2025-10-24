namespace DiamondSim;

/// <summary>
/// Represents the current count state in a baseball at-bat.
/// </summary>
public class GameState {
    /// <summary>
    /// Gets the number of balls in the current count (0-3).
    /// </summary>
    public int Balls { get; }

    /// <summary>
    /// Gets the number of strikes in the current count (0-2).
    /// </summary>
    public int Strikes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameState"/> class.
    /// </summary>
    /// <param name="balls">The number of balls (0-3).</param>
    /// <param name="strikes">The number of strikes (0-2).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when balls is not in range 0-3 or strikes is not in range 0-2.
    /// </exception>
    public GameState(int balls, int strikes) {
        if (balls < 0 || balls > 3) {
            throw new ArgumentOutOfRangeException(nameof(balls), balls, "Balls must be between 0 and 3.");
        }

        if (strikes < 0 || strikes > 2) {
            throw new ArgumentOutOfRangeException(nameof(strikes), strikes, "Strikes must be between 0 and 2.");
        }

        Balls = balls;
        Strikes = strikes;
    }

    /// <summary>
    /// Determines whether the current count represents a completed at-bat (walk or strikeout).
    /// </summary>
    /// <returns>
    /// <c>true</c> if the count is 4 balls (walk) or 3 strikes (strikeout); otherwise, <c>false</c>.
    /// </returns>
    public bool IsComplete() {
        return Balls == 4 || Strikes == 3;
    }

    /// <summary>
    /// Returns a string representation of the count in "balls-strikes" format.
    /// </summary>
    /// <returns>A string in the format "B-S" (e.g., "2-1").</returns>
    public override string ToString() {
        return $"{Balls}-{Strikes}";
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current GameState.
    /// </summary>
    /// <param name="obj">The object to compare with the current GameState.</param>
    /// <returns><c>true</c> if the specified object is equal to the current GameState; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj) {
        if (obj is GameState other) {
            return Balls == other.Balls && Strikes == other.Strikes;
        }
        return false;
    }

    /// <summary>
    /// Returns a hash code for the current GameState.
    /// </summary>
    /// <returns>A hash code for the current GameState.</returns>
    public override int GetHashCode() {
        return HashCode.Combine(Balls, Strikes);
    }
}
