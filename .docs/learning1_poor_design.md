Why on earth would we NOT simply fix the GameState.Equals method? Then test that?!? This seems like a HUGE waste of time and code.

You're 100% correct. This is a terrible design decision. Here's what's happening:

The Core Problem
GameState.Equals() intentionally only compares balls/strikes, treating GameState as if it's just a "count" object. But GameState actually contains 20+ fields representing the entire game situation.

Why This Design Exists
Looking at GameStateTests.cs, the tests explicitly validate this behavior:

[Test]
public void Equals_SameValues_ReturnsTrue() {
    var state1 = new GameState(2, 1);
    var state2 = new GameState(2, 1);
    Assert.That(state1.Equals(state2), Is.True);
}

This suggests GameState was originally designed as just a count tracker, and the other fields were added later without updating Equals().

The Workaround's Cost
Instead of fixing the root cause, someone created:

TestSnapshot - 48 lines
TestSnapshotTests - 170 lines
Every test must now use .ToTestSnapshot() instead of direct comparison
Total: 218+ lines of workaround code

The Correct Solution
Fix GameState.Equals() to compare all relevant fields:

public override bool Equals(object? obj) {
    if (obj is GameState other) {
        return Balls == other.Balls
            && Strikes == other.Strikes
            && Inning == other.Inning
            && Half == other.Half
            && Outs == other.Outs
            && OnFirst == other.OnFirst
            && OnSecond == other.OnSecond
            && OnThird == other.OnThird
            && AwayScore == other.AwayScore
            && HomeScore == other.HomeScore
            && Offense == other.Offense
            && Defense == other.Defense
            && IsFinal == other.IsFinal;
    }
    return false;
}

Then update GetHashCode() accordingly and delete all the TestSnapshot code.

Recommendation
Delete TestSnapshot.cs and TestSnapshotTests.cs entirely and fix GameState.Equals() properly. This is a classic example of technical debt where a workaround became institutionalized instead of fixing the underlying design flaw.
