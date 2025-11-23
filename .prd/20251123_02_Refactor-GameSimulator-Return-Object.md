# PRD: Refactor GameSimulator to Return Rich Object (TDD Approach)

**Date:** 2025-11-23
**Priority:** High
**Type:** Architecture Refactoring / Technical Debt
**Estimated Effort:** 4-6 hours
**Author:** Senior Developer (TDD Practitioner)
**Approach:** RED → GREEN → REFACTOR (Test-Driven Development)

## Executive Summary

Refactor `GameSimulator.RunGame()` to return a rich `GameResult` object instead of a formatted string using strict Test-Driven Development. Each feature will be built incrementally: write failing test, make it pass with minimal code, refactor.

## Problem Statement

### Current Architecture (Anti-Pattern)

```csharp
public class GameSimulator {
    public string RunGame() {
        // ... simulate game ...
        var formatter = new GameReportFormatter(...);
        return formatter.FormatReport();  // ❌ Returns string
    }
}
```

**Critical Issues:**
1. **Violates SRP**: GameSimulator both simulates AND formats
2. **Untestable**: Tests must parse strings (brittle, slow)
3. **Inflexible**: Cannot reuse data for other outputs
4. **Tight Coupling**: Game logic coupled to console format

## Objectives

1. **Proper OO Design**: Return rich object with all game data
2. **TDD Discipline**: Every line of production code driven by a failing test
3. **Backward Compatibility**: Keep existing tests passing during migration
4. **Incremental Progress**: Small, safe steps with continuous green tests

## TDD Implementation Plan

### Phase 1: GameMetadata (Start Simple)

**Why Start Here?** Simplest data structure, no dependencies, easy to test.

#### Step 1.1: Test GameMetadata Constructor

**RED** - Write failing test:
```csharp
[TestFixture]
public class GameMetadataTests {
    [Test]
    public void Constructor_WithValidData_SetsProperties() {
        // Arrange
        var homeTeam = "Sharks";
        var awayTeam = "Comets";
        var seed = 42;
        var timestamp = DateTime.Now;
        var logHash = "abc123";

        // Act
        var metadata = new GameMetadata(homeTeam, awayTeam, seed, timestamp, logHash);

        // Assert
        Assert.That(metadata.HomeTeamName, Is.EqualTo(homeTeam));
        Assert.That(metadata.AwayTeamName, Is.EqualTo(awayTeam));
        Assert.That(metadata.Seed, Is.EqualTo(seed));
        Assert.That(metadata.Timestamp, Is.EqualTo(timestamp));
        Assert.That(metadata.LogHash, Is.EqualTo(logHash));
    }
}
```

**GREEN** - Minimal code to pass:
```csharp
public sealed class GameMetadata {
    public string HomeTeamName { get; init; }
    public string AwayTeamName { get; init; }
    public int Seed { get; init; }
    public DateTime Timestamp { get; init; }
    public string LogHash { get; init; }

    public GameMetadata(string homeTeamName, string awayTeamName, int seed, DateTime timestamp, string logHash) {
        HomeTeamName = homeTeamName;
        AwayTeamName = awayTeamName;
        Seed = seed;
        Timestamp = timestamp;
        LogHash = logHash;
    }
}
```

**REFACTOR** - Add validation:
```csharp
[Test]
public void Constructor_WithNullHomeTeam_ThrowsArgumentNullException() {
    Assert.Throws<ArgumentNullException>(() =>
        new GameMetadata(null!, "Away", 42, DateTime.Now, "hash"));
}
```

Then add null checks to constructor.

#### Step 1.2: Test GameMetadata Immutability

**RED** - Write test:
```csharp
[Test]
public void Properties_AreInitOnly_CannotBeModified() {
    var metadata = new GameMetadata("Home", "Away", 42, DateTime.Now, "hash");

    // This should not compile if properties are init-only
    // metadata.Seed = 99; // ❌ Compiler error

    Assert.Pass("Properties are init-only");
}
```

**GREEN** - Already passes (properties are `init`)

**REFACTOR** - None needed

### Phase 2: TeamLineup (Build on GameMetadata)

#### Step 2.1: Test TeamLineup Constructor

**RED** - Write failing test:
```csharp
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

    private IReadOnlyList<Batter> CreateTestBatters(int count) {
        return Enumerable.Range(1, count)
            .Select(i => new Batter($"Player {i}", BatterRatings.Average))
            .ToList()
            .AsReadOnly();
    }
}
```

**GREEN** - Minimal code:
```csharp
public sealed class TeamLineup {
    public string TeamName { get; init; }
    public IReadOnlyList<Batter> Batters { get; init; }

    public TeamLineup(string teamName, IReadOnlyList<Batter> batters) {
        TeamName = teamName;
        Batters = batters;
    }
}
```

#### Step 2.2: Test Lineup Validation

**RED** - Write test:
```csharp
[Test]
public void Constructor_WithWrongBatterCount_ThrowsArgumentException() {
    var batters = CreateTestBatters(8); // Wrong count

    Assert.Throws<ArgumentException>(() =>
        new TeamLineup("Team", batters));
}
```

**GREEN** - Add validation:
```csharp
public TeamLineup(string teamName, IReadOnlyList<Batter> batters) {
    TeamName = teamName ?? throw new ArgumentNullException(nameof(teamName));
    Batters = batters ?? throw new ArgumentNullException(nameof(batters));

    if (batters.Count != 9) {
        throw new ArgumentException("Lineup must have exactly 9 batters", nameof(batters));
    }
}
```

### Phase 3: PlayLogEntry (More Complex)

#### Step 3.1: Test PlayLogEntry Constructor

**RED** - Write test:
```csharp
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
        // ... etc
    }
}
```

**GREEN** - Minimal code (similar to previous)

#### Step 3.2: Test PlayLogEntry.ToDisplayString()

**RED** - Write test:
```csharp
[Test]
public void ToDisplayString_WithStrikeout_FormatsCorrectly() {
    // Arrange
    var entry = new PlayLogEntry(
        inning: 1,
        half: InningHalf.Top,
        batterName: "Player 1",
        pitchingTeamName: "Away",
        resolution: CreateStrikeoutResolution(),
        isWalkoff: false,
        outsAfter: 1
    );

    // Act
    var display = entry.ToDisplayString();

    // Assert
    Assert.That(display, Does.StartWith("[Top 1]"));
    Assert.That(display, Does.Contain("Player 1"));
    Assert.That(display, Does.Contain("vs Away P"));
    Assert.That(display, Does.Contain("Strikeout"));
    Assert.That(display, Does.Contain("1 out"));
}
```

**GREEN** - Implement `ToDisplayString()`:
```csharp
public string ToDisplayString() {
    var halfStr = Half == InningHalf.Top ? "Top" : "Bot";
    var prefix = IsWalkoff ? "WALK-OFF! " : "";
    var outcome = FormatOutcome(Resolution);
    var outsPhrase = Resolution.OutsAdded > 0 ? $" {OutsAfter} out{(OutsAfter == 1 ? "" : "s")}." : "";

    return $"[{halfStr} {Inning}] {BatterName} vs {PitchingTeamName} P — {prefix}{outcome}.{outsPhrase}";
}

private string FormatOutcome(PaResolution resolution) {
    // Minimal implementation for now
    return resolution.Tag.ToString();
}
```

**REFACTOR** - Extract formatting logic, add more tests for different outcomes

### Phase 4: GameResult (Compose Everything)

#### Step 4.1: Test GameResult Constructor

**RED** - Write test:
```csharp
[TestFixture]
public class GameResultTests {
    [Test]
    public void Constructor_WithValidData_SetsAllProperties() {
        // Arrange
        var metadata = CreateTestMetadata();
        var boxScore = new BoxScore();
        var lineScore = new LineScore();
        var playLog = new List<PlayLogEntry>().AsReadOnly();
        var finalState = CreateTestGameState();
        var homeLineup = CreateTestLineup("Home");
        var awayLineup = CreateTestLineup("Away");

        // Act
        var result = new GameResult(metadata, boxScore, lineScore,
            playLog, finalState, homeLineup, awayLineup);

        // Assert
        Assert.That(result.Metadata, Is.EqualTo(metadata));
        Assert.That(result.BoxScore, Is.EqualTo(boxScore));
        Assert.That(result.LineScore, Is.EqualTo(lineScore));
        Assert.That(result.PlayLog, Is.EqualTo(playLog));
        Assert.That(result.FinalState, Is.EqualTo(finalState));
        Assert.That(result.HomeLineup, Is.EqualTo(homeLineup));
        Assert.That(result.AwayLineup, Is.EqualTo(awayLineup));
    }
}
```

**GREEN** - Minimal code:
```csharp
public sealed class GameResult {
    public GameMetadata Metadata { get; init; }
    public BoxScore BoxScore { get; init; }
    public LineScore LineScore { get; init; }
    public IReadOnlyList<PlayLogEntry> PlayLog { get; init; }
    public GameState FinalState { get; init; }
    public TeamLineup HomeLineup { get; init; }
    public TeamLineup AwayLineup { get; init; }

    public GameResult(
        GameMetadata metadata,
        BoxScore boxScore,
        LineScore lineScore,
        IReadOnlyList<PlayLogEntry> playLog,
        GameState finalState,
        TeamLineup homeLineup,
        TeamLineup awayLineup) {

        Metadata = metadata;
        BoxScore = boxScore;
        LineScore = lineScore;
        PlayLog = playLog;
        FinalState = finalState;
        HomeLineup = homeLineup;
        AwayLineup = awayLineup;
    }
}
```

**REFACTOR** - Add null checks

### Phase 5: GameSimulator.RunGameV2() (The Big One)

#### Step 5.1: Test RunGameV2 Returns GameResult

**RED** - Write test:
```csharp
[TestFixture]
public class GameSimulatorV2Tests {
    [Test]
    public void RunGameV2_ReturnsGameResult() {
        // Arrange
        var simulator = new GameSimulator("Home", "Away", 42);

        // Act
        var result = simulator.RunGameV2();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<GameResult>());
    }
}
```

**GREEN** - Minimal code:
```csharp
public GameResult RunGameV2() {
    // For now, just return a minimal valid result
    return new GameResult(
        metadata: new GameMetadata("Home", "Away", _seed, DateTime.Now, "temp"),
        boxScore: new BoxScore(),
        lineScore: new LineScore(),
        playLog: new List<PlayLogEntry>().AsReadOnly(),
        finalState: new GameState(/* minimal state */),
        homeLineup: new TeamLineup("Home", CreateEmptyLineup()),
        awayLineup: new TeamLineup("Away", CreateEmptyLineup())
    );
}
```

**REFACTOR** - This will fail because we need actual game simulation. Next test...

#### Step 5.2: Test RunGameV2 Metadata Is Correct

**RED** - Write test:
```csharp
[Test]
public void RunGameV2_Metadata_HasCorrectTeamNames() {
    // Arrange
    var simulator = new GameSimulator("Sharks", "Comets", 42);

    // Act
    var result = simulator.RunGameV2();

    // Assert
    Assert.That(result.Metadata.HomeTeamName, Is.EqualTo("Sharks"));
    Assert.That(result.Metadata.AwayTeamName, Is.EqualTo("Comets"));
    Assert.That(result.Metadata.Seed, Is.EqualTo(42));
}
```

**GREEN** - Update RunGameV2 to use actual team names

#### Step 5.3: Test RunGameV2 Actually Simulates Game

**RED** - Write test:
```csharp
[Test]
public void RunGameV2_SimulatesCompleteGame() {
    // Arrange
    var simulator = new GameSimulator("Home", "Away", 42);

    // Act
    var result = simulator.RunGameV2();

    // Assert
    Assert.That(result.FinalState.IsFinal, Is.True);
    Assert.That(result.PlayLog.Count, Is.GreaterThan(0));
    Assert.That(result.BoxScore.AwayBatters.Count, Is.GreaterThan(0));
}
```

**GREEN** - Now implement actual game simulation in RunGameV2()

**REFACTOR** - Extract common logic, clean up

### Phase 6: Backward Compatibility

#### Step 6.1: Test RunGame() Still Works

**RED** - Write test:
```csharp
[Test]
public void RunGame_StillReturnsString() {
    // Arrange
    var simulator = new GameSimulator("Home", "Away", 42);

    // Act
    string report = simulator.RunGame();

    // Assert
    Assert.That(report, Is.Not.Null);
    Assert.That(report, Does.Contain("Final:"));
}
```

**GREEN** - Implement RunGame() as wrapper:
```csharp
public string RunGame() {
    return RunGameV2().ToConsoleReport();
}
```

#### Step 6.2: Test All Existing Tests Still Pass

**RED** - Run existing test suite (should all pass)

**GREEN** - If any fail, fix them

**REFACTOR** - Clean up any duplication

### Phase 7: Extension Method

#### Step 7.1: Test ToConsoleReport Extension

**RED** - Write test:
```csharp
[TestFixture]
public class GameResultExtensionsTests {
    [Test]
    public void ToConsoleReport_ReturnsFormattedString() {
        // Arrange
        var result = CreateTestGameResult();

        // Act
        string report = result.ToConsoleReport();

        // Assert
        Assert.That(report, Is.Not.Null);
        Assert.That(report, Does.Contain("Final:"));
        Assert.That(report, Does.Contain(result.Metadata.HomeTeamName));
    }
}
```

**GREEN** - Implement extension:
```csharp
public static class GameResultExtensions {
    public static string ToConsoleReport(this GameResult result) {
        var formatter = new GameReportFormatter(result);
        return formatter.Format();
    }
}
```

**REFACTOR** - Update GameReportFormatter to accept GameResult

### Phase 8: Formatter Strategy (Optional Future Enhancement)

**Note:** Phase 7 uses extension methods for simplicity. This phase describes an optional future enhancement using the Strategy pattern for better extensibility and dependency injection.

#### Option A: Extension Methods (Current Plan - Simplest)

```csharp
public static class GameResultExtensions {
    public static string ToConsoleReport(this GameResult result) {
        var formatter = new ConsoleReportFormatter(result);
        return formatter.Format();
    }

    public static string ToJson(this GameResult result) {
        return System.Text.Json.JsonSerializer.Serialize(result);
    }
}
```

**Pros:** Simple, easy to use, no DI needed
**Cons:** Hard to mock for testing, can't inject different formatters

#### Option B: Strategy Pattern with DI (Future Enhancement)

```csharp
/// <summary>
/// Strategy interface for formatting game results.
/// Enables dependency injection, testing, and runtime format selection.
/// </summary>
public interface IGameResultFormatter {
    string Format(GameResult result);
}

/// <summary>
/// Console text formatter (current format).
/// </summary>
public class ConsoleReportFormatter : IGameResultFormatter {
    public string Format(GameResult result) {
        // Current GameReportFormatter logic
    }
}

/// <summary>
/// JSON formatter for API responses.
/// </summary>
public class JsonFormatter : IGameResultFormatter {
    public string Format(GameResult result) {
        return System.Text.Json.JsonSerializer.Serialize(result);
    }
}

/// <summary>
/// Database persistence formatter.
/// </summary>
public class DatabaseFormatter : IGameResultFormatter {
    private readonly IDbConnection _connection;

    public DatabaseFormatter(IDbConnection connection) {
        _connection = connection;
    }

    public string Format(GameResult result) {
        SaveToDatabase(result);
        return $"Game {result.Metadata.Seed} saved to database";
    }
}

// Usage with DI:
public class GameService {
    private readonly IGameResultFormatter _formatter;

    public GameService(IGameResultFormatter formatter) {
        _formatter = formatter;
    }

    public string RunAndFormatGame(string home, string away, int seed) {
        var simulator = new GameSimulator(home, away, seed);
        var result = simulator.RunGameV2();
        return _formatter.Format(result);
    }
}
```

**Pros:** Testable, injectable, extensible, runtime selection
**Cons:** More complex, requires DI container

**Recommendation:** Start with **Option A (Extension Methods)**. Migrate to **Option B (Strategy Pattern)** only if we need:
- Multiple output formats in production
- Dependency injection for testing formatters
- Runtime format selection based on configuration
- Complex formatting logic that benefits from separate classes

## TDD Principles Applied

### 1. Red-Green-Refactor Cycle
- **RED**: Write a failing test first
- **GREEN**: Write minimal code to make it pass
- **REFACTOR**: Clean up without changing behavior

### 2. Test One Thing at a Time
- Each test focuses on a single behavior
- Small, incremental steps
- Easy to debug when tests fail

### 3. Minimal Production Code
- Only write code needed to pass current test
- Don't anticipate future requirements
- YAGNI (You Aren't Gonna Need It)

### 4. Continuous Green Tests
- All tests pass after each step
- Never commit broken tests
- Backward compatibility maintained

### 5. Refactor Fearlessly
- Tests provide safety net
- Can improve design without breaking functionality
- Extract methods, rename, reorganize

## Success Criteria

- [ ] All new classes have 100% test coverage
- [ ] Every production code line driven by a failing test
- [ ] All 266+ existing tests still pass
- [ ] New API (`RunGameV2()`) fully functional
- [ ] Backward compatibility maintained (`RunGame()` works)
- [ ] No test skipped or ignored
- [ ] Clean, refactored code with no duplication

## Benefits of TDD Approach

### For Quality
- ✅ **High Confidence**: Every line tested
- ✅ **No Regressions**: Existing tests catch breaks
- ✅ **Better Design**: Tests force good design
- ✅ **Living Documentation**: Tests show how to use API

### For Process
- ✅ **Small Steps**: Easy to understand and review
- ✅ **Safe Refactoring**: Tests provide safety net
- ✅ **Clear Progress**: Green tests = done
- ✅ **Easy Debugging**: Know exactly what broke

### For Team
- ✅ **Reviewable**: Small, focused commits
- ✅ **Maintainable**: Tests explain intent
- ✅ **Extensible**: Easy to add features
- ✅ **Confidence**: Team trusts the code

## Implementation Order (TDD Steps)

1. **GameMetadata** (simplest, no dependencies)
2. **TeamLineup** (uses Batter, already exists)
3. **PlayLogEntry** (uses PaResolution, already exists)
4. **GameResult** (composes above)
5. **GameSimulator.RunGameV2()** (uses GameResult)
6. **GameSimulator.RunGame()** (wrapper for compatibility)
7. **GameResultExtensions.ToConsoleReport()** (formatting)
8. **GameReportFormatter** refactor (accept GameResult)
9. **Test Migration** (gradually update existing tests)

Each step follows RED → GREEN → REFACTOR.

## Related Documents

- Test Writing Guidelines: `.docs/test-writing-guidelines.md`
- Current GameSimulator: `src/DiamondSim/GameSimulator.cs`
- Previous Cleanup PRD: `.prd/20251123_01_Cleanup-GameLoop-Tests.md`

---

**Approval Required:** Yes
**Approach:** Test-Driven Development (TDD)
**Estimated Time:** 4-6 hours (including all tests)
**Breaking Changes:** None (backward compatible)
