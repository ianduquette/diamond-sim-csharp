namespace DiamondSim.Tests.Scoring;

/// <summary>
/// Tests for the BoxScore class and player statistics tracking.
/// </summary>
[TestFixture]
[Category("Scoring")]
public class BoxScoreTests {
    private BoxScore _boxScore = null!;

    [SetUp]
    public void Setup() {
        _boxScore = new BoxScore();
    }

    [Test]
    public void IncrementBatterStats_Single_IncrementsCorrectStats() {
        // Arrange
        var team = Team.Away;
        int lineupPosition = 0;
        var paType = PaType.Single;
        int runsScored = 0;
        int rbiDelta = 0;
        bool batterScored = false;

        // Act
        _boxScore.IncrementBatterStats(team, lineupPosition, paType, runsScored, rbiDelta, batterScored);

        // Assert
        var stats = _boxScore.AwayBatters[lineupPosition];
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
        var team = Team.Home;
        int lineupPosition = 3;
        var paType = PaType.HomeRun;
        int runsScored = 1; // Solo HR
        int rbiDelta = 1;
        bool batterScored = true;

        // Act
        _boxScore.IncrementBatterStats(team, lineupPosition, paType, runsScored, rbiDelta, batterScored);

        // Assert
        var stats = _boxScore.HomeBatters[lineupPosition];
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
        var team = Team.Away;
        int lineupPosition = 1;
        var paType = PaType.BB;
        int runsScored = 0;
        int rbiDelta = 0;
        bool batterScored = false;

        // Act
        _boxScore.IncrementBatterStats(team, lineupPosition, paType, runsScored, rbiDelta, batterScored);

        // Assert
        var stats = _boxScore.AwayBatters[lineupPosition];
        Assert.That(stats.PA, Is.EqualTo(1), "PA should increment");
        Assert.That(stats.AB, Is.EqualTo(0), "AB should NOT increment for walk");
        Assert.That(stats.H, Is.EqualTo(0), "H should NOT increment for walk");
        Assert.That(stats.BB, Is.EqualTo(1), "BB should increment");
        Assert.That(stats.TB, Is.EqualTo(0), "TB should be 0 for walk");
    }

    [Test]
    public void IncrementBatterStats_Strikeout_IncrementsCorrectStats() {
        // Arrange
        var team = Team.Home;
        int lineupPosition = 8;
        var paType = PaType.K;
        int runsScored = 0;
        int rbiDelta = 0;
        bool batterScored = false;

        // Act
        _boxScore.IncrementBatterStats(team, lineupPosition, paType, runsScored, rbiDelta, batterScored);

        // Assert
        var stats = _boxScore.HomeBatters[lineupPosition];
        Assert.That(stats.PA, Is.EqualTo(1), "PA should increment");
        Assert.That(stats.AB, Is.EqualTo(1), "AB should increment for strikeout per MLB rules");
        Assert.That(stats.K, Is.EqualTo(1), "K should increment");
    }

    [Test]
    public void IncrementBatterStats_ReachOnError_IncrementsABButNotH() {
        // Arrange
        var team = Team.Away;
        int lineupPosition = 5;
        var paType = PaType.ReachOnError;
        int runsScored = 1; // Runner scored on error
        int rbiDelta = 0; // ROE = 0 RBI per official rules
        bool batterScored = false;

        // Act
        _boxScore.IncrementBatterStats(team, lineupPosition, paType, runsScored, rbiDelta, batterScored);

        // Assert
        var stats = _boxScore.AwayBatters[lineupPosition];
        Assert.That(stats.PA, Is.EqualTo(1), "PA should increment");
        Assert.That(stats.AB, Is.EqualTo(1), "AB should increment for error");
        Assert.That(stats.H, Is.EqualTo(0), "H should NOT increment for error");
        Assert.That(stats.RBI, Is.EqualTo(0), "RBI should be 0 for ROE per Rule 9.06(g)");
        Assert.That(stats.TB, Is.EqualTo(0), "TB should be 0 for error");
    }

    [Test]
    public void IncrementBatterStats_Double_IncrementsCorrectStats() {
        // Arrange
        var team = Team.Home;
        int lineupPosition = 2;
        var paType = PaType.Double;
        int runsScored = 2; // Two runners scored
        int rbiDelta = 2;
        bool batterScored = false;

        // Act
        _boxScore.IncrementBatterStats(team, lineupPosition, paType, runsScored, rbiDelta, batterScored);

        // Assert
        var stats = _boxScore.HomeBatters[lineupPosition];
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
        var team = Team.Away;
        int lineupPosition = 4;

        // Act - Simulate multiple PAs
        _boxScore.IncrementBatterStats(team, lineupPosition, PaType.Single, 0, 0, false);
        _boxScore.IncrementBatterStats(team, lineupPosition, PaType.BB, 0, 0, false);
        _boxScore.IncrementBatterStats(team, lineupPosition, PaType.HomeRun, 1, 1, true);
        _boxScore.IncrementBatterStats(team, lineupPosition, PaType.K, 0, 0, false);

        // Assert
        var stats = _boxScore.AwayBatters[lineupPosition];
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
        var team = Team.Home; // Home team pitching
        int pitcherId = 0;
        var paType = PaType.K;
        int outsAdded = 1;
        int runsScored = 0;

        // Act
        _boxScore.IncrementPitcherStats(team, pitcherId, paType, outsAdded, runsScored);

        // Assert
        var stats = _boxScore.HomePitchers[pitcherId];
        Assert.That(stats.BF, Is.EqualTo(1), "BF should increment");
        Assert.That(stats.OutsRecorded, Is.EqualTo(1), "OutsRecorded should be 1");
        Assert.That(stats.K, Is.EqualTo(1), "K should increment");
        Assert.That(stats.H, Is.EqualTo(0), "H should be 0");
        Assert.That(stats.R, Is.EqualTo(0), "R should be 0");
    }

    [Test]
    public void IncrementPitcherStats_HomeRun_IncrementsCorrectStats() {
        // Arrange
        var team = Team.Away; // Away team pitching
        int pitcherId = 0;
        var paType = PaType.HomeRun;
        int outsAdded = 0;
        int runsScored = 2; // 2-run HR

        // Act
        _boxScore.IncrementPitcherStats(team, pitcherId, paType, outsAdded, runsScored);

        // Assert
        var stats = _boxScore.AwayPitchers[pitcherId];
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
        var team = Team.Home;
        int pitcherId = 0;
        var paType = PaType.BB;
        int outsAdded = 0;
        int runsScored = 0;

        // Act
        _boxScore.IncrementPitcherStats(team, pitcherId, paType, outsAdded, runsScored);

        // Assert
        var stats = _boxScore.HomePitchers[pitcherId];
        Assert.That(stats.BF, Is.EqualTo(1), "BF should increment");
        Assert.That(stats.BB, Is.EqualTo(1), "BB should increment");
        Assert.That(stats.H, Is.EqualTo(0), "H should be 0 for walk");
        Assert.That(stats.OutsRecorded, Is.EqualTo(0), "OutsRecorded should be 0");
    }

    [Test]
    public void ValidateTeamHits_MatchingHits_ReturnsTrue() {
        // Arrange
        _boxScore.IncrementBatterStats(Team.Away, 0, PaType.Single, 0, 0, false);
        _boxScore.IncrementBatterStats(Team.Away, 1, PaType.Double, 0, 0, false);
        _boxScore.IncrementBatterStats(Team.Away, 2, PaType.HomeRun, 1, 1, true);

        // Act
        bool isValid = _boxScore.ValidateTeamHits(Team.Away, 3);

        // Assert
        Assert.That(isValid, Is.True, "Team hits should match sum of individual hits");
    }

    [Test]
    public void ValidateTeamHits_MismatchedHits_ReturnsFalse() {
        // Arrange
        _boxScore.IncrementBatterStats(Team.Home, 0, PaType.Single, 0, 0, false);
        _boxScore.IncrementBatterStats(Team.Home, 1, PaType.Double, 0, 0, false);

        // Act
        bool isValid = _boxScore.ValidateTeamHits(Team.Home, 5); // Wrong expected value

        // Assert
        Assert.That(isValid, Is.False, "Validation should fail when hits don't match");
    }

    [Test]
    public void ValidateDefensiveOuts_MatchingOuts_ReturnsTrue() {
        // Arrange
        _boxScore.IncrementPitcherStats(Team.Away, 0, PaType.K, 1, 0);
        _boxScore.IncrementPitcherStats(Team.Away, 0, PaType.InPlayOut, 1, 0);
        _boxScore.IncrementPitcherStats(Team.Away, 0, PaType.InPlayOut, 2, 0); // Double play

        // Act
        bool isValid = _boxScore.ValidateDefensiveOuts(Team.Away, 4);

        // Assert
        Assert.That(isValid, Is.True, "Defensive outs should match sum of pitcher outs");
    }

    [Test]
    public void GetTotalPitcherOuts_ReturnsCorrectSum() {
        // Arrange
        _boxScore.IncrementPitcherStats(Team.Home, 0, PaType.K, 1, 0);
        _boxScore.IncrementPitcherStats(Team.Home, 0, PaType.K, 1, 0);
        _boxScore.IncrementPitcherStats(Team.Home, 0, PaType.InPlayOut, 1, 0);

        // Act
        int totalOuts = _boxScore.GetTotalPitcherOuts(Team.Home);

        // Assert
        Assert.That(totalOuts, Is.EqualTo(3), "Total pitcher outs should be 3");
    }

    [Test]
    public void IncrementBatterStats_HBP_DoesNotIncrementAB() {
        // Arrange
        var team = Team.Away;
        int lineupPosition = 6;
        var paType = PaType.HBP;
        int runsScored = 0;
        int rbiDelta = 0;
        bool batterScored = false;

        // Act
        _boxScore.IncrementBatterStats(team, lineupPosition, paType, runsScored, rbiDelta, batterScored);

        // Assert
        var stats = _boxScore.AwayBatters[lineupPosition];
        Assert.That(stats.PA, Is.EqualTo(1), "PA should increment");
        Assert.That(stats.AB, Is.EqualTo(0), "AB should NOT increment for HBP");
        Assert.That(stats.HBP, Is.EqualTo(1), "HBP should increment");
        Assert.That(stats.TB, Is.EqualTo(0), "TB should be 0 for HBP");
    }

    [Test]
    public void IncrementBatterStats_Triple_IncrementsCorrectStats() {
        // Arrange
        var team = Team.Home;
        int lineupPosition = 7;
        var paType = PaType.Triple;
        int runsScored = 1;
        int rbiDelta = 1;
        bool batterScored = false;

        // Act
        _boxScore.IncrementBatterStats(team, lineupPosition, paType, runsScored, rbiDelta, batterScored);

        // Assert
        var stats = _boxScore.HomeBatters[lineupPosition];
        Assert.That(stats.PA, Is.EqualTo(1), "PA should increment");
        Assert.That(stats.AB, Is.EqualTo(1), "AB should increment for triple");
        Assert.That(stats.H, Is.EqualTo(1), "H should increment for triple");
        Assert.That(stats.Triples, Is.EqualTo(1), "Triples should increment");
        Assert.That(stats.TB, Is.EqualTo(3), "TB should be 3 for triple");
        Assert.That(stats.RBI, Is.EqualTo(1), "RBI should be 1");
    }
}
