namespace DiamondSim.Tests.Scoring;

/// <summary>
/// Tests for the BoxScore class and player statistics tracking.
/// </summary>
[TestFixture]
[Category("Scoring")]
public class BoxScoreTests {
    [Test]
    public void IncrementBatterStats_Single_IncrementsCorrectStats() {
        // Arrange
        var boxScore = new BoxScore();
        var team = Team.Away;
        var paType = PaType.Single;

        // Act
        boxScore.IncrementBatterStats(team, lineupPosition: 0, paType, runsScored: 0, rbiDelta: 0, batterScored: false);

        // Assert
        var stats = boxScore.AwayBatters[0];
        Assert.That(stats.PA, Is.EqualTo(1), "PA should increment");
        Assert.That(stats.AB, Is.EqualTo(1), "AB should increment for single");
        Assert.That(stats.H, Is.EqualTo(1), "H should increment for single");
        Assert.That(stats.Singles, Is.EqualTo(1), "Singles should increment");
        Assert.That(stats.TB, Is.EqualTo(1), "TB should be 1 for single");
        Assert.That(stats.RBI, Is.EqualTo(0), "RBI should be 0 when no runs scored");
        Assert.That(stats.R, Is.EqualTo(0), "R should be 0 when batter didn't score");
    }

    [Test]
    public void IncrementBatterStats_HomeRun_IncrementsCorrectStats() {
        // Arrange
        var boxScore = new BoxScore();
        var team = Team.Home;
        var paType = PaType.HomeRun;

        // Act
        boxScore.IncrementBatterStats(team, lineupPosition: 3, paType, runsScored: 1, rbiDelta: 1, batterScored: true);

        // Assert
        var stats = boxScore.HomeBatters[3];
        Assert.That(stats.PA, Is.EqualTo(1), "PA should increment");
        Assert.That(stats.AB, Is.EqualTo(1), "AB should increment for HR");
        Assert.That(stats.H, Is.EqualTo(1), "H should increment for HR");
        Assert.That(stats.HR, Is.EqualTo(1), "HR should increment");
        Assert.That(stats.TB, Is.EqualTo(4), "TB should be 4 for HR");
        Assert.That(stats.RBI, Is.EqualTo(1), "RBI should be 1 for solo HR");
        Assert.That(stats.R, Is.EqualTo(1), "R should be 1 when batter scored");
    }

    [Test]
    public void IncrementBatterStats_Walk_DoesNotIncrementAB() {
        // Arrange
        var boxScore = new BoxScore();
        var team = Team.Away;
        var paType = PaType.BB;

        // Act
        boxScore.IncrementBatterStats(team, lineupPosition: 1, paType, runsScored: 0, rbiDelta: 0, batterScored: false);

        // Assert
        var stats = boxScore.AwayBatters[1];
        Assert.That(stats.PA, Is.EqualTo(1), "PA should increment");
        Assert.That(stats.AB, Is.EqualTo(0), "AB should NOT increment for walk");
        Assert.That(stats.H, Is.EqualTo(0), "H should NOT increment for walk");
        Assert.That(stats.BB, Is.EqualTo(1), "BB should increment");
        Assert.That(stats.TB, Is.EqualTo(0), "TB should be 0 for walk");
    }

    [Test]
    public void IncrementBatterStats_Strikeout_IncrementsCorrectStats() {
        // Arrange
        var boxScore = new BoxScore();
        var team = Team.Home;
        var paType = PaType.K;

        // Act
        boxScore.IncrementBatterStats(team, lineupPosition: 8, paType, runsScored: 0, rbiDelta: 0, batterScored: false);

        // Assert
        var stats = boxScore.HomeBatters[8];
        Assert.That(stats.PA, Is.EqualTo(1), "PA should increment");
        Assert.That(stats.AB, Is.EqualTo(1), "AB should increment for strikeout per MLB rules");
        Assert.That(stats.K, Is.EqualTo(1), "K should increment");
    }

    [Test]
    public void IncrementBatterStats_ReachOnError_IncrementsABButNotH() {
        // Arrange
        var boxScore = new BoxScore();
        var team = Team.Away;
        var paType = PaType.ReachOnError;

        // Act
        boxScore.IncrementBatterStats(team, lineupPosition: 5, paType, runsScored: 1, rbiDelta: 0, batterScored: false);

        // Assert
        var stats = boxScore.AwayBatters[5];
        Assert.That(stats.PA, Is.EqualTo(1), "PA should increment");
        Assert.That(stats.AB, Is.EqualTo(1), "AB should increment for error");
        Assert.That(stats.H, Is.EqualTo(0), "H should NOT increment for error");
        Assert.That(stats.RBI, Is.EqualTo(0), "RBI should be 0 for ROE per Rule 9.06(g)");
        Assert.That(stats.TB, Is.EqualTo(0), "TB should be 0 for error");
    }

    [Test]
    public void IncrementBatterStats_Double_IncrementsCorrectStats() {
        // Arrange
        var boxScore = new BoxScore();
        var team = Team.Home;
        var paType = PaType.Double;

        // Act
        boxScore.IncrementBatterStats(team, lineupPosition: 2, paType, runsScored: 2, rbiDelta: 2, batterScored: false);

        // Assert
        var stats = boxScore.HomeBatters[2];
        Assert.That(stats.PA, Is.EqualTo(1), "PA should increment");
        Assert.That(stats.AB, Is.EqualTo(1), "AB should increment for double");
        Assert.That(stats.H, Is.EqualTo(1), "H should increment for double");
        Assert.That(stats.Doubles, Is.EqualTo(1), "Doubles should increment");
        Assert.That(stats.TB, Is.EqualTo(2), "TB should be 2 for double");
        Assert.That(stats.RBI, Is.EqualTo(2), "RBI should be 2 for two runners scoring");
        Assert.That(stats.R, Is.EqualTo(0), "R should be 0 when batter didn't score");
    }

    [Test]
    public void IncrementBatterStats_MultiplePAs_AccumulatesStats() {
        // Arrange
        var boxScore = new BoxScore();
        var team = Team.Away;

        // Act - Simulate multiple PAs
        boxScore.IncrementBatterStats(team, lineupPosition: 4, PaType.Single, runsScored: 0, rbiDelta: 0, batterScored: false);
        boxScore.IncrementBatterStats(team, lineupPosition: 4, PaType.BB, runsScored: 0, rbiDelta: 0, batterScored: false);
        boxScore.IncrementBatterStats(team, lineupPosition: 4, PaType.HomeRun, runsScored: 1, rbiDelta: 1, batterScored: true);
        boxScore.IncrementBatterStats(team, lineupPosition: 4, PaType.K, runsScored: 0, rbiDelta: 0, batterScored: false);

        // Assert
        var stats = boxScore.AwayBatters[4];
        Assert.That(stats.PA, Is.EqualTo(4), "PA should be 4");
        Assert.That(stats.AB, Is.EqualTo(3), "AB should be 3 (single + HR + K, not BB)");
        Assert.That(stats.H, Is.EqualTo(2), "H should be 2 (single + HR)");
        Assert.That(stats.Singles, Is.EqualTo(1), "Singles should be 1");
        Assert.That(stats.HR, Is.EqualTo(1), "HR should be 1");
        Assert.That(stats.BB, Is.EqualTo(1), "BB should be 1");
        Assert.That(stats.K, Is.EqualTo(1), "K should be 1");
        Assert.That(stats.TB, Is.EqualTo(5), "TB should be 5 (1 + 4)");
        Assert.That(stats.RBI, Is.EqualTo(1), "RBI should be 1 (HR only)");
        Assert.That(stats.R, Is.EqualTo(1), "R should be 1 (scored on HR)");
    }

    [Test]
    public void IncrementPitcherStats_Strikeout_IncrementsCorrectStats() {
        // Arrange
        var boxScore = new BoxScore();
        var team = Team.Home; // Home team pitching
        var paType = PaType.K;

        // Act
        boxScore.IncrementPitcherStats(team, pitcherId: 0, paType, outsAdded: 1, runsScored: 0);

        // Assert
        var stats = boxScore.HomePitchers[0];
        Assert.That(stats.BF, Is.EqualTo(1), "BF should increment");
        Assert.That(stats.OutsRecorded, Is.EqualTo(1), "OutsRecorded should be 1");
        Assert.That(stats.K, Is.EqualTo(1), "K should increment");
        Assert.That(stats.H, Is.EqualTo(0), "H should be 0");
        Assert.That(stats.R, Is.EqualTo(0), "R should be 0");
    }

    [Test]
    public void IncrementPitcherStats_HomeRun_IncrementsCorrectStats() {
        // Arrange
        var boxScore = new BoxScore();
        var team = Team.Away; // Away team pitching
        var paType = PaType.HomeRun;

        // Act
        boxScore.IncrementPitcherStats(team, pitcherId: 0, paType, outsAdded: 0, runsScored: 2);

        // Assert
        var stats = boxScore.AwayPitchers[0];
        Assert.That(stats.BF, Is.EqualTo(1), "BF should increment");
        Assert.That(stats.OutsRecorded, Is.EqualTo(0), "OutsRecorded should be 0");
        Assert.That(stats.H, Is.EqualTo(1), "H should increment for HR");
        Assert.That(stats.HR, Is.EqualTo(1), "HR should increment");
        Assert.That(stats.R, Is.EqualTo(2), "R should be 2");
        Assert.That(stats.ER, Is.EqualTo(2), "ER should be 2 (all runs earned in v0.2)");
    }

    [Test]
    public void IncrementPitcherStats_Walk_IncrementsCorrectStats() {
        // Arrange
        var boxScore = new BoxScore();
        var team = Team.Home;
        var paType = PaType.BB;

        // Act
        boxScore.IncrementPitcherStats(team, pitcherId: 0, paType, outsAdded: 0, runsScored: 0);

        // Assert
        var stats = boxScore.HomePitchers[0];
        Assert.That(stats.BF, Is.EqualTo(1), "BF should increment");
        Assert.That(stats.BB, Is.EqualTo(1), "BB should increment");
        Assert.That(stats.H, Is.EqualTo(0), "H should be 0 for walk");
        Assert.That(stats.OutsRecorded, Is.EqualTo(0), "OutsRecorded should be 0");
    }

    [Test]
    public void ValidateTeamHits_MatchingHits_ReturnsTrue() {
        // Arrange
        var boxScore = new BoxScore();
        boxScore.IncrementBatterStats(Team.Away, lineupPosition: 0, PaType.Single, runsScored: 0, rbiDelta: 0, batterScored: false);
        boxScore.IncrementBatterStats(Team.Away, lineupPosition: 1, PaType.Double, runsScored: 0, rbiDelta: 0, batterScored: false);
        boxScore.IncrementBatterStats(Team.Away, lineupPosition: 2, PaType.HomeRun, runsScored: 1, rbiDelta: 1, batterScored: true);

        // Act
        bool isValid = boxScore.ValidateTeamHits(Team.Away, expectedTeamHits: 3);

        // Assert
        Assert.That(isValid, Is.True, "Team hits should match sum of individual hits");
    }

    [Test]
    public void ValidateTeamHits_MismatchedHits_ReturnsFalse() {
        // Arrange
        var boxScore = new BoxScore();
        boxScore.IncrementBatterStats(Team.Home, lineupPosition: 0, PaType.Single, runsScored: 0, rbiDelta: 0, batterScored: false);
        boxScore.IncrementBatterStats(Team.Home, lineupPosition: 1, PaType.Double, runsScored: 0, rbiDelta: 0, batterScored: false);

        // Act
        bool isValid = boxScore.ValidateTeamHits(Team.Home, expectedTeamHits: 5); // Wrong expected value

        // Assert
        Assert.That(isValid, Is.False, "Validation should fail when hits don't match");
    }

    [Test]
    public void ValidateDefensiveOuts_MatchingOuts_ReturnsTrue() {
        // Arrange
        var boxScore = new BoxScore();
        boxScore.IncrementPitcherStats(Team.Away, pitcherId: 0, PaType.K, outsAdded: 1, runsScored: 0);
        boxScore.IncrementPitcherStats(Team.Away, pitcherId: 0, PaType.InPlayOut, outsAdded: 1, runsScored: 0);
        boxScore.IncrementPitcherStats(Team.Away, pitcherId: 0, PaType.InPlayOut, outsAdded: 2, runsScored: 0); // Double play

        // Act
        bool isValid = boxScore.ValidateDefensiveOuts(Team.Away, expectedOuts: 4);

        // Assert
        Assert.That(isValid, Is.True, "Defensive outs should match sum of pitcher outs");
    }

    [Test]
    public void GetTotalPitcherOuts_ReturnsCorrectSum() {
        // Arrange
        var boxScore = new BoxScore();
        boxScore.IncrementPitcherStats(Team.Home, pitcherId: 0, PaType.K, outsAdded: 1, runsScored: 0);
        boxScore.IncrementPitcherStats(Team.Home, pitcherId: 0, PaType.K, outsAdded: 1, runsScored: 0);
        boxScore.IncrementPitcherStats(Team.Home, pitcherId: 0, PaType.InPlayOut, outsAdded: 1, runsScored: 0);

        // Act
        int totalOuts = boxScore.GetTotalPitcherOuts(Team.Home);

        // Assert
        Assert.That(totalOuts, Is.EqualTo(3), "Total pitcher outs should be 3");
    }

    [Test]
    public void IncrementBatterStats_HBP_DoesNotIncrementAB() {
        // Arrange
        var boxScore = new BoxScore();
        var team = Team.Away;
        var paType = PaType.HBP;

        // Act
        boxScore.IncrementBatterStats(team, lineupPosition: 6, paType, runsScored: 0, rbiDelta: 0, batterScored: false);

        // Assert
        var stats = boxScore.AwayBatters[6];
        Assert.That(stats.PA, Is.EqualTo(1), "PA should increment");
        Assert.That(stats.AB, Is.EqualTo(0), "AB should NOT increment for HBP");
        Assert.That(stats.HBP, Is.EqualTo(1), "HBP should increment");
        Assert.That(stats.TB, Is.EqualTo(0), "TB should be 0 for HBP");
    }

    [Test]
    public void IncrementBatterStats_Triple_IncrementsCorrectStats() {
        // Arrange
        var boxScore = new BoxScore();
        var team = Team.Home;
        var paType = PaType.Triple;

        // Act
        boxScore.IncrementBatterStats(team, lineupPosition: 7, paType, runsScored: 1, rbiDelta: 1, batterScored: false);

        // Assert
        var stats = boxScore.HomeBatters[7];
        Assert.That(stats.PA, Is.EqualTo(1), "PA should increment");
        Assert.That(stats.AB, Is.EqualTo(1), "AB should increment for triple");
        Assert.That(stats.H, Is.EqualTo(1), "H should increment for triple");
        Assert.That(stats.Triples, Is.EqualTo(1), "Triples should increment");
        Assert.That(stats.TB, Is.EqualTo(3), "TB should be 3 for triple");
        Assert.That(stats.RBI, Is.EqualTo(1), "RBI should be 1");
    }
}
