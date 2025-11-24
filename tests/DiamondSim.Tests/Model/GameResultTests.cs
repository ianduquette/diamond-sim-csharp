using DiamondSim.Tests.TestHelpers;

namespace DiamondSim.Tests.Model;

[TestFixture]
public class GameResultTests {
    [Test]
    public void Constructor_WithValidData_SetsAllProperties() {
        // Arrange
        var metadata = CreateTestMetadata();
        var boxScore = new BoxScore();
        var lineScore = new LineScore();
        var playLog = new List<PlayLogEntry>().AsReadOnly();
        var finalState = GameStateTestHelper.CreateGameState(
            inning: 9,
            half: InningHalf.Bottom,
            outs: 0,
            awayScore: 3,
            homeScore: 5,
            isFinal: true
        );
        var homeLineup = CreateTestLineup("Home");
        var awayLineup = CreateTestLineup("Away");

        // Act
        var result = new GameResult(
            metadata,
            boxScore,
            lineScore,
            playLog,
            finalState,
            homeLineup,
            awayLineup
        );

        // Assert
        Assert.That(result.Metadata, Is.EqualTo(metadata));
        Assert.That(result.BoxScore, Is.EqualTo(boxScore));
        Assert.That(result.LineScore, Is.EqualTo(lineScore));
        Assert.That(result.PlayLog, Is.EqualTo(playLog));
        Assert.That(result.FinalState, Is.EqualTo(finalState));
        Assert.That(result.HomeLineup, Is.EqualTo(homeLineup));
        Assert.That(result.AwayLineup, Is.EqualTo(awayLineup));
    }

    [Test]
    public void Constructor_WithNullMetadata_ThrowsArgumentNullException() {
        var boxScore = new BoxScore();
        var lineScore = new LineScore();
        var playLog = new List<PlayLogEntry>().AsReadOnly();
        var finalState = GameStateTestHelper.CreateGameState(isFinal: true);
        var homeLineup = CreateTestLineup("Home");
        var awayLineup = CreateTestLineup("Away");

        Assert.Throws<ArgumentNullException>(() =>
            new GameResult(null!, boxScore, lineScore, playLog, finalState, homeLineup, awayLineup));
    }

    [Test]
    public void Constructor_WithNullBoxScore_ThrowsArgumentNullException() {
        var metadata = CreateTestMetadata();
        var lineScore = new LineScore();
        var playLog = new List<PlayLogEntry>().AsReadOnly();
        var finalState = GameStateTestHelper.CreateGameState(isFinal: true);
        var homeLineup = CreateTestLineup("Home");
        var awayLineup = CreateTestLineup("Away");

        Assert.Throws<ArgumentNullException>(() =>
            new GameResult(metadata, null!, lineScore, playLog, finalState, homeLineup, awayLineup));
    }

    [Test]
    public void Constructor_WithNullLineScore_ThrowsArgumentNullException() {
        var metadata = CreateTestMetadata();
        var boxScore = new BoxScore();
        var playLog = new List<PlayLogEntry>().AsReadOnly();
        var finalState = GameStateTestHelper.CreateGameState(isFinal: true);
        var homeLineup = CreateTestLineup("Home");
        var awayLineup = CreateTestLineup("Away");

        Assert.Throws<ArgumentNullException>(() =>
            new GameResult(metadata, boxScore, null!, playLog, finalState, homeLineup, awayLineup));
    }

    [Test]
    public void Constructor_WithNullPlayLog_ThrowsArgumentNullException() {
        var metadata = CreateTestMetadata();
        var boxScore = new BoxScore();
        var lineScore = new LineScore();
        var finalState = GameStateTestHelper.CreateGameState(isFinal: true);
        var homeLineup = CreateTestLineup("Home");
        var awayLineup = CreateTestLineup("Away");

        Assert.Throws<ArgumentNullException>(() =>
            new GameResult(metadata, boxScore, lineScore, null!, finalState, homeLineup, awayLineup));
    }

    [Test]
    public void Constructor_WithNullFinalState_ThrowsArgumentNullException() {
        var metadata = CreateTestMetadata();
        var boxScore = new BoxScore();
        var lineScore = new LineScore();
        var playLog = new List<PlayLogEntry>().AsReadOnly();
        var homeLineup = CreateTestLineup("Home");
        var awayLineup = CreateTestLineup("Away");

        Assert.Throws<ArgumentNullException>(() =>
            new GameResult(metadata, boxScore, lineScore, playLog, null!, homeLineup, awayLineup));
    }

    [Test]
    public void Constructor_WithNullHomeLineup_ThrowsArgumentNullException() {
        var metadata = CreateTestMetadata();
        var boxScore = new BoxScore();
        var lineScore = new LineScore();
        var playLog = new List<PlayLogEntry>().AsReadOnly();
        var finalState = GameStateTestHelper.CreateGameState(isFinal: true);
        var awayLineup = CreateTestLineup("Away");

        Assert.Throws<ArgumentNullException>(() =>
            new GameResult(metadata, boxScore, lineScore, playLog, finalState, null!, awayLineup));
    }

    [Test]
    public void Constructor_WithNullAwayLineup_ThrowsArgumentNullException() {
        var metadata = CreateTestMetadata();
        var boxScore = new BoxScore();
        var lineScore = new LineScore();
        var playLog = new List<PlayLogEntry>().AsReadOnly();
        var finalState = GameStateTestHelper.CreateGameState(isFinal: true);
        var homeLineup = CreateTestLineup("Home");

        Assert.Throws<ArgumentNullException>(() =>
            new GameResult(metadata, boxScore, lineScore, playLog, finalState, homeLineup, null!));
    }

    private GameMetadata CreateTestMetadata() {
        return new GameMetadata("Home", "Away", 42, DateTime.Now);
    }

    private TeamLineup CreateTestLineup(string teamName) {
        var batters = Enumerable.Range(1, 9)
            .Select(i => new Batter($"{teamName} {i}", BatterRatings.Average))
            .ToList()
            .AsReadOnly();
        return new TeamLineup(teamName, batters);
    }

    [Test]
    public void LogHash_IsValid64CharHexString() {
        // Arrange
        var result = CreateValidGameResult();

        // Act
        var logHash = result.LogHash;

        // Assert - LogHash should be 64-character hex string (SHA-256)
        Assert.That(logHash, Is.Not.Null, "LogHash should not be null");
        Assert.That(logHash, Has.Length.EqualTo(64), "LogHash should be 64 characters (SHA-256)");
        Assert.That(logHash, Does.Match("^[0-9a-f]{64}$"), "LogHash should be lowercase hex");
    }

    [Test]
    public void LogHash_SamePlayLogAndScore_ProducesSameHash() {
        // Arrange - Create two GameResults with identical PlayLog and FinalScore
        var playLog = CreateTestPlayLog();
        var finalState = GameStateTestHelper.CreateGameState(
            inning: 9,
            half: InningHalf.Bottom,
            awayScore: 5,
            homeScore: 3,
            isFinal: true
        );

        var result1 = CreateGameResultWithPlayLogAndState(playLog, finalState);
        var result2 = CreateGameResultWithPlayLogAndState(playLog, finalState);

        // Act & Assert - LogHash should be identical
        Assert.That(result2.LogHash, Is.EqualTo(result1.LogHash),
            "Same PlayLog and FinalScore should produce same LogHash");
    }

    private GameResult CreateValidGameResult() {
        var metadata = CreateTestMetadata();
        var boxScore = new BoxScore();
        var lineScore = new LineScore();
        var playLog = CreateTestPlayLog();
        var finalState = GameStateTestHelper.CreateGameState(isFinal: true);
        var homeLineup = CreateTestLineup("Home");
        var awayLineup = CreateTestLineup("Away");

        return new GameResult(metadata, boxScore, lineScore, playLog, finalState, homeLineup, awayLineup);
    }

    private GameResult CreateGameResultWithPlayLogAndState(IReadOnlyList<PlayLogEntry> playLog, GameState finalState) {
        var metadata = CreateTestMetadata();
        var boxScore = new BoxScore();
        var lineScore = new LineScore();
        var homeLineup = CreateTestLineup("Home");
        var awayLineup = CreateTestLineup("Away");

        return new GameResult(metadata, boxScore, lineScore, playLog, finalState, homeLineup, awayLineup);
    }

    private IReadOnlyList<PlayLogEntry> CreateTestPlayLog() {
        var resolution = new PaResolution(
            OutsAdded: 1,
            RunsScored: 0,
            NewBases: new BaseState(false, false, false),
            Type: PaType.K,
            Tag: OutcomeTag.K
        );
        return new List<PlayLogEntry> {
            new PlayLogEntry(1, InningHalf.Top, "Batter 1", "Team", resolution, false, 1),
            new PlayLogEntry(1, InningHalf.Top, "Batter 2", "Team", resolution, false, 2)
        }.AsReadOnly();
    }
}
