namespace DiamondSim.Tests.Model;

[TestFixture]
public class PlayLogEntryTests {
    [Test]
    public void Constructor_WithValidData_SetsProperties() {
        // Arrange
        var inning = 1;
        var half = InningHalf.Top;
        var batterName = "Player 1";
        var pitchingTeam = "Away";
        var resolution = CreateTestResolution();
        var isWalkoff = false;
        var outsAfter = 1;

        // Act
        var entry = new PlayLogEntry(inning, half, batterName, pitchingTeam,
            resolution, isWalkoff, outsAfter);

        // Assert
        Assert.That(entry.Inning, Is.EqualTo(inning));
        Assert.That(entry.Half, Is.EqualTo(half));
        Assert.That(entry.BatterName, Is.EqualTo(batterName));
        Assert.That(entry.PitchingTeamName, Is.EqualTo(pitchingTeam));
        Assert.That(entry.Resolution, Is.EqualTo(resolution));
        Assert.That(entry.IsWalkoff, Is.EqualTo(isWalkoff));
        Assert.That(entry.OutsAfter, Is.EqualTo(outsAfter));
    }

    [Test]
    public void Constructor_WithNullBatterName_ThrowsArgumentNullException() {
        var resolution = CreateTestResolution();
        Assert.Throws<ArgumentNullException>(() =>
            new PlayLogEntry(1, InningHalf.Top, null!, "Team", resolution, false, 1));
    }

    [Test]
    public void Constructor_WithNullPitchingTeam_ThrowsArgumentNullException() {
        var resolution = CreateTestResolution();
        Assert.Throws<ArgumentNullException>(() =>
            new PlayLogEntry(1, InningHalf.Top, "Batter", null!, resolution, false, 1));
    }

    [Test]
    public void Constructor_WithNullResolution_ThrowsArgumentNullException() {
        Assert.Throws<ArgumentNullException>(() =>
            new PlayLogEntry(1, InningHalf.Top, "Batter", "Team", null!, false, 1));
    }

    private PaResolution CreateTestResolution() {
        return new PaResolution(
            OutsAdded: 1,
            RunsScored: 0,
            NewBases: new BaseState(false, false, false),
            Type: PaType.K,
            Tag: OutcomeTag.K
        );
    }
}
