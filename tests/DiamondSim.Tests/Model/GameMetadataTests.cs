using DiamondSim.Tests.TestHelpers;

namespace DiamondSim.Tests.Model;

[TestFixture]
public class GameMetadataTests {
    [Test]
    public void Constructor_WithValidData_SetsProperties() {
        // Arrange
        var homeTeam = "Sharks";
        var awayTeam = "Comets";
        var seed = 42;
        var timestamp = DateTime.Now;

        // Act
        var metadata = new GameMetadata(homeTeam, awayTeam, seed, timestamp);

        // Assert
        GameMetadataTestHelpers.AssertGameMetadata(metadata, homeTeam, awayTeam, seed, timestamp);
    }

    [Test]
    public void Constructor_WithNullHomeTeam_ThrowsArgumentNullException() {
        Assert.Throws<ArgumentNullException>(() =>
            new GameMetadata(null!, "Away", 42, DateTime.Now));
    }

    [Test]
    public void Constructor_WithNullAwayTeam_ThrowsArgumentNullException() {
        Assert.Throws<ArgumentNullException>(() =>
            new GameMetadata("Home", null!, 42, DateTime.Now));
    }
}
