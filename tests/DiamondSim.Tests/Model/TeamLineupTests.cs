namespace DiamondSim.Tests.Model;

[TestFixture]
public class TeamLineupTests {
    [Test]
    public void Constructor_WithValidData_SetsProperties() {
        // Arrange
        var teamName = "Sharks";
        var batters = CreateTestBatters(9);

        // Act
        var lineup = new TeamLineup(teamName, batters);

        // Assert
        Assert.That(lineup.TeamName, Is.EqualTo(teamName));
        Assert.That(lineup.Batters, Is.EqualTo(batters));
        Assert.That(lineup.Batters.Count, Is.EqualTo(9));
    }

    [Test]
    public void Constructor_WithNullTeamName_ThrowsArgumentNullException() {
        var batters = CreateTestBatters(9);
        Assert.Throws<ArgumentNullException>(() =>
            new TeamLineup(null!, batters));
    }

    [Test]
    public void Constructor_WithNullBatters_ThrowsArgumentNullException() {
        Assert.Throws<ArgumentNullException>(() =>
            new TeamLineup("Team", null!));
    }

    [Test]
    public void Constructor_WithWrongBatterCount_ThrowsArgumentException() {
        var batters = CreateTestBatters(8); // Wrong count

        var ex = Assert.Throws<ArgumentException>(() =>
            new TeamLineup("Team", batters));

        Assert.That(ex!.Message, Does.Contain("9 batters"));
    }

    private IReadOnlyList<Batter> CreateTestBatters(int count) {
        return Enumerable.Range(1, count)
            .Select(i => new Batter($"Player {i}", BatterRatings.Average))
            .ToList()
            .AsReadOnly();
    }
}
