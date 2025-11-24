namespace DiamondSim.Tests.TestHelpers;

/// <summary>
/// Helper methods for asserting GameMetadata properties in tests.
/// Reduces duplication across test files.
/// </summary>
public static class GameMetadataTestHelpers {
    /// <summary>
    /// Asserts that a GameMetadata object matches expected values.
    /// If expectedTimestamp is not provided, asserts that Timestamp is not default.
    /// </summary>
    public static void AssertGameMetadata(
        GameMetadata actual,
        string expectedHomeTeam,
        string expectedAwayTeam,
        int expectedSeed,
        DateTime? expectedTimestamp = null) {

        Assert.Multiple(() => {
            Assert.That(actual.HomeTeamName, Is.EqualTo(expectedHomeTeam), "HomeTeamName mismatch");
            Assert.That(actual.AwayTeamName, Is.EqualTo(expectedAwayTeam), "AwayTeamName mismatch");
            Assert.That(actual.Seed, Is.EqualTo(expectedSeed), "Seed mismatch");

            if (expectedTimestamp.HasValue) {
                Assert.That(actual.Timestamp, Is.EqualTo(expectedTimestamp.Value), "Timestamp mismatch");
            }
            else {
                Assert.That(actual.Timestamp, Is.Not.EqualTo(default(DateTime)),
                    "Timestamp should not be default value");
            }
        });
    }
}
