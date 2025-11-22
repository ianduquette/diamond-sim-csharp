# PRD: BaseRunnerAdvancementTests Refactoring & Relocation

**Date:** 2025-11-22
**Status:** Proposed
**Priority:** Medium
**Category:** Test Organization & Quality

## 1. Executive Summary

`BaseRunnerAdvancementTests.cs` is currently misplaced in the `Scoring/` directory, violates the "ONE ACT PER TEST" rule, and has insufficient test coverage. This PRD outlines a plan to relocate, refactor, and expand this test file to properly test the `BaseRunnerAdvancement` class using probabilistic testing patterns.

## 2. Current State Analysis

### 2.1 What BaseRunnerAdvancementTests.cs Does
- **Purpose:** Tests the `BaseRunnerAdvancement` class using probabilistic sampling
- **Current Location:** `tests/DiamondSim.Tests/Scoring/BaseRunnerAdvancementTests.cs` ❌
- **Test Count:** 1 test only
- **Test Name:** `DoublePlay_Should_Only_Happen_On_Grounders`
- **Testing Method:** Seeded RNG with 25,000 trials per BIP type (75,000 total)

### 2.2 What BaseRunnerAdvancement Class Does
The `BaseRunnerAdvancement` class (located at `src/DiamondSim/BaseRunnerAdvancement.cs`) is responsible for:
- Determining how runners advance on balls in play
- Calculating double play probabilities based on BIP type (ground ball, fly ball, line drive)
- Resolving runner movements for different hit types
- **This is PROBABILISTIC GAME MECHANICS** (like BallInPlayResolver, ContactResolver)

### 2.3 Repository Structure Analysis

**Existing Test Directories:**
- `tests/DiamondSim.Tests/Probabilities/` - Contains:
  - `BallInPlayTests.cs` - Tests `BallInPlayResolver` (probabilistic BIP outcomes)
  - `ContactRateBaselineTests.cs` - Tests contact rate probabilities
  - `CountContactTests.cs` - Tests count-conditioned contact
  - `AtBatLoopTests.cs` - Tests at-bat loop probabilities

**Pattern:** All probabilistic game mechanics tests are in `Probabilities/`

### 2.4 Critical Issues

#### Issue #1: Wrong Directory ❌
- **Current:** `tests/DiamondSim.Tests/Scoring/`
- **Problem:** This tests probabilistic game mechanics (runner advancement), NOT scoring
- **Correct Location:** `tests/DiamondSim.Tests/Probabilities/`
- **Rationale:**
  - Uses seeded RNG with statistical sampling (same pattern as `BallInPlayTests.cs`)
  - Tests probabilistic outcomes of game mechanics
  - Belongs alongside other probabilistic mechanics tests

#### Issue #2: Violates "ONE ACT PER TEST" Rule ❌
Current test has **THREE Acts**:
```csharp
// Act 1: Test ground balls (25,000 iterations) - lines 29-43
for (int i = 0; i < trials; i++) { ... }

// Act 2: Test fly balls (25,000 iterations) - lines 46-60
for (int i = 0; i < trials; i++) { ... }

// Act 3: Test line drives (25,000 iterations) - lines 63-77
for (int i = 0; i < trials; i++) { ... }
```

**Should be THREE separate tests:**
1. `DoublePlay_OccursOnGroundBalls` - Verifies DP probability > 0 for ground balls
2. `DoublePlay_NeverOccursOnFlyBalls` - Verifies DP probability = 0 for fly balls
3. `DoublePlay_NeverOccursOnLineDrives` - Verifies DP probability = 0 for line drives

#### Issue #3: Missing ExecuteSut Pattern ❌
The test has inline simulation loops instead of using the `ExecuteSut` pattern that's standard in all other probabilistic tests (see `BallInPlayTests.cs`, `ContactRateBaselineTests.cs`).

#### Issue #4: Insufficient Test Coverage ⚠️
For a class called `BaseRunnerAdvancement`, having only ONE test (about double plays) is inadequate.

**Gap Analysis - Comparing to BallInPlayTests.cs:**
- `BallInPlayTests.cs` has 11 tests covering various power/stuff combinations
- `BaseRunnerAdvancementTests.cs` has 1 test covering only DP rules

**Missing Coverage:**
- Sacrifice fly advancement rules (runner must tag up)
- Runner advancement on singles (R1→3rd probability, R2→home probability)
- Runner advancement on doubles
- Runner advancement on triples
- Bases-loaded scenarios
- Multiple runners advancement
- Error-based advancement probabilities
- Force play scenarios

#### Issue #5: No Coverage Overlap with BallInPlayTests ✅
**Good News:** After scanning `BallInPlayTests.cs`, there is NO overlap:
- `BallInPlayTests.cs` tests `BallInPlayResolver` (what TYPE of hit: single, double, HR, out)
- `BaseRunnerAdvancementTests.cs` tests `BaseRunnerAdvancement` (HOW runners move on those hits)
- These are complementary, not redundant

## 3. Proposed Solution

### 3.1 File Relocation
**Move:** `tests/DiamondSim.Tests/Scoring/BaseRunnerAdvancementTests.cs`
**To:** `tests/DiamondSim.Tests/Probabilities/BaseRunnerAdvancementTests.cs`

**Rationale:**
- `BaseRunnerAdvancement` is part of the ball-in-play resolution pipeline (PRD-03)
- Uses probabilistic testing with seeded RNG (same pattern as `BallInPlayTests.cs`)
- The `Probabilities/` directory contains all probabilistic game mechanics tests
- Consistent with existing test organization

### 3.2 Refactor Existing Test

#### Current Structure (WRONG):
```csharp
[Test]
public void DoublePlay_Should_Only_Happen_On_Grounders() {
    // Arrange
    const int trials = 25_000;

    // Act 1: Ground balls
    for (int i = 0; i < trials; i++) { ... }

    // Act 2: Fly balls
    for (int i = 0; i < trials; i++) { ... }

    // Act 3: Line drives
    for (int i = 0; i < trials; i++) { ... }

    // Assert all three
}
```

#### Proposed Structure (CORRECT):
```csharp
[TestFixture]
public class BaseRunnerAdvancementTests {
    private const int Seed = 99999;
    private const int Trials = 10_000; // Reduced from 25K

    [Test]
    public void DoublePlay_OccursOnGroundBalls() {
        // Act
        var dpCount = ExecuteSut(BipType.GroundBall);

        // Assert
        Assert.That(dpCount, Is.GreaterThan(0), "DPs should occur on ground balls");
        var dpRate = (double)dpCount / Trials;
        Assert.That(dpRate, Is.InRange(0.10, 0.30), "DP rate should be 10-30% on ground balls with R1");
    }

    [Test]
    public void DoublePlay_NeverOccursOnFlyBalls() {
        // Act
        var dpCount = ExecuteSut(BipType.FlyBall);

        // Assert
        Assert.That(dpCount, Is.EqualTo(0), "DPs should NEVER occur on fly balls");
    }

    [Test]
    public void DoublePlay_NeverOccursOnLineDrives() {
        // Act
        var dpCount = ExecuteSut(BipType.LineDrive);

        // Assert
        Assert.That(dpCount, Is.EqualTo(0), "DPs should NEVER occur on line drives");
    }

    /// <summary>
    /// Executes the System Under Test (SUT) - counts double plays for a given BIP type.
    /// </summary>
    private static int ExecuteSut(BipType bipType) {
        var rng = new SeededRandom(Seed);
        var advancement = new BaseRunnerAdvancement();
        var basesWithR1 = new BaseState(OnFirst: true, OnSecond: false, OnThird: false);
        int currentOuts = 1;
        int dpCount = 0;

        for (int i = 0; i < Trials; i++) {
            var resolution = advancement.Resolve(
                AtBatTerminal.BallInPlay,
                BipOutcome.Out,
                bipType,
                basesWithR1,
                currentOuts,
                rng
            );

            if (resolution.Flags?.IsDoublePlay ?? false) {
                dpCount++;
            }
        }

        return dpCount;
    }
}
```

### 3.3 Add Comprehensive Test Coverage (Future Phase)

#### Sacrifice Fly Tests:
```csharp
[Test]
public void SacrificeFly_RunnerOnThird_ScoresFromThird() {
    // Test that R3 scores on sac fly
}

[Test]
public void SacrificeFly_RunnerOnSecond_DoesNotAdvance() {
    // Test that R2 stays on 2nd (must tag up)
}
```

#### Single Advancement Tests:
```csharp
[Test]
public void Single_RunnerOnFirst_AdvancesToThird_Probability() {
    // Test R1→3rd advancement rate on singles
}

[Test]
public void Single_RunnerOnSecond_ScoresHome_Probability() {
    // Test R2→home advancement rate on singles
}
```

#### Double Advancement Tests:
```csharp
[Test]
public void Double_RunnerOnFirst_ScoresHome_Probability() {
    // Test R1→home advancement rate on doubles
}
```

#### Error-Based Advancement Tests:
```csharp
[Test]
public void Error_AllowsExtraBase_Probability() {
    // Test error-based advancement probabilities
}
```

## 4. Implementation Plan

### Phase 1: Immediate Fixes (Current PR)
1. ✅ Create this PRD document
2. Move `BaseRunnerAdvancementTests.cs` from `Scoring/` to `Probabilities/`
3. Split single test into 3 separate tests (ONE ACT each)
4. Add `ExecuteSut` helper method following `BallInPlayTests.cs` pattern
5. Reduce iterations from 25,000 to 10,000 per test (30% faster, still statistically valid)
6. Add class-level constants for `Seed` and `Trials`
7. Add range assertion for ground ball DP rate (not just > 0)
8. Verify all tests pass

### Phase 2: Expand Coverage (Future PR)
9. Add sacrifice fly advancement tests
10. Add single/double/triple advancement probability tests
11. Add error-based advancement tests
12. Add bases-loaded scenario tests
13. Add force play logic tests

## 5. Success Criteria

### Phase 1:
- ✅ File in correct directory (`Probabilities/`)
- ✅ 3 separate tests (one Act each)
- ✅ All tests pass
- ✅ Follows test-writing-guidelines.md
- ✅ Uses `ExecuteSut` pattern
- ✅ Reduced test execution time (~60% faster: 30K iterations vs 75K)
- ✅ Class-level constants for seed and trials

### Phase 2:
- ✅ Comprehensive coverage of `BaseRunnerAdvancement` class
- ✅ All advancement scenarios tested probabilistically
- ✅ Clear documentation of MLB rules being tested

## 6. Files Affected

### Phase 1:
**Created:**
- `tests/DiamondSim.Tests/Probabilities/BaseRunnerAdvancementTests.cs` (moved & refactored)

**Deleted:**
- `tests/DiamondSim.Tests/Scoring/BaseRunnerAdvancementTests.cs` (old location)

### Phase 2:
**Modified:**
- `tests/DiamondSim.Tests/Probabilities/BaseRunnerAdvancementTests.cs` (expanded coverage)

## 7. Related PRDs
- PRD-03: Ball In Play Resolution (20251024_03_BallInPlayResolution.md)
- Test Refactoring: 20251121_01_scoring-tests-refactoring.md

## 8. Technical Notes

### Why Probabilities/ Not Model/?
- `Model/` contains tests for data structures (`GameState`)
- `Probabilities/` contains tests for probabilistic game mechanics
- `BaseRunnerAdvancement` uses RNG and produces probabilistic outcomes
- Pattern matches `BallInPlayTests.cs` exactly

### Why 10,000 Trials is Sufficient:
- Statistical confidence for binary outcomes (DP yes/no)
- Matches pattern in other probabilistic tests
- Faster test execution improves developer experience
- Still provides adequate confidence for assertions

### ExecuteSut Pattern Benefits:
- Encapsulates simulation loop
- Makes test intent clearer
- Easier to maintain
- Consistent with other probabilistic tests
