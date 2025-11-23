# PRD: Clean Up GameLoop Test Files

**Date:** 2025-11-23
**Priority:** Medium
**Type:** Technical Debt / Test Quality Improvement
**Estimated Effort:** 3-4 hours

## Problem Statement

The GameLoop test directory contains 5 test files with various quality issues when measured against the project's [Test Writing Guidelines](.docs/test-writing-guidelines.md):

1. **CliContractTests.cs** - Uses CLI execution which should be refactored to use production code directly
2. **GameLoopTests.cs** - Multiple violations of "ONE ACT PER TEST" rule
3. **GameLoop_ExtrasAndWalkOffs_Tests.cs** - Good structure but could be improved
4. **GameReportValidationTests.cs** - Complex validation logic that could be better encapsulated
5. **SnapshotTests.cs** - Good structure but has redundancy

### Key Issues Identified

#### Critical Violations
- **CLI Execution in Tests**: CliContractTests.cs spawns processes to test the .exe - should use production code directly
- **Multiple Acts in Single Tests**: Several tests in `GameLoopTests.cs` run a game simulation AND then perform multiple parsing/extraction operations
- **Missing ExecuteSut Pattern**: Complex setup is repeated across tests without proper abstraction
- **Redundant Test Coverage**: Multiple tests verify the same behaviors with different approaches

#### Code Quality Issues
- **Helper Methods Not Encapsulated**: Parsing logic scattered across test methods
- **Magic Numbers**: Seeds and expected values not defined as constants where appropriate
- **Inconsistent Patterns**: Different files use different approaches for similar tasks

## Objectives

1. **Eliminate CLI Execution**: Refactor CliContractTests to use production code directly
2. **Align with Test Writing Guidelines**: Ensure all tests follow the ONE ACT PER TEST rule
3. **Eliminate Redundancy**: Consolidate overlapping test coverage
4. **Improve Maintainability**: Extract common patterns into reusable helpers
5. **Enhance Readability**: Use constants appropriately (not for literal values like inning numbers)
6. **Identify Coverage Gaps**: Document any missing test scenarios

## Detailed Analysis by File

### 1. CliContractTests.cs (183 lines)

**Status**: ❌ Needs Complete Refactoring

**Critical Issue**: Tests spawn external processes to run the .exe file. This violates the principle that tests should use production code directly.

**Current Approach (WRONG)**:
```csharp
[Test]
public void Cli_WithValidArguments_ExitsZero() {
    var args = "--home Sharks --away Comets --seed 42";
    var (exitCode, stdout, stderr) = RunCli(args);  // ❌ Spawns process
    Assert.That(exitCode, Is.EqualTo(0));
}
```

**Recommended Approach**:
```csharp
[TestFixture]
public class ArgumentParsingTests {
    [Test]
    public void ParseArguments_WithValidArguments_ReturnsConfig() {
        // Arrange
        var args = new[] { "--home", "Sharks", "--away", "Comets", "--seed", "42" };

        // Act
        var config = ArgumentParser.Parse(args);  // ✅ Uses production code

        // Assert
        Assert.That(config.HomeTeam, Is.EqualTo("Sharks"));
        Assert.That(config.AwayTeam, Is.EqualTo("Comets"));
        Assert.That(config.Seed, Is.EqualTo(42));
    }

    [Test]
    public void ParseArguments_MissingHomeTeam_ThrowsArgumentException() {
        // Arrange
        var args = new[] { "--away", "Comets", "--seed", "42" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ArgumentParser.Parse(args));
    }
}
```

**Required Production Code Changes**:
- Extract argument parsing logic from `Program.cs` into `ArgumentParser` class
- Make `ArgumentParser.Parse()` return a configuration object
- Make validation logic testable without process execution

**Estimated Changes**:
- Delete CliContractTests.cs (183 lines)
- Create ArgumentParsingTests.cs (~100 lines)
- Create ArgumentParser.cs in production code (~80 lines)

### 2. GameLoopTests.cs (232 lines)

**Status**: ⚠️ Needs Significant Refactoring

**Critical Issues**:

1. **Multiple Acts Per Test** (Lines 11-22, 25-41, 44-59, etc.):
```csharp
// WRONG - Two acts: RunGame() AND ExtractSection()
[Test]
public void BoxScore_AtBatCalculation_ExcludesWalksHbpAndSacFlies() {
    var simulator = new GameSimulator("Home", "Away", 42);
    string report = simulator.RunGame();  // ❌ First Act

    var lines = report.Split('\n');
    var battingSection = ExtractSection(lines, "BATTING", "PITCHING");  // ❌ Second Act
}
```

2. **Repeated Game Simulation**: Every test runs a full game simulation
3. **No ExecuteSut Pattern**: Complex setup repeated in each test
4. **Helper Method Misuse**: `ExtractSection` is used as part of Act, not Arrange

**Recommended Refactoring**:

```csharp
[TestFixture]
public class GameLoopTests {
    private const int TestSeed = 42;
    private const string HomeTeam = "Home";
    private const string AwayTeam = "Away";

    // Run game once in ExecuteSut, return parsed report
    private static GameReport ExecuteSut(int seed = TestSeed) {
        var simulator = new GameSimulator(HomeTeam, AwayTeam, seed);
        string report = simulator.RunGame();
        return new GameReport(report);
    }

    [Test]
    public void GameSimulation_CompletesSuccessfully() {
        // Act
        var report = ExecuteSut();

        // Assert
        Assert.That(report.RawText, Is.Not.Null);
        Assert.That(report.RawText, Does.Contain("Final:"));
    }

    [Test]
    public void BoxScore_AtBatCalculation_IsValid() {
        // Act
        var report = ExecuteSut();

        // Assert
        Assert.That(report.BattingSection, Does.Contain("AB"));
        Assert.That(report.BattingSection, Does.Contain("BB"));
    }

    // Helper class to encapsulate report parsing
    private class GameReport {
        public string RawText { get; }
        public string BattingSection { get; }
        public string PitchingSection { get; }
        public string LineScoreHeader { get; }

        public GameReport(string report) {
            RawText = report;
            var lines = report.Split('\n');
            BattingSection = ExtractSection(lines, "BATTING", "PITCHING");
            PitchingSection = ExtractSection(lines, "PITCHING", "Seed:");
        }

        private static string ExtractSection(string[] lines, string start, string end) {
            var sectionLines = new List<string>();
            bool inSection = false;
            foreach (var line in lines) {
                if (line.Contains(start)) inSection = true;
                if (inSection) sectionLines.Add(line);
                if (line.Contains(end) && inSection) break;
            }
            return string.Join('\n', sectionLines);
        }
    }
}
```

**Tests to Consolidate**:
- `GameSimulation_ProducesNineInnings_OrFewerForWalkoff` → Redundant with other tests
- `LineScore_HasNineInningColumns` + `LineScore_ShowsRunsHitsErrors` → Combine into one test
- `PlayLog_EachLineHasRequiredComponents` + `PlayLog_OutsPhrase_ShowsCorrectCount` → Combine

**Estimated Reduction**: 232 lines → ~150 lines (35% reduction)

### 3. GameLoop_ExtrasAndWalkOffs_Tests.cs (345 lines)

**Status**: ✅ Good Structure, Minor Improvements Needed

**Strengths**:
- Tests follow ONE ACT PER TEST rule
- Clear test names with good documentation
- Proper use of InningScorekeeper for unit testing

**Minor Issues**:
- Some repeated game state creation could be extracted
- Test data setup is verbose but acceptable for clarity

**Recommended Changes**:
- Extract common state creation into helper method
- Use constants only for values that have semantic meaning (like TiedScore)
- No major refactoring needed

**Example Improvement**:
```csharp
// ✅ Use existing GameStateTestHelper.CreateGameState() - no need for custom helper
[Test]
public void ProceedsToExtras_WhenTiedAfterNine() {
    // Arrange
    var scorekeeper = new InningScorekeeper();
    var state = GameStateTestHelper.CreateGameState(
        inning: 9,
        half: InningHalf.Bottom,
        outs: 2,
        awayScore: 3,
        homeScore: 3
    );
    // ... rest of test
}
```

**Note**: The existing `GameStateTestHelper.CreateGameState()` already provides sensible defaults and automatically sets offense/defense based on inning half. Tests should use this helper instead of creating custom state creation methods.

### 4. GameReportValidationTests.cs (644 lines)

**Status**: ⚠️ Needs Refactoring for Encapsulation

**Issues**:

1. **Validation Logic Not Encapsulated**: Lines 80-296 have repeated validation patterns
2. **Helper Classes at Bottom**: `BattingStats`, `PitchingStats`, `VerificationResults` should be in separate file
3. **Complex Parsing Logic**: Lines 317-411 could be methods on helper classes

**Recommended Refactoring**:

```csharp
// Move to separate file: GameReportValidator.cs in TestHelpers
public class GameReportValidator {
    private readonly string _report;
    private readonly GameReportParser _parser;

    public GameReportValidator(string report) {
        _report = report;
        _parser = new GameReportParser(report);
    }

    public ValidationResults Validate() {
        var results = new ValidationResults();
        ValidateFormulaConsistency(results);
        ValidateStatReconciliation(results);
        ValidateLogicalChecks(results);
        ValidateEdgeCases(results);
        return results;
    }

    private void ValidateFormulaConsistency(ValidationResults results) {
        foreach (var batter in _parser.GetBatters()) {
            batter.AssertFormulaConsistency(results);
        }
    }
}

// In test file:
[Test]
public void VerifyStatisticalConsistency_ForSeed(int seed) {
    // Act
    var report = ExecuteSut(seed);
    var validator = new GameReportValidator(report);
    var results = validator.Validate();

    // Assert
    Assert.That(results.AllChecksPassed, Is.True, results.GetFailureDetails());
}
```

**Estimated Reduction**: 644 lines → ~200 lines in test file + ~300 lines in validator class (better separation)

### 5. SnapshotTests.cs (188 lines)

**Status**: ✅ Good, Minor Redundancy

**Issues**:
- `GameSimulation_ProducesDeterministicOutput` (lines 19-50) overlaps with other tests
- `GameSimulation_ProducesValidLineScore` (lines 89-118) redundant with GameLoopTests
- `GameSimulation_ProducesValidBoxScore` (lines 121-145) redundant with GameLoopTests
- `GameSimulation_ProducesPlayByPlayLog` (lines 148-167) redundant with GameLoopTests

**Recommended Changes**:
- Keep determinism tests (LogHash comparison)
- Remove redundant structure validation tests (covered in GameLoopTests)
- Focus on snapshot-specific concerns

**Estimated Reduction**: 188 lines → ~100 lines (47% reduction)

## Coverage Gap Analysis

### Missing Test Scenarios

1. **Error Handling**:
   - No tests for malformed game states
   - No tests for invalid resolution data
   - No tests for edge cases in scoring logic

2. **Boundary Conditions**:
   - No tests for very high-scoring games (20+ runs)
   - No tests for no-hitter scenarios
   - No tests for perfect game scenarios

3. **Integration**:
   - Limited tests for full game flow with specific scenarios
   - No tests for specific play sequences

### Recommended New Tests

```csharp
[TestFixture]
public class GameLoopEdgeCaseTests {
    [Test]
    public void Game_WithNoHits_ProducesValidReport() {
        // Test no-hitter scenarios
    }

    [Test]
    public void Game_WithHighScoring_HandlesLargeNumbers() {
        // Test games with 20+ runs
    }

    [Test]
    public void Game_WithPerfectGame_ProducesValidReport() {
        // Test perfect game scenarios
    }
}
```

## Implementation Plan

### Phase 1: Refactor CliContractTests.cs (Highest Priority)
1. Extract argument parsing logic from Program.cs into ArgumentParser class
2. Create ArgumentParsingTests.cs to test parsing logic directly
3. Delete CliContractTests.cs
4. Ensure all argument validation is testable

**Estimated Time**: 1.5 hours

### Phase 2: Refactor GameLoopTests.cs
1. Create `GameReport` helper class to encapsulate parsing
2. Implement `ExecuteSut` pattern
3. Consolidate redundant tests
4. Update all tests to follow ONE ACT PER TEST rule
5. Add class-level constants for semantic values only

**Estimated Time**: 2 hours

### Phase 3: Extract GameReportValidator
1. Create new file `GameReportValidator.cs` in TestHelpers
2. Move validation logic from GameReportValidationTests
3. Encapsulate parsing logic in helper classes
4. Update tests to use new validator

**Estimated Time**: 1.5 hours

### Phase 4: Clean Up SnapshotTests.cs
1. Remove redundant tests
2. Focus on determinism verification
3. Consolidate snapshot storage logic

**Estimated Time**: 30 minutes

### Phase 5: Minor Improvements
1. Add helper methods to GameLoop_ExtrasAndWalkOffs_Tests.cs
2. Add missing test coverage for edge cases

**Estimated Time**: 1 hour

## Success Criteria

- [ ] No tests spawn external processes or execute CLI
- [ ] All tests follow ONE ACT PER TEST rule
- [ ] No redundant test coverage between files
- [ ] All complex parsing logic encapsulated in helper classes
- [ ] Constants used only for semantic values, not literal numbers
- [ ] Test file line counts reduced by 25-30%
- [ ] All tests pass after refactoring
- [ ] Code coverage maintained or improved
- [ ] Test execution time not significantly increased

## Risks & Mitigation

**Risk**: Breaking existing tests during refactoring
**Mitigation**: Refactor one file at a time, run tests after each change

**Risk**: Losing test coverage during consolidation
**Mitigation**: Document which tests are being consolidated and why

**Risk**: Making tests harder to understand
**Mitigation**: Add clear documentation and follow established patterns

## Related Files

- `.docs/test-writing-guidelines.md` - Test writing standards
- `tests/DiamondSim.Tests/GameLoop/CliContractTests.cs` - CLI tests (needs complete refactoring)
- `tests/DiamondSim.Tests/GameLoop/GameLoopTests.cs` - Main game loop tests (needs refactoring)
- `tests/DiamondSim.Tests/GameLoop/GameLoop_ExtrasAndWalkOffs_Tests.cs` - Extra innings tests
- `tests/DiamondSim.Tests/GameLoop/GameReportValidationTests.cs` - Report validation (needs refactoring)
- `tests/DiamondSim.Tests/GameLoop/SnapshotTests.cs` - Snapshot tests (needs cleanup)
- `src/DiamondSim/Program.cs` - Needs ArgumentParser extraction

## Appendix: Test Count Summary

| File | Current Tests | After Cleanup | Reduction |
|------|--------------|---------------|-----------|
| CliContractTests.cs | 8 | 0 (replaced) | 100% |
| ArgumentParsingTests.cs | 0 | 8 (new) | N/A |
| GameLoopTests.cs | 13 | 8 | 38% |
| GameLoop_ExtrasAndWalkOffs_Tests.cs | 9 | 9 | 0% |
| GameReportValidationTests.cs | 3 | 3 | 0% (but better organized) |
| SnapshotTests.cs | 8 | 4 | 50% |
| **Total** | **41** | **32** | **22%** |

**Total Line Reduction**: ~1,592 lines → ~1,050 lines (34% reduction)
