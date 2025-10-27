# PRD: LOB at Instant of Third Out

**Status:** Draft
**Created:** 2025-10-26
**Epic:** Baseball Simulation Engine - Scoring & Recording
**Dependencies:** PRD-20251025-01 (Core Rules Corrections)

---

## 1. Overview

### 1.1 Purpose

This PRD defines the implementation plan for correctly computing Left On Base (LOB) statistics by capturing the base state at the instant the third out occurs, rather than using the post-play base state. This ensures accurate LOB tracking according to official baseball scoring rules while maintaining existing walk-off behavior.

### 1.2 Problem Statement

The current implementation computes LOB from the base state after a play completes, which is incorrect for plays that end the half-inning. According to official baseball rules, LOB must reflect the runners who were on base at the moment the third out was recorded, not the final base state after all advancement is resolved.

**Example of Current Bug:**
- Situation: R1/R2, 2 outs
- Play: Ball in play; R2 scores before the third out is recorded on a trailing runner
- Current behavior: LOB = 2 (incorrectly counts R2 who scored)
- Correct behavior: LOB = 1 (only R1 still on base at instant of third out; R2 scored before the out)

### 1.3 Scope

**In Scope:**
- Extend [`PaResolution`](src/DiamondSim/PaResolution.cs) with `BasesAtThirdOut` snapshot field
- Implement LOB computation from third-out snapshot in [`InningScorekeeper`](src/DiamondSim/InningScorekeeper.cs)
- Maintain walk-off exception (LOB = 0 always)
- Add backward compatibility fallback for missing snapshots
- Create comprehensive test coverage for third-out scenarios

**Out of Scope:**
- Runner-by-runner causal detail (which specific runner made the out)
- Pitcher responsibility or inherited runner tracking
- Refactoring all existing tests (opportunistic updates only)
- Changes to how bases/outs/runs are otherwise updated

---

## 2. Current State Analysis

### 2.1 Existing Components

**[`PaResolution`](src/DiamondSim/PaResolution.cs:33-39)** - Current structure (after Core Rules Corrections):
```csharp
public sealed record PaResolution(
    int OutsAdded,
    int RunsScored,
    BaseState NewBases,
    PaType Type,
    PaFlags? Flags = null,
    bool HadError = false,
    BaseState? AdvanceOnError = null
);
```

**[`InningScorekeeper`](src/DiamondSim/InningScorekeeper.cs)** - Handles half-inning transitions and LOB recording

**[`GameState`](src/DiamondSim/GameState.cs)** - Tracks current game situation including bases

**[`BaseState`](src/DiamondSim/Model.cs)** - Represents base occupancy (OnFirst, OnSecond, OnThird)

### 2.2 Identified Gaps

1. **No third-out snapshot:** [`PaResolution`](src/DiamondSim/PaResolution.cs) lacks a field to capture base state at the instant of the third out
2. **Incorrect LOB source:** LOB is computed from `NewBases` (post-play) instead of pre-out state
3. **No producer logic:** PA resolution builders don't detect or capture third-out scenarios
4. **Missing validation:** No tests verify LOB correctness for complex third-out plays

---

## 3. Requirements

### 3.1 Functional Requirements

#### FR-1: Third-Out Snapshot in PaResolution

**Requirement:** Extend [`PaResolution`](src/DiamondSim/PaResolution.cs) to include a snapshot of base occupancy at the instant the third out occurs.

**New Field:**
```csharp
public sealed record PaResolution(
    int OutsAdded,
    int RunsScored,
    BaseState NewBases,
    PaType Type,
    PaFlags? Flags = null,
    bool HadError = false,
    BaseState? AdvanceOnError = null,
    BaseState? BasesAtThirdOut = null          // NEW: Snapshot at instant of 3rd out
);
```

**Semantics:**
- `BasesAtThirdOut` captures base occupancy **at the instant the third out occurs**
- This may differ from the starting state if a runner scored before the third out or a runner/batter was already retired earlier in the play
- **Do not include retired or scored runners** - only runners still on base at the moment of the third out
- Only meaningful when `OutsAdded` causes total outs to reach 3 (ends the half)
- `null` when the PA does not end the half-inning
- **Read-only:** Must NOT be used to mutate game state bases

**Example:**
```csharp
// Before: R1/R2/R3, 0 outs
// Play: Triple play (all three runners out)
// Resolution:
new PaResolution(
    OutsAdded: 3,
    RunsScored: 0,
    NewBases: BaseState.Empty,           // Post-play: bases cleared
    Type: PaType.InPlayOut,
    BasesAtThirdOut: new BaseState {     // At third out: all occupied
        OnFirst = true,
        OnSecond = true,
        OnThird = true
    }
)
```

#### FR-2: LOB Computation Rules

**Requirement:** Compute LOB according to official baseball scoring rules with walk-off exception.

**Algorithm:**
```
IF (IsWalkoffSituation == true):
    LOB = 0  // Walk-off rule: game ends mid-play, no runners stranded
ELSE IF (endsHalf == true AND BasesAtThirdOut != null):
    LOB = Count of occupied bases in BasesAtThirdOut
    // Reason: Authoritative third-out state
ELSE IF (endsHalf == true AND BasesAtThirdOut == null):
    LOB = Count of occupied bases in NewBases  // Legacy fallback
    // Emit warning: "Missing BasesAtThirdOut; used post-play bases"
ELSE:
    // PA does not end half - no LOB computation
    LOB unchanged
```

**Validation Logic (Deterministic):**
1. **Walk-off check first:** If walk-off situation, short-circuit with LOB = 0
2. **Snapshot check:** If half ends and snapshot exists, use snapshot
3. **Fallback:** If half ends but no snapshot, use legacy method with warning
4. **No-op:** If half doesn't end, no LOB computation

#### FR-3: Walk-off Exception (Unchanged)

**Requirement:** Maintain existing walk-off behavior where LOB is always 0.

**Rationale:** In walk-off situations, the game ends the instant the winning run crosses home plate. No runners are "left on base" because the game is over mid-play.

**Applies to:**
- Walk-off home runs (all runs count, LOB = 0)
- Walk-off non-home runs (clamped runs, LOB = 0)

#### FR-4: Backward Compatibility

**Requirement:** Engine must tolerate missing `BasesAtThirdOut` snapshots without crashing.

**Behavior:**
- If `BasesAtThirdOut == null` on a half-ending PA, fall back to legacy behavior (use `NewBases`)
- Emit a warning log message: `"LOB computed from post-play bases; BasesAtThirdOut snapshot missing"`
- This allows incremental migration of PA resolution builders

**Migration Path:**
1. Phase 1: Add field to [`PaResolution`](src/DiamondSim/PaResolution.cs), update consumer logic with fallback
2. Phase 2: Update PA resolution builders to populate snapshot
3. Phase 3: Remove fallback once all producers are updated (future PRD)

### 3.2 Non-Functional Requirements

#### NFR-1: Determinism
- Given identical PA sequences, LOB values must be identical
- No randomness in LOB computation
- Snapshot capture must be deterministic

#### NFR-2: Performance
- Snapshot capture adds O(1) overhead (simple struct copy)
- LOB computation remains O(1)
- No impact on simulation throughput

#### NFR-3: Backward Compatibility
- Existing tests using [`PaResolution`](src/DiamondSim/PaResolution.cs) without `BasesAtThirdOut` continue to work
- Default value (`null`) maintains current behavior until producers are updated

---

## 4. Detailed Design

### 4.1 Data Model Changes

#### 4.1.1 PaResolution Extension

**File:** [`src/DiamondSim/PaResolution.cs`](src/DiamondSim/PaResolution.cs)

```csharp
/// <summary>
/// Represents the complete resolution of a plate appearance, including outs, runs, and base state changes.
/// </summary>
/// <param name="OutsAdded">The number of outs recorded on this plate appearance (0-3, includes triple plays).</param>
/// <param name="RunsScored">The number of runs scored on this plate appearance.</param>
/// <param name="NewBases">The resulting base state after the plate appearance.</param>
/// <param name="Type">The type of plate appearance outcome.</param>
/// <param name="Flags">Optional flags for special outcomes (double play, sacrifice fly, etc.).</param>
/// <param name="HadError">Whether the play involved a fielding error.</param>
/// <param name="AdvanceOnError">Which runners advanced specifically due to error (null if no error).</param>
/// <param name="BasesAtThirdOut">Snapshot of base occupancy at the instant the third out occurred (null if PA doesn't end half).</param>
public sealed record PaResolution(
    int OutsAdded,
    int RunsScored,
    BaseState NewBases,
    PaType Type,
    PaFlags? Flags = null,
    bool HadError = false,
    BaseState? AdvanceOnError = null,
    BaseState? BasesAtThirdOut = null
);
```

**Field Semantics:**
- `BasesAtThirdOut`: Captures base occupancy **at the instant the third out occurs**
- This may differ from the starting state if a runner scored before the third out or a runner/batter was already retired earlier in the play
- **Do not include retired or scored runners** - only runners still on base at the moment of the third out
- Only populated when `OutsAdded` causes total outs to reach 3
- Must NOT be used to mutate game state (read-only for LOB computation)
- `null` indicates either: (a) PA doesn't end half, or (b) producer hasn't been updated yet

### 4.2 Algorithm Specifications

#### 4.2.1 LOB Computation Algorithm

**Location:** [`InningScorekeeper.PerformHalfInningTransition()`](src/DiamondSim/InningScorekeeper.cs)

```csharp
private int ComputeLeftOnBase(
    GameState state,
    PaResolution resolution,
    bool isWalkoff)
{
    // Rule 1: Walk-off always results in LOB = 0
    if (isWalkoff) {
        return 0;
    }

    // Rule 2: Use third-out snapshot if available (authoritative)
    if (resolution.BasesAtThirdOut != null) {
        return CountOccupiedBases(resolution.BasesAtThirdOut);
    }

    // Rule 3: Fallback to legacy behavior (post-play bases)
    // This maintains backward compatibility until all producers are updated
    _logger?.LogWarning(
        "LOB computed from post-play bases; BasesAtThirdOut snapshot missing. " +
        "PA Type: {Type}, OutsAdded: {Outs}",
        resolution.Type,
        resolution.OutsAdded
    );

    return CountOccupiedBases(resolution.NewBases);
}

private int CountOccupiedBases(BaseState bases)
{
    int count = 0;
    if (bases.OnFirst) count++;
    if (bases.OnSecond) count++;
    if (bases.OnThird) count++;
    return count;
}
```

#### 4.2.2 Producer Logic (PA Resolution Builders)

**Location:** [`AtBatSimulator`](src/DiamondSim/AtBatSimulator.cs), [`BallInPlayResolver`](src/DiamondSim/BallInPlayResolver.cs)

**Detection Logic:**
```csharp
private BaseState? CaptureThirdOutSnapshot(
    GameState priorState,
    int outsAdded,
    BaseState basesAtThirdOut)  // Passed from play resolution logic
{
    int totalOuts = priorState.Outs + outsAdded;

    // Only capture snapshot if this PA ends the half
    if (totalOuts >= 3) {
        // Return the base state AT THE INSTANT OF THE THIRD OUT
        // This excludes runners who scored or were retired before the third out
        return basesAtThirdOut;
    }

    return null;  // PA doesn't end half
}
```

**Note:** The actual snapshot must be captured by the play resolution logic that tracks runner movement. This helper validates and packages it into the `PaResolution`.

**Integration Example:**
```csharp
// In BallInPlayResolver or similar
var resolution = new PaResolution(
    OutsAdded: outsRecorded,
    RunsScored: runsScored,
    NewBases: finalBases,
    Type: paType,
    Flags: flags,
    HadError: hadError,
    AdvanceOnError: errorAdvancement,
    BasesAtThirdOut: CaptureThirdOutSnapshot(priorState, outsRecorded)
);
```

### 4.3 State Transition Logic

**Half-Inning Transition Flow (Updated):**

```
1. Capture pre-PA GameState snapshot
2. Apply walk-off clamping (if applicable)
3. Calculate RBI (using clamped runs)
4. Classify earned/unearned (using clamped runs)
5. Apply state mutation:
   - Add runs to score
   - Add outs
   - Update bases (unless walk-off suppresses)
   - Advance batting order
6. Check for half/inning end:
   IF (outs >= 3):
       // NEW: Use BasesAtThirdOut for LOB if available
       LOB = ComputeLeftOnBase(state, resolution, isWalkoff)
       Record LOB to line score
       Flip sides
       Reset bases/outs
   IF (walkoff):
       Set IsFinal = true
       LOB = 0 (enforced by ComputeLeftOnBase)
7. Emit PA log event
```

**Critical:** LOB computation occurs AFTER all scoring logic but BEFORE state mutation for the next half.

---

## 5. Test Plan

### 5.1 Unit Test Cases

#### Test Suite: LOB Third-Out Snapshot Tests

**File:** `tests/DiamondSim.Tests/LobThirdOutTests.cs` (NEW)

| Test Case | Setup | Expected Result |
|-----------|-------|-----------------|
| `TriplePlay_BasesLoaded_LobEqualsThree` | R1/R2/R3, 0 outs, triple play | LOB=3, half ends, bases cleared |
| `DoublePlay_RunnerOnThird_LobEqualsOne` | R3 only, <2 outs, DP ends half | LOB=1 (R3 stranded) |
| `StrikeoutWithRunners_LobEqualsTwo` | R1/R2, 2 outs, strikeout | LOB=2 |
| `ForceOutAtSecond_RunnerAdvances_LobEqualsOne` | R3, 2 outs, ground ball force at 2nd | LOB=1 (R3 at moment of out, not R2 after) |
| `LineoutDoublePlay_RunnersStranded_LobEqualsTwo` | R1/R2, <2 outs, line out + DP | LOB=2 |
| `WalkoffNonHomeRun_LobAlwaysZero` | B9, tie, R3, single wins game | LOB=0 (walk-off exception) |
| `WalkoffHomeRun_LobAlwaysZero` | B9, down 2, bases loaded, HR | LOB=0 (walk-off exception) |
| `BackCompatFallback_NoSnapshot_UsesNewBases` | Half ends, `BasesAtThirdOut=null` | LOB from `NewBases`, warning logged |
| `MidInningOut_NoLobComputation` | R1/R2, 1 out, single out | LOB unchanged (half doesn't end) |

#### Test Suite: Integration with Walk-off Tests

**File:** `tests/DiamondSim.Tests/WalkoffTests.cs` (UPDATED)

Add assertions to existing walk-off tests:
- Verify `LOB = 0` in all walk-off scenarios
- Verify `BasesAtThirdOut` is ignored when walk-off applies
- Test both HR and non-HR walk-offs

### 5.2 Edge Case Scenarios

#### Scenario 1: Triple Play with Bases Loaded
```
Before: R1/R2/R3, 0 outs
Play: Line drive caught, runners doubled off 2nd and 3rd
Resolution:
  OutsAdded: 3
  RunsScored: 0
  NewBases: Empty
  BasesAtThirdOut: {OnFirst=true, OnSecond=true, OnThird=true}
Expected: LOB = 3
```

#### Scenario 2: Runner Scores Before Third Out
```
Before: R1/R2, 2 outs
Play: Ball in play; R2 scores, then third out recorded on trailing runner
Resolution:
  OutsAdded: 1
  RunsScored: 1
  NewBases: Empty  // Post-play state
  BasesAtThirdOut: {OnFirst=true}  // Only R1 still on base at instant of 3rd out
Expected: LOB = 1 (R2 scored before third out, so not stranded)
```

#### Scenario 3: Walk-off with Runners Stranded
```
Before: B9, tie, R1/R3, 2 outs
Play: Single scores R3, game ends
Resolution:
  OutsAdded: 0
  RunsScored: 1 (clamped)
  NewBases: {OnFirst=true, OnSecond=true}  // Batter and R1 advanced
  BasesAtThirdOut: null  // Not applicable (no third out)
Expected: LOB = 0 (walk-off exception, even though runners remain)
```

#### Scenario 4: Backward Compatibility Fallback
```
Before: R1/R2, 2 outs
Play: Strikeout (producer not updated yet)
Resolution:
  OutsAdded: 1
  RunsScored: 0
  NewBases: {OnFirst=true, OnSecond=true}
  BasesAtThirdOut: null  // Producer hasn't been updated
Expected: LOB = 2 (from NewBases), warning logged
```

### 5.3 Acceptance Criteria Validation

| Criterion | Validation Method |
|-----------|-------------------|
| LOB from third-out snapshot | `LobThirdOutTests` suite - verify snapshot used when available |
| Walk-off LOB always zero | `WalkoffTests` suite - verify LOB=0 in all walk-off scenarios |
| Backward compatibility | Test with `BasesAtThirdOut=null`, verify fallback + warning |
| Determinism | Run same PA sequence 100x, verify identical LOB values |
| No state mutation from snapshot | Verify `BasesAtThirdOut` never modifies game state bases |

---

## 6. Implementation Plan

### 6.1 Phase 1: Data Model Extension (30 minutes)

**Tasks:**
1. Add `BasesAtThirdOut` field to [`PaResolution`](src/DiamondSim/PaResolution.cs)
2. Update XML documentation
3. Verify existing tests still compile and pass

**Validation:** Code compiles, no test failures

### 6.2 Phase 2: Consumer Logic (1-2 hours)

**Tasks:**
1. Implement `ComputeLeftOnBase()` method in [`InningScorekeeper`](src/DiamondSim/InningScorekeeper.cs)
2. Integrate into `PerformHalfInningTransition()`
3. Add logging for fallback cases
4. Update walk-off logic to enforce LOB=0

**Validation:** Logic compiles, ready for testing

### 6.3 Phase 3: Test Suite Creation (2-3 hours)

**Tasks:**
1. Create `LobThirdOutTests.cs` with all test cases
2. Update `WalkoffTests.cs` with LOB assertions
3. Add backward compatibility tests
4. Write edge case scenario tests

**Validation:** All tests pass (using fallback initially)

### 6.4 Phase 4: Producer Updates (2-3 hours)

**Tasks:**
1. Implement `CaptureThirdOutSnapshot()` helper
2. Update [`AtBatSimulator`](src/DiamondSim/AtBatSimulator.cs) to populate snapshot
3. Update [`BallInPlayResolver`](src/DiamondSim/BallInPlayResolver.cs) to populate snapshot
4. Test with snapshot-based LOB computation

**Validation:** All tests pass with snapshot (no fallback warnings)

### 6.5 Phase 5: Integration & Documentation (1 hour)

**Tasks:**
1. Run full test suite
2. Verify determinism (100x same sequence)
3. Update code documentation
4. Code review

**Validation:** All tests pass, determinism verified, documentation complete

### 6.6 Total Estimated Effort

**6-9 hours** of development time

---

## 7. Dependencies & Risks

### 7.1 Dependencies

| Dependency | Status | Impact |
|------------|--------|--------|
| PRD-20251025-01 (Core Rules Corrections) | ✅ Complete | Provides extended [`PaResolution`](src/DiamondSim/PaResolution.cs) structure |
| [`InningScorekeeper`](src/DiamondSim/InningScorekeeper.cs) | ✅ Exists | Handles half-inning transitions |
| [`BaseState`](src/DiamondSim/Model.cs) | ✅ Exists | Represents base occupancy |

### 7.2 Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| **Producer logic complexity** | Medium | Medium | Start with simple detection, iterate |
| **Snapshot timing errors** | Low | High | Clear documentation, comprehensive tests |
| **Fallback masking bugs** | Medium | Low | Emit warnings, plan to remove fallback |
| **Walk-off edge cases** | Low | Medium | Explicit walk-off tests with LOB assertions |

### 7.3 Known Limitations

1. **Runner-by-runner detail:** Snapshot doesn't identify which specific runner made the out (sufficient for v1)
2. **Intermediate states:** Snapshot captures state at instant of third out only, not all mid-play states
3. **Fallback indefinite:** No timeline for removing backward compatibility fallback

---

## 8. Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Test coverage | >95% for new code | Code coverage report |
| Determinism | 100% identical LOB | 100-run validation |
| LOB accuracy | 100% correct for third-out plays | Test suite pass rate |
| Fallback usage | 0% after producer updates | Warning log count |

---

## 9. Future Enhancements

### 9.1 Potential Improvements
- Remove backward compatibility fallback once all producers updated
- Add runner-by-runner causal tracking (which runner made the out)
- Capture intermediate base states for complex plays
- Add LOB breakdown by inning to box score

### 9.2 Deferred to Other PRDs
- Pitcher-specific LOB tracking (requires pitching roster)
- Inherited runner LOB attribution (requires pitcher change tracking)
- Advanced base running statistics (stolen bases, caught stealing)

---

## 10. Appendix

### 10.1 Official Baseball Rules References

- **Rule 9.02(a)(3):** Left on base is the number of runners remaining on base after the third out
- **Rule 5.08(a):** Game ending procedures (walk-off situations)
- **Rule 5.06(b)(4)(A):** Home run is a dead ball - all runners must touch all bases

### 10.2 Related PRDs

- PRD-20251025-01: Core Rules Corrections (walk-off clamping, RBI, earned runs)
- PRD-20251024-04: Inning Scoring (line score tracking)
- PRD-20251024-03: Ball In Play Resolution (PA resolution structure)

### 10.3 Code References

- [`PaResolution.cs`](src/DiamondSim/PaResolution.cs:33-39) - Current structure
- [`InningScorekeeper.cs`](src/DiamondSim/InningScorekeeper.cs) - Half-inning transitions
- [`BaseState`](src/DiamondSim/Model.cs) - Base occupancy representation
- [`GameState.IsWalkoffSituation()`](src/DiamondSim/GameState.cs:309-314) - Walk-off detection

### 10.4 Test Examples

**Example Test: Triple Play LOB**
```csharp
[Fact]
public void TriplePlay_BasesLoaded_LobEqualsThree()
{
    // Arrange
    var state = new GameState {
        Inning = 5,
        Half = InningHalf.Top,
        Outs = 0,
        OnFirst = true,
        OnSecond = true,
        OnThird = true
    };

    var resolution = new PaResolution(
        OutsAdded: 3,
        RunsScored: 0,
        NewBases: BaseState.Empty,
        Type: PaType.InPlayOut,
        BasesAtThirdOut: new BaseState {
            OnFirst = true,
            OnSecond = true,
            OnThird = true
        }
    );

    // Act
    var result = scorekeeper.ApplyPlateAppearance(state, resolution);

    // Assert
    Assert.Equal(3, result.LineScore.GetInningLob(5, InningHalf.Top));
    Assert.Equal(InningHalf.Bottom, result.State.Half);
    Assert.Equal(0, result.State.Outs);
}
