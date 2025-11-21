# PRD: Fix Runs Mismatch Bug

**Date:** 2025-11-01
**Version:** 1.0
**Status:** Planning
**Priority:** Critical

## Problem Statement

From 200-game analysis, several games show data integrity violations where:
- Home batting runs ≠ Away pitching runs allowed
- Away batting runs ≠ Home pitching runs allowed

Examples from the analysis:
- Home scored 3, Away P shows only 2 runs allowed
- Home scored 7, Away P shows 8 runs allowed
- Home scored 4, Away P shows 5 runs allowed
- Home scored 1, Away P shows 2 runs allowed
- Home scored 2, Away P shows 3 runs allowed

## Root Cause Hypothesis

Based on code review of `InningScorekeeper.cs`, the likely causes are:

### 1. Walk-off Run Clamping Mismatch (Most Likely)
**Location:** `InningScorekeeper.cs:246-378` - `ApplyPlateAppearance()` method

**Issue:** Walk-off clamping may be applied inconsistently:
- Line 258: `ApplyWalkoffClamping()` clamps runs for the batting team
- Line 284-288: Clamped runs added to batting team score
- Line 342-348: `BoxScore.IncrementPitcherStats()` called with `clampedRuns`

**Potential Bug:** The pitcher stats increment happens AFTER walk-off detection, but the clamping logic might not be correctly synchronized. Specifically:
- If walk-off occurs, bases are cleared (lines 304-308)
- But pitcher stats are incremented with `clampedRuns` which should match batting runs
- The issue might be in how `resolution.RunsScored` vs `clampedRuns` are used

### 2. Earned/Unearned Run Classification Error
**Location:** `InningScorekeeper.cs:153-180` - `ClassifyRuns()` method

**Less Likely:** The ER calculation uses `clampedRuns`, but this shouldn't affect total R count.

### 3. RBI Calculation Side Effect
**Location:** `InningScorekeeper.cs:115-140` - `CalculateRbi()` method

**Unlikely:** RBI calculation doesn't affect run totals, only attribution.

## Investigation Plan

### Phase 1: Trigger the Bug (DONE ✅)
- [x] Added assertion in `GameReportFormatter.cs:259-277` to detect mismatches
- [x] Assertion will throw `InvalidOperationException` with detailed error message
- [ ] Run 200 games to collect failing seeds

### Phase 2: Reproduce & Analyze
1. **Collect Failing Seeds**
   ```powershell
   # Run 200 games - assertion will stop on first failure
   dotnet run --project src/DiamondSim/DiamondSim.csproj -- --home Home --away Away --seed <FAILING_SEED>
   ```

2. **Analyze Pattern**
   - Check if failures are walk-off games
   - Check if failures involve specific run-scoring scenarios
   - Compare `resolution.RunsScored` vs `clampedRuns` in failing cases

3. **Add Debug Logging** (if needed)
   - Temporarily add console output in `InningScorekeeper.ApplyPlateAppearance()`
   - Log: `resolution.RunsScored`, `clampedRuns`, `walkoffApplied`, batting team, defense team

### Phase 3: Fix Implementation

#### Option A: If Walk-off Clamping Issue
**Fix Location:** `InningScorekeeper.cs:342-348`

Ensure pitcher stats use the SAME clamped runs as batting stats:
```csharp
// BEFORE (line 342-348)
BoxScore.IncrementPitcherStats(
    team: state.Defense,
    pitcherId: 0,
    paType: resolution.Type,
    outsAdded: resolution.OutsAdded,
    runsScored: clampedRuns  // ✅ Already using clampedRuns
);
```

**Verify:** The code already uses `clampedRuns`. The bug might be elsewhere.

#### Option B: If Walk-off Base Clearing Issue
**Fix Location:** `InningScorekeeper.cs:298-308`

The walk-off path clears bases but might not be accounting for all runners:
```csharp
// Walk-off: clear bases (game ends mid-play)
newState.OnFirst = false;
newState.OnSecond = false;
newState.OnThird = false;
```

**Check:** Does `resolution.RunsScored` include ALL runners who should score, or does it get truncated?

#### Option C: If Resolution Calculation Issue
**Fix Location:** `BaseRunnerAdvancement.cs` (need to review)

The `PaResolution` might be calculating `RunsScored` incorrectly in certain scenarios.

### Phase 4: Add Regression Tests

**Test File:** `tests/DiamondSim.Tests/Scoring/RunsIntegrityTests.cs`

```csharp
[Fact]
public void ApplyPlateAppearance_WalkoffScenario_PitcherRunsMatchBattingRuns()
{
    // Arrange: Bottom 9th, home trailing by 1, bases loaded, single scores 2
    var scorekeeper = new InningScorekeeper();
    var state = new GameState(
        // ... setup walk-off scenario
    );
    var resolution = new PaResolution(
        // ... single that scores winning run
    );

    // Act
    var result = scorekeeper.ApplyPlateAppearance(state, resolution);

    // Assert
    int homeBattingRuns = scorekeeper.LineScore.HomeTotal;
    int awayPitchingRuns = scorekeeper.BoxScore.AwayPitchers[0].R;
    Assert.Equal(homeBattingRuns, awayPitchingRuns);
}

[Theory]
[InlineData(12345)]   // Known good seed
[InlineData(999999)]  // Walk-off seed
[InlineData(XXXXX)]   // Failing seed from 200-game run
public void FullGame_RunsIntegrity_BattingRunsMatchPitchingRuns(int seed)
{
    // Arrange
    var simulator = new GameSimulator("Home", "Away", seed);

    // Act
    string report = simulator.RunGame();

    // Assert - this will be caught by the assertion we added
    // But we can also explicitly check here
    Assert.True(report.Contains("Final:"));
}
```

**Additional Tests:**
1. Walk-off home run (all runs count)
2. Walk-off single (clamped runs)
3. Walk-off with bases loaded
4. Regular 9-inning game
5. Extra innings game

### Phase 5: Verification

1. **Run Full Test Suite**
   ```bash
   dotnet test
   ```

2. **Run 200 Games Again**
   ```powershell
   .\run_200_games.ps1
   ```
   - Should complete without assertion failures
   - Verify IP formatting shows "9.0" not "9."
   - Verify seeds list has "Game" prefix on all lines

3. **Spot Check Output**
   - Manually verify a few games have matching runs
   - Check walk-off games specifically

## Success Criteria

- [ ] All 200 games complete without assertion failures
- [ ] IP formatting shows "X.0" format consistently
- [ ] Seeds list has "Game" prefix on all lines
- [ ] New regression tests pass
- [ ] Existing test suite still passes
- [ ] Manual verification of walk-off games shows correct run totals

## Timeline Estimate

- Phase 2 (Reproduce): 30 minutes
- Phase 3 (Fix): 1-2 hours (depending on complexity)
- Phase 4 (Tests): 1 hour
- Phase 5 (Verification): 30 minutes

**Total:** 3-4 hours

## Deliverables

1. **Code Changes:**
   - Fix in `InningScorekeeper.cs` (or related file)
   - New test file: `tests/DiamondSim.Tests/Scoring/RunsIntegrityTests.cs`

2. **Documentation:**
   - Update this PRD with actual root cause once identified
   - Add comments in code explaining the fix

3. **Verification:**
   - 200-game run output showing no assertion failures
   - Test suite passing with new tests

## Notes

- The assertion we added will make debugging much easier by catching the exact moment the mismatch occurs
- The bug is likely in a specific edge case (walk-off, specific base/out situation)
- Once we have a failing seed, we can step through the exact sequence that causes the mismatch

## Related Issues

- IP formatting bug (FIXED ✅)
- Seeds list formatting bug (FIXED ✅)
- Runs mismatch bug (IN PROGRESS 🔄)
