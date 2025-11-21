# Scoring Tests Refactoring Plan

## Executive Summary

After analyzing all 6 test files in the `tests/DiamondSim.Tests/Scoring/` folder, this document provides a comprehensive plan for refactoring these tests to follow the established guidelines while maintaining their critical validation role.

## Current State Analysis

### Test Files Overview

1. **BoxScoreTests.cs** (339 lines, 18 tests)
   - Tests: Batter/pitcher stat tracking, validation methods
   - Issues: Uses `[SetUp]`, instance field `_boxScore`
   - Complexity: Simple - direct method calls

2. **LineScoreTests.cs** (686 lines, 24 tests)
   - Tests: Line score tracking, LOB calculation, walk-offs, skip bottom 9th
   - Issues: Uses `[SetUp]`, instance fields `_scorekeeper`, `_initialState`
   - Complexity: Medium - uses helper methods `ScoreRunsAndEndHalf()`, `PrefillThroughInning8Quietly()`

3. **InningScoreTests.cs** (1,187 lines, 30 tests)
   - Tests: Complete inning scoring logic, walk-offs, extras, skip bottom 9th, determinism
   - Issues: Uses `[SetUp]`, instance field `_scorekeeper`
   - Complexity: Medium - uses helper method `ScoreRunsAndEndHalf()`

4. **InningSummaryRegressionTests.cs** (140 lines, 2 tests)
   - Tests: Regression tests for specific bugs (third-out scoring, line score sums)
   - Issues: None - already clean
   - Complexity: Medium - includes parsing logic

5. **RbiAttributionTests.cs** (not read yet)
6. **EarnedRunTests.cs** (not read yet)
7. **BaseRunnerAdvancementTests.cs** (not read yet)
8. **WalkoffTests.cs** (not read yet)

## Critical Analysis: What Should We Do?

### ❌ DO NOT Combine Test Files

**Reasoning:**
- Each file tests a **distinct component** with different responsibilities:
  - `BoxScoreTests` → BoxScore class (player statistics)
  - `LineScoreTests` → LineScore class (inning-by-inning runs, LOB)
  - `InningScoreTests` → InningScorekeeper class (game state transitions)
  - `InningSummaryRegressionTests` → End-to-end regression validation

- These are **NOT redundant** - they test different layers:
  - BoxScore = Player-level statistics
  - LineScore = Team-level inning tracking
  - InningScorekeeper = Game flow orchestration

- Combining would create **massive, unmaintainable test files** (1,000+ lines)

### ✅ KEEP InningSummaryRegressionTests

**Reasoning:**
- **Regression tests are critical** - they prevent specific bugs from reoccurring
- Only 2 tests, 140 lines - not a maintenance burden
- Tests end-to-end behavior that unit tests might miss
- Already clean (no `[SetUp]`, no instance fields)
- Provides **safety net** for refactoring other tests

**Verdict: DO NOT DELETE OR MERGE**

### 🎯 What Actually Needs Fixing

The real issues are **consistent across all files**:
1. Remove `[SetUp]` methods
2. Remove instance fields
3. Create objects in Arrange section
4. Add class-level constants for magic numbers
5. Keep helper methods (they're valuable for readability)

## Refactoring Strategy

### Phase 1: Simple Refactors (BoxScoreTests)

**File:** `BoxScoreTests.cs`
**Effort:** Low
**Changes:**
- Remove `[SetUp]` and `_boxScore` field
- Create `new BoxScore()` in each test's Arrange
- Add constants: `LineupPosition0 = 0`, `PitcherId0 = 0`
- **NO ExecuteSut needed** - setup is trivial

**Example Before:**
```csharp
private BoxScore _boxScore = null!;

[SetUp]
public void Setup() {
    _boxScore = new BoxScore();
}

[Test]
public void IncrementBatterStats_Single_IncrementsCorrectStats() {
    var team = Team.Away;
    int lineupPosition = 0;
    // ...
    _boxScore.IncrementBatterStats(team, lineupPosition, ...);
}
```

**Example After:**
```csharp
private const int LineupPosition0 = 0;

[Test]
public void IncrementBatterStats_Single_IncrementsCorrectStats() {
    // Arrange
    var boxScore = new BoxScore();

    // Act
    boxScore.IncrementBatterStats(Team.Away, LineupPosition0, PaType.Single,
        runsScored: 0, rbiDelta: 0, batterScored: false);

    // Assert
    var stats = boxScore.AwayBatters[LineupPosition0];
    Assert.That(stats.H, Is.EqualTo(1));
}
```

### Phase 2: Medium Refactors (LineScoreTests, InningScoreTests)

**Files:** `LineScoreTests.cs`, `InningScoreTests.cs`
**Effort:** Medium
**Changes:**
- Remove `[SetUp]` and instance fields
- Create `new InningScorekeeper()` in each test's Arrange
- **KEEP helper methods** like `ScoreRunsAndEndHalf()` - they're essential for readability
- Make helper methods `static` and pass scorekeeper as parameter
- Add constants for common values

**Key Insight:** Helper methods are NOT a violation - they're **test utilities** that make complex scenarios readable.

**Example Before:**
```csharp
private InningScorekeeper _scorekeeper = null!;

[SetUp]
public void Setup() {
    _scorekeeper = new InningScorekeeper();
}

[Test]
public void Test() {
    var state = _initialState;
    state = ScoreRunsAndEndHalf(state, 2);
}

private GameState ScoreRunsAndEndHalf(GameState state, int runs) {
    // Uses _scorekeeper
}
```

**Example After:**
```csharp
[Test]
public void Test() {
    // Arrange
    var scorekeeper = new InningScorekeeper();
    var state = CreateInitialState();

    // Act
    state = ScoreRunsAndEndHalf(scorekeeper, state, 2);

    // Assert
    Assert.That(scorekeeper.LineScore.AwayTotal, Is.EqualTo(2));
}

private static GameState ScoreRunsAndEndHalf(InningScorekeeper scorekeeper,
    GameState state, int runs) {
    // Now takes scorekeeper as parameter
}

private static GameState CreateInitialState() {
    return new GameState(0, 0, inning: 1, half: InningHalf.Top, ...);
}
```

### Phase 3: Keep As-Is (InningSummaryRegressionTests)

**File:** `InningSummaryRegressionTests.cs`
**Effort:** None
**Changes:** None needed - already follows guidelines

## Missing Tests Analysis

### What's NOT Tested (Potential Gaps)

After reviewing the test files, here are areas that might need additional coverage:

1. **Error Handling**
   - What happens with invalid lineup positions?
   - What happens with negative runs/outs?
   - Boundary conditions (inning 100 is tested, but what about inning 0?)

2. **Edge Cases**
   - Multiple pitchers in one game (BoxScore)
   - Pinch hitters/runners (BoxScore)
   - Defensive substitutions (BoxScore)

3. **Integration Gaps**
   - BoxScore + LineScore consistency (partially tested)
   - Full game simulation with all components working together

**Recommendation:** These gaps are **acceptable** for current scope. The existing tests provide excellent coverage of core functionality. Additional tests can be added as bugs are discovered or new features are added.

## Implementation Order

### Priority 1: BoxScoreTests (Start Here)
- **Why:** Simplest refactor, establishes pattern
- **Effort:** 1-2 hours
- **Risk:** Low

### Priority 2: LineScoreTests
- **Why:** Medium complexity, important for line score display
- **Effort:** 2-3 hours
- **Risk:** Medium (many tests, complex scenarios)

### Priority 3: InningScoreTests
- **Why:** Most complex, most tests, but follows same pattern as LineScoreTests
- **Effort:** 3-4 hours
- **Risk:** Medium-High (critical game logic)

### Priority 4: Remaining Files (RbiAttributionTests, EarnedRunTests, etc.)
- **Why:** Need to read and assess first
- **Effort:** TBD
- **Risk:** TBD

## Success Criteria

### For Each Refactored File

✅ No `[SetUp]` methods
✅ No instance fields (except constants)
✅ Objects created in Arrange section
✅ Class-level constants for magic numbers
✅ Helper methods are static and take dependencies as parameters
✅ All tests still pass
✅ Test names clearly describe what's being tested
✅ One `// Assert` comment per test

### Overall Success

✅ All Scoring tests follow consistent pattern
✅ Tests are more maintainable (no hidden state)
✅ Tests are easier to understand (explicit dependencies)
✅ No loss of test coverage
✅ No new bugs introduced

## Risks & Mitigation

### Risk 1: Breaking Tests During Refactor
**Mitigation:**
- Refactor one file at a time
- Run tests after each change
- Use git to track changes and enable easy rollback

### Risk 2: Making Tests Less Readable
**Mitigation:**
- Keep helper methods (they improve readability)
- Use descriptive constant names
- Add comments where complexity is unavoidable

### Risk 3: Missing Subtle Bugs
**Mitigation:**
- InningSummaryRegressionTests provides safety net
- Run full test suite after each file refactor
- Review changes carefully before committing

## Conclusion

The Scoring tests are **well-structured and comprehensive**. They do NOT need to be combined or deleted. The refactoring needed is **mechanical and low-risk**:

1. Remove `[SetUp]` and instance fields
2. Create objects in Arrange
3. Make helper methods static with explicit parameters
4. Add constants for magic numbers

This will make the tests more maintainable while preserving their excellent coverage of critical game logic.

**Recommendation:** Proceed with refactoring in the order specified above, starting with BoxScoreTests to establish the pattern.
