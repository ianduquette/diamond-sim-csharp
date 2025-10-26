namespace DiamondSim;

/// <summary>
/// Represents the current count state in a baseball at-bat and the complete game situation.
/// </summary>
public class GameState {
    /// <summary>
    /// Gets or sets the number of balls in the current count (0-4).
    /// </summary>
    public int Balls { get; set; }

    /// <summary>
    /// Gets or sets the number of strikes in the current count (0-3).
    /// </summary>
    public int Strikes { get; set; }

    /// <summary>
    /// Gets or sets the current inning number (starts at 1).
    /// </summary>
    public int Inning { get; set; }

    /// <summary>
    /// Gets or sets which half of the inning is being played.
    /// </summary>
    public InningHalf Half { get; set; }

    /// <summary>
    /// Gets or sets the number of outs in the current half-inning (0-2).
    /// </summary>
    public int Outs { get; set; }

    /// <summary>
    /// Gets or sets whether the game has reached a final state.
    /// </summary>
    public bool IsFinal { get; set; }

    /// <summary>
    /// Gets or sets whether a runner is on first base.
    /// </summary>
    public bool OnFirst { get; set; }

    /// <summary>
    /// Gets or sets whether a runner is on second base.
    /// </summary>
    public bool OnSecond { get; set; }

    /// <summary>
    /// Gets or sets whether a runner is on third base.
    /// </summary>
    public bool OnThird { get; set; }

    /// <summary>
    /// Gets or sets the away team's score.
    /// </summary>
    public int AwayScore { get; set; }

    /// <summary>
    /// Gets or sets the home team's score.
    /// </summary>
    public int HomeScore { get; set; }

    /// <summary>
    /// Gets or sets the away team's earned runs.
    /// </summary>
    public int AwayEarnedRuns { get; set; }

    /// <summary>
    /// Gets or sets the away team's unearned runs.
    /// </summary>
    public int AwayUnearnedRuns { get; set; }

    /// <summary>
    /// Gets or sets the home team's earned runs.
    /// </summary>
    public int HomeEarnedRuns { get; set; }

    /// <summary>
    /// Gets or sets the home team's unearned runs.
    /// </summary>
    public int HomeUnearnedRuns { get; set; }

    /// <summary>
    /// Gets or sets the away team's current batting order position (0-8).
    /// </summary>
    public int AwayBattingOrderIndex { get; set; }

    /// <summary>
    /// Gets or sets the home team's current batting order position (0-8).
    /// </summary>
    public int HomeBattingOrderIndex { get; set; }

    /// <summary>
    /// Gets or sets which team is currently batting.
    /// </summary>
    public Team Offense { get; set; }

    /// <summary>
    /// Gets or sets which team is currently fielding.
    /// </summary>
    public Team Defense { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameState"/> class with only count information.
    /// This constructor maintains backward compatibility with existing code.
    /// </summary>
    /// <param name="balls">The number of balls (0-3).</param>
    /// <param name="strikes">The number of strikes (0-2).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when balls is not in range 0-3 or strikes is not in range 0-2.
    /// </exception>
    public GameState(int balls, int strikes) {
        if (balls < 0 || balls > 4) {
            throw new ArgumentOutOfRangeException(nameof(balls), balls, "Balls must be between 0 and 4.");
        }

        if (strikes < 0 || strikes > 3) {
            throw new ArgumentOutOfRangeException(nameof(strikes), strikes, "Strikes must be between 0 and 3.");
        }

        Balls = balls;
        Strikes = strikes;

        // Initialize game situation with sensible defaults
        Inning = 1;
        Half = InningHalf.Top;
        Outs = 0;
        IsFinal = false;
        OnFirst = false;
        OnSecond = false;
        OnThird = false;
        AwayScore = 0;
        HomeScore = 0;
        AwayEarnedRuns = 0;
        AwayUnearnedRuns = 0;
        HomeEarnedRuns = 0;
        HomeUnearnedRuns = 0;
        AwayBattingOrderIndex = 0;
        HomeBattingOrderIndex = 0;
        Offense = Team.Away;
        Defense = Team.Home;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameState"/> class with full game situation.
    /// </summary>
    /// <param name="balls">The number of balls (0-4).</param>
    /// <param name="strikes">The number of strikes (0-3).</param>
    /// <param name="inning">The current inning number.</param>
    /// <param name="half">Which half of the inning is being played.</param>
    /// <param name="outs">The number of outs (0-2).</param>
    /// <param name="onFirst">Whether a runner is on first base.</param>
    /// <param name="onSecond">Whether a runner is on second base.</param>
    /// <param name="onThird">Whether a runner is on third base.</param>
    /// <param name="awayScore">The away team's score.</param>
    /// <param name="homeScore">The home team's score.</param>
    /// <param name="awayBattingOrderIndex">The away team's batting order position (0-8).</param>
    /// <param name="homeBattingOrderIndex">The home team's batting order position (0-8).</param>
    /// <param name="offense">Which team is currently batting.</param>
    /// <param name="defense">Which team is currently fielding.</param>
    /// <param name="isFinal">Whether the game has reached a final state.</param>
    /// <param name="awayEarnedRuns">The away team's earned runs (optional, defaults to 0).</param>
    /// <param name="awayUnearnedRuns">The away team's unearned runs (optional, defaults to 0).</param>
    /// <param name="homeEarnedRuns">The home team's earned runs (optional, defaults to 0).</param>
    /// <param name="homeUnearnedRuns">The home team's unearned runs (optional, defaults to 0).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any parameter is outside its valid range.
    /// </exception>
    public GameState(
        int balls,
        int strikes,
        int inning,
        InningHalf half,
        int outs,
        bool onFirst,
        bool onSecond,
        bool onThird,
        int awayScore,
        int homeScore,
        int awayBattingOrderIndex,
        int homeBattingOrderIndex,
        Team offense,
        Team defense,
        bool isFinal = false,
        int awayEarnedRuns = 0,
        int awayUnearnedRuns = 0,
        int homeEarnedRuns = 0,
        int homeUnearnedRuns = 0) {
        if (balls < 0 || balls > 4) {
            throw new ArgumentOutOfRangeException(nameof(balls), balls, "Balls must be between 0 and 4.");
        }

        if (strikes < 0 || strikes > 3) {
            throw new ArgumentOutOfRangeException(nameof(strikes), strikes, "Strikes must be between 0 and 3.");
        }

        if (inning < 1) {
            throw new ArgumentOutOfRangeException(nameof(inning), inning, "Inning must be at least 1.");
        }

        if (outs < 0 || outs > 2) {
            throw new ArgumentOutOfRangeException(nameof(outs), outs, "Outs must be between 0 and 2.");
        }

        if (awayScore < 0) {
            throw new ArgumentOutOfRangeException(nameof(awayScore), awayScore, "Away score cannot be negative.");
        }

        if (homeScore < 0) {
            throw new ArgumentOutOfRangeException(nameof(homeScore), homeScore, "Home score cannot be negative.");
        }

        if (awayBattingOrderIndex < 0 || awayBattingOrderIndex > 8) {
            throw new ArgumentOutOfRangeException(nameof(awayBattingOrderIndex), awayBattingOrderIndex, "Batting order index must be between 0 and 8.");
        }

        if (homeBattingOrderIndex < 0 || homeBattingOrderIndex > 8) {
            throw new ArgumentOutOfRangeException(nameof(homeBattingOrderIndex), homeBattingOrderIndex, "Batting order index must be between 0 and 8.");
        }

        Balls = balls;
        Strikes = strikes;
        Inning = inning;
        Half = half;
        Outs = outs;
        OnFirst = onFirst;
        OnSecond = onSecond;
        OnThird = onThird;
        AwayScore = awayScore;
        HomeScore = homeScore;
        AwayEarnedRuns = awayEarnedRuns;
        AwayUnearnedRuns = awayUnearnedRuns;
        HomeEarnedRuns = homeEarnedRuns;
        HomeUnearnedRuns = homeUnearnedRuns;
        AwayBattingOrderIndex = awayBattingOrderIndex;
        HomeBattingOrderIndex = homeBattingOrderIndex;
        Offense = offense;
        Defense = defense;
        IsFinal = isFinal;
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

    /// <summary>
    /// Determines whether the current count represents a walk (4 balls).
    /// </summary>
    /// <returns><c>true</c> if the count is 4 balls; otherwise, <c>false</c>.</returns>
    public bool IsWalk() => Balls >= 4;

    /// <summary>
    /// Determines whether the current count represents a strikeout (3 strikes).
    /// </summary>
    /// <returns><c>true</c> if the count is 3 strikes; otherwise, <c>false</c>.</returns>
    public bool IsStrikeout() => Strikes >= 3;

    /// <summary>
    /// Determines whether the current count represents a terminal state (walk or strikeout).
    /// </summary>
    /// <returns><c>true</c> if the count is a walk or strikeout; otherwise, <c>false</c>.</returns>
    public bool IsTerminal() => IsWalk() || IsStrikeout();

    /// <summary>
    /// Increments the ball count by one.
    /// </summary>
    public void IncrementBalls() => Balls++;

    /// <summary>
    /// Increments the strike count by one.
    /// </summary>
    public void IncrementStrikes() => Strikes++;

    /// <summary>
    /// Increments the strike count by one, but only if strikes is less than 2.
    /// This implements the foul ball rule where a foul ball cannot create a third strike.
    /// </summary>
    public void IncrementStrikesSafe() {
        if (Strikes < 2) {
            Strikes++;
        }
    }

    /// <summary>
    /// Gets the score of the team currently batting.
    /// </summary>
    /// <returns>The offensive team's current score.</returns>
    public int GetOffenseScore() {
        return Offense == Team.Away ? AwayScore : HomeScore;
    }

    /// <summary>
    /// Gets the score of the team currently fielding.
    /// </summary>
    /// <returns>The defensive team's current score.</returns>
    public int GetDefenseScore() {
        return Defense == Team.Away ? AwayScore : HomeScore;
    }

    /// <summary>
    /// Gets the batting order index for the team currently batting.
    /// </summary>
    /// <returns>The offensive team's current batting order position (0-8).</returns>
    public int GetBattingOrderIndex() {
        return Offense == Team.Away ? AwayBattingOrderIndex : HomeBattingOrderIndex;
    }

    /// <summary>
    /// Determines whether a walk-off situation is possible (bottom of 9th or later, home team batting, tied or trailing by one run or less).
    /// </summary>
    /// <returns><c>true</c> if a walk-off situation is possible; otherwise, <c>false</c>.</returns>
    public bool IsWalkoffSituation() {
        return Inning >= 9
            && Half == InningHalf.Bottom
            && Offense == Team.Home
            && HomeScore <= AwayScore;
    }
}
