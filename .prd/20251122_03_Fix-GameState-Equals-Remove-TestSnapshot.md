# PRD: Fix GameState.Equals() and Remove TestSnapshot Workaround

**Date:** 2025-11-22
**Priority:** High
**Type:** Technical Debt / Bug Fix
**Estimated Effort:** 2-3 hours

## Problem Statement

The [`GameState.Equals()`](../../src/DiamondSim/GameState.cs:264-269) method only compares `Balls` and `Strikes`, ignoring all other game state fields (inning, outs, baserunners, scores, etc.). This design flaw led to the creation of a 218+ line workaround (`TestSnapshot` and `TestSnapshotTests`) instead of fixing the root cause.

### Current Broken Behavior
```csharp
public override bool Equals(object? obj) {
    if (obj is GameState other) {
        return Balls == other.Balls && Strikes == other.Strikes;
    }
    return false;
}
```

Two `GameState` objects with:
- Same count (2-1) but different innings (1st vs 9th)
- Same count but different scores (0-0 vs 5-4)
- Same count but different baserunners

...are considered **equal**, which is incorrect.

## Objectives

1. Fix `GameState.Equals()` to compare all relevant game state fields
2. Fix `GameState.GetHashCode()` to match the new equality logic
3. Add comprehensive equality tests
4. Remove the `TestSnapshot` workaround code
5. Update all tests that use `ToTestSnapshot()` to use direct `GameState` comparison
6. Verify all tests pass
7. Run full game simulation to ensure no regressions

## Implementation Plan

### Phase 1: Fix GameState.Equals() and GetHashCode()

**File:** `src/DiamondSim/GameState.cs`

1. Update `Equals()` method (lines 264-269):
```csharp
public override bool Equals(object? obj) {
    if (obj is GameState other) {
        return Balls == other.Balls
            && Strikes == other.Strikes
            && Inning == other.Inning
            && Half == other.Half
            && Outs == other.Outs
            && OnFirst == other.OnFirst
            && OnSecond == other.OnSecond
            && OnThird == other.OnThird
            && AwayScore == other.AwayScore
            && HomeScore == other.HomeScore
            && AwayBattingOrderIndex == other.AwayBattingOrderIndex
            && HomeBattingOrderIndex == other.HomeBattingOrderIndex
            && Offense == other.Offense
            && Defense == other.Defense
            && IsFinal == other.IsFinal;
    }
    return false;
}
```

**Note:** We exclude earned/unearned run fields from equality as they're derived statistics, not core game state.

2. Update `GetHashCode()` method (lines 275-277):
```csharp
public override int GetHashCode() {
    var hash = new HashCode();
    hash.Add(Balls);
    hash.Add(Strikes);
    hash.Add(Inning);
    hash.Add(Half);
    hash.Add(Outs);
    hash.Add(OnFirst);
    hash.Add(OnSecond);
    hash.Add(OnThird);
    hash.Add(AwayScore);
    hash.Add(HomeScore);
    hash.Add(AwayBattingOrderIndex);
    hash.Add(HomeBattingOrderIndex);
    hash.Add(Offense);
    hash.Add(Defense);
    hash.Add(IsFinal);
    return hash.ToHashCode();
}
```

### Phase 2: Add Comprehensive Equality Tests

**File:** `tests/DiamondSim.Tests/Model/GameStateTests.cs`

Add new test cases after line 252:

```csharp
[Test]
public void Equals_DifferentInning_ReturnsFalse() {
    // Arrange
    var state1 = new GameState(2, 1, 1, InningHalf.Top, 0, false, false, false, 0, 0, 0, 0, Team.Away, Team.Home);
    var state2 = new GameState(2, 1, 9, InningHalf.Top, 0, false, false, false, 0, 0, 0, 0, Team.Away, Team.Home);

    // Act & Assert
    Assert.That(state1, Is.Not.EqualTo(state2), "Different innings should not be equal");
}

[Test]
public void Equals_DifferentOuts_ReturnsFalse() {
    // Arrange
    var state1 = new GameState(2, 1, 5, InningHalf.Top, 0, false, false, false, 2, 3, 0, 0, Team.Away, Team.Home);
    var state2 = new GameState(2, 1, 5, InningHalf.Top, 2, false, false, false, 2, 3, 0, 0, Team.Away, Team.Home);

    // Act & Assert
    Assert.That(state1, Is.Not.EqualTo(state2), "Different outs should not be equal");
}

[Test]
public void Equals_DifferentBaserunners_ReturnsFalse() {
    // Arrange
    var state1 = new GameState(2, 1, 5, InningHalf.Top, 1, true, false, false, 2, 3, 0, 0, Team.Away, Team.Home);
    var state2 = new GameState(2, 1, 5, InningHalf.Top, 1, false, true, false, 2, 3, 0, 0, Team.Away, Team.Home);

    // Act & Assert
    Assert.That(state1, Is.Not.EqualTo(state2), "Different baserunners should not be equal");
}

[Test]
public void Equals_DifferentScores_ReturnsFalse() {
    // Arrange
    var state1 = new GameState(2, 1, 5, InningHalf.Top, 1, false, false, false, 2, 3, 0, 0, Team.Away, Team.Home);
    var state2 = new GameState(2, 1, 5, InningHalf.Top, 1, false, false, false, 5, 4, 0, 0, Team.Away, Team.Home);

    // Act & Assert
    Assert.That(state1, Is.Not.EqualTo(state2), "Different scores should not be equal");
}

[Test]
public void Equals_DifferentHalf_ReturnsFalse() {
    // Arrange
    var state1 = new GameState(2, 1, 5, InningHalf.Top, 1, false, false, false, 2, 3, 0, 0, Team.Away, Team.Home);
    var state2 = new GameState(2, 1, 5, InningHalf.Bottom, 1, false, false, false, 2, 3, 0, 0, Team.Home, Team.Away);

    // Act & Assert
    Assert.That(state1, Is.Not.EqualTo(state2), "Different inning halves should not be equal");
}

[Test]
public void Equals_DifferentOffenseDefense_ReturnsFalse() {
    // Arrange
    var state1 = new GameState(2, 1, 5, InningHalf.Top, 1, false, false, false, 2, 3, 0, 0, Team.Away, Team.Home);
    var state2 = new GameState(2, 1, 5, InningHalf.Top, 1, false, false, false, 2, 3, 0, 0, Team.Home, Team.Away);

    // Act & Assert
    Assert.That(state1, Is.Not.EqualTo(state2), "Different offense/defense should not be equal");
}

[Test]
public void Equals_DifferentIsFinal_ReturnsFalse() {
    // Arrange
    var state1 = new GameState(2, 1, 9, InningHalf.Bottom, 0, false, false, false, 5, 6, 0, 0, Team.Home, Team.Away, isFinal: false);
    var state2 = new GameState(2, 1, 9, InningHalf.Bottom, 0, false, false, false, 5, 6, 0, 0, Team.Home, Team.Away, isFinal: true);

    // Act & Assert
    Assert.That(state1, Is.Not.EqualTo(state2), "Different IsFinal should not be equal");
}

[Test]
public void Equals_DifferentBattingOrder_ReturnsFalse() {
    // Arrange
    var state1 = new GameState(2, 1, 5, InningHalf.Top, 1, false, false, false, 2, 3, 3, 5, Team.Away, Team.Home);
    var state2 = new GameState(2, 1, 5, InningHalf.Top, 1, false, false, false, 2, 3, 4, 6, Team.Away, Team.Home);

    // Act & Assert
    Assert.That(state1, Is.Not.EqualTo(state2), "Different batting order indices should not be equal");
}

[Test]
public void Equals_AllFieldsIdentical_ReturnsTrue() {
    // Arrange
    var state1 = new GameState(2, 1, 7, InningHalf.Bottom, 2, true, false, true, 4, 5, 3, 6, Team.Home, Team.Away, isFinal: false);
    var state2 = new GameState(2, 1, 7, InningHalf.Bottom, 2, true, false, true, 4, 5, 3, 6, Team.Home, Team.Away, isFinal: false);

    // Act & Assert
    Assert.That(state1, Is.EqualTo(state2), "Identical game states should be equal");
}

[Test]
public void GetHashCode_AllFieldsIdentical_ReturnsSameHash() {
    // Arrange
    var state1 = new GameState(2, 1, 7, InningHalf.Bottom, 2, true, false, true, 4, 5, 3, 6, Team.Home, Team.Away);
    var state2 = new GameState(2, 1, 7, InningHalf.Bottom, 2, true, false, true, 4, 5, 3, 6, Team.Home, Team.Away);

    // Act
    var hash1 = state1.GetHashCode();
    var hash2 = state2.GetHashCode();

    // Assert
    Assert.That(hash1, Is.EqualTo(hash2), "Identical game states should have same hash code");
}
```

### Phase 3: Remove TestSnapshot Workaround

**Files to Delete:**
1. `tests/DiamondSim.Tests/TestHelpers/TestSnapshot.cs`
2. `tests/DiamondSim.Tests/TestHelpers/TestSnapshotTests.cs`

### Phase 4: Update Tests Using ToTestSnapshot()

**CRITICAL GOTCHA DISCOVERED:** The tests don't compare full `GameState` objects - they extract snapshots and then assert on individual fields using `Assert.Multiple()`. This is actually a **good pattern** that should be preserved.

**Pattern Found (25 occurrences):**
```csharp
var snapshot = result.StateAfter.ToTestSnapshot();
Assert.Multiple(() => {
    Assert.That(snapshot.HomeScore, Is.EqualTo(4), "...");
    Assert.That(snapshot.AwayScore, Is.EqualTo(3), "...");
    Assert.That(snapshot.IsFinal, Is.True, "...");
    // ... more field assertions
});
```

**Correct Replacement Strategy:**
Instead of comparing snapshots, assert directly on `GameState` fields:
```csharp
var state = result.StateAfter;
Assert.Multiple(() => {
    Assert.That(state.HomeScore, Is.EqualTo(4), "...");
    Assert.That(state.AwayScore, Is.EqualTo(3), "...");
    Assert.That(state.IsFinal, Is.True, "...");
    // ... more field assertions
});
```

**Files to Update (25 occurrences total):**
- `tests/DiamondSim.Tests/Scoring/WalkoffTests.cs` - 21 occurrences
- `tests/DiamondSim.Tests/Scoring/InningScoreTests.cs` - 4 occurrences
- `tests/DiamondSim.Tests/TestHelpers/TestSnapshotTests.cs` - Will be deleted (5 occurrences)

**Why This Pattern is Better:**
1. More explicit - shows exactly which fields are being tested
2. Better error messages - NUnit reports which specific field failed
3. No intermediate object allocation
4. Clearer test intent

### Phase 5: Verification

1. **Run all unit tests:**
   ```bash
   dotnet test
   ```
   Expected: All tests pass

2. **Run game simulation:**
   ```bash
   dotnet run --project src/DiamondSim
   ```
   Expected: Game completes successfully with valid output

3. **Run 200-game simulation:**
   ```powershell
   .\run_200_games.ps1
   ```
   Expected: All games complete without errors

## Critical Analysis: Will Existing Tests Break?

### Existing GameStateTests.cs Analysis

**Lines 175-186: `Equals_SameValues_ReturnsTrue`**
```csharp
var state1 = new GameState(2, 1);  // Uses simple constructor
var state2 = new GameState(2, 1);
Assert.That(state1.Equals(state2), Is.True);
```
✅ **WILL STILL PASS** - Both use simple constructor which sets identical defaults for all fields (inning=1, half=Top, outs=0, etc.). New Equals() will compare all fields and they'll all match.

**Lines 189-200: `Equals_DifferentValues_ReturnsFalse`**
```csharp
var state1 = new GameState(2, 1);  // 2 balls, 1 strike
var state2 = new GameState(1, 2);  // 1 ball, 2 strikes
Assert.That(state1.Equals(state2), Is.False);
```
✅ **WILL STILL PASS** - Different balls/strikes means they're not equal. New Equals() checks balls/strikes first, so still returns false.

**Lines 227-238: `GetHashCode_SameValues_ReturnsSameHash`**
✅ **WILL STILL PASS** - Same states = same hash with new implementation.

**Lines 241-252: `GetHashCode_DifferentValues_ReturnsDifferentHash`**
✅ **WILL STILL PASS** - Different balls/strikes = different hash.

### Conclusion: TestSnapshot Was PURELY a Workaround

**YES - TestSnapshot was 100% a workaround for the broken Equals() method.**

The existing `GameStateTests.cs` tests will **all continue to pass** because:
1. They use the simple constructor which sets identical defaults for all non-count fields
2. The new Equals() still checks balls/strikes (just checks MORE fields too)
3. Tests comparing different counts will still fail equality (as intended)

**TestSnapshot served NO other purpose** - it was created solely because:
- Tests needed to compare full game state (inning, outs, scores, etc.)
- But `GameState.Equals()` only compared balls/strikes
- So they created a workaround record type that excluded balls/strikes

Once we fix `GameState.Equals()`, TestSnapshot becomes completely redundant.

## Success Criteria

- [ ] `GameState.Equals()` compares all relevant fields
- [ ] `GameState.GetHashCode()` matches equality logic
- [ ] 11 new comprehensive equality tests added and passing
- [ ] **Verify all 6 existing GameStateTests still pass** (they will)
- [ ] `TestSnapshot.cs` deleted
- [ ] `TestSnapshotTests.cs` deleted
- [ ] All 25 uses of `.ToTestSnapshot()` replaced with direct field access
- [ ] All unit tests pass (100% pass rate)
- [ ] Single game simulation runs successfully
- [ ] 200-game simulation completes without errors
- [ ] No regressions in game logic or scoring

## Risks & Mitigation

**Risk:** Existing tests may rely on the broken equality behavior
**Mitigation:** The existing `GameStateTests.cs` only tests count comparison, which will still work. Other tests use `ToTestSnapshot()` which we'll update.

**Risk:** Hash code changes could affect collections using `GameState` as keys
**Mitigation:** Codebase scan confirms NO usage of `GameState` in collections (Dictionary, HashSet, List). Safe to proceed.

**Risk:** Tests might be comparing full `GameState` objects expecting old behavior
**Mitigation:** Search confirms NO direct `GameState` equality comparisons in tests. All comparisons use `ToTestSnapshot()` pattern which we'll update.

## Gotchas Discovered

### 1. No Collections Using GameState
✅ **SAFE:** Searched for `Dictionary<GameState`, `HashSet<GameState`, `List<GameState>` - **zero results**. Hash code changes won't break anything.

### 2. Test Pattern is Field-by-Field Assertions
✅ **GOOD NEWS:** Tests don't compare full `GameState` objects. They use:
```csharp
var snapshot = state.ToTestSnapshot();
Assert.Multiple(() => {
    Assert.That(snapshot.Field1, Is.EqualTo(expected1));
    Assert.That(snapshot.Field2, Is.EqualTo(expected2));
});
```
This means we can simply replace `snapshot` with `state` and delete the snapshot line.

### 3. Only 25 Occurrences to Update
✅ **MANAGEABLE:** Only 25 uses of `.ToTestSnapshot()` across 3 files:
- 21 in `WalkoffTests.cs`
- 4 in `InningScoreTests.cs`
- 5 in `TestSnapshotTests.cs` (will be deleted)

### 4. No Direct GameState Equality in Tests
✅ **SAFE:** No tests use `Assert.That(state1, Is.EqualTo(state2))` pattern currently, so changing `Equals()` won't break existing tests.

## Notes

- This fix eliminates 218+ lines of workaround code
- Improves test clarity by allowing direct `GameState` comparison
- Aligns `Equals()` behavior with the actual semantic meaning of `GameState`
- The original design treated `GameState` as just a "count" object, but it evolved into a full game state container

## Related Files

- `src/DiamondSim/GameState.cs` - Core implementation
- `tests/DiamondSim.Tests/Model/GameStateTests.cs` - Equality tests
- `tests/DiamondSim.Tests/TestHelpers/TestSnapshot.cs` - To be deleted
- `tests/DiamondSim.Tests/TestHelpers/TestSnapshotTests.cs` - To be deleted
