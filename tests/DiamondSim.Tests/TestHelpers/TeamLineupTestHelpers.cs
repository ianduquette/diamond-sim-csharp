namespace DiamondSim.Tests.TestHelpers;

/// <summary>
/// Helper methods for asserting TeamLineup and Batter properties in tests.
/// </summary>
public static class TeamLineupTestHelpers {
    /// <summary>
    /// Asserts that a batter matches expected properties.
    /// </summary>
    public static void AssertBatter(Batter actual, string expectedName, BatterRatings? expectedRatings = null) {
        Assert.That(actual, Is.Not.Null, "Batter should not be null");
        Assert.That(actual.Name, Is.EqualTo(expectedName), $"Batter name should be '{expectedName}'");

        if (expectedRatings != null) {
            Assert.That(actual.Ratings.Contact, Is.EqualTo(expectedRatings.Contact), "Contact rating mismatch");
            Assert.That(actual.Ratings.Power, Is.EqualTo(expectedRatings.Power), "Power rating mismatch");
            Assert.That(actual.Ratings.Patience, Is.EqualTo(expectedRatings.Patience), "Patience rating mismatch");
        }
    }

    /// <summary>
    /// Asserts that two lineups match (same team name and batter names in same order).
    /// </summary>
    public static void AssertLineup(TeamLineup expected, TeamLineup actual) {
        Assert.That(actual, Is.Not.Null, "Actual lineup should not be null");
        Assert.That(expected, Is.Not.Null, "Expected lineup should not be null");

        Assert.That(actual.TeamName, Is.EqualTo(expected.TeamName), "Team names should match");
        Assert.That(actual.Batters.Count, Is.EqualTo(expected.Batters.Count), "Batter counts should match");

        for (int i = 0; i < expected.Batters.Count; i++) {
            Assert.That(actual.Batters[i].Name, Is.EqualTo(expected.Batters[i].Name),
                $"Batter at position {i} should be '{expected.Batters[i].Name}'");
        }
    }
}
