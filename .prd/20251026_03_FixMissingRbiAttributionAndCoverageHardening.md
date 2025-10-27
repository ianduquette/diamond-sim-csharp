# PRD: RBI Attribution & Coverage Hardening

**Status:** Revision 1
**Created:** 2025-10-26
**Updated:** 2025-10-27
**Epic:** Baseball Simulation Engine - Scoring & Recording
**Dependencies:** PRD-20251025-01 (Core Rules Corrections), PRD-20251026-02 (LOB at Instant of Third Out)

---

## 1. Overview

### 1.1 Purpose

This PRD defines the implementation plan for fixing RBI (Runs Batted In) attribution in the DiamondSim engine and hardening test coverage around RBI and walk-off behavior. This is a surgical, technical change that ensures RBI is computed explicitly by the scorer per official baseball rules and never inferred from runs scored.

### 1.2 Problem Statement

The current implementation has a critical gap in RBI attribution:

1. **RBI Inference from Runs:** The box score may be inferring RBI from runs scored rather than computing it explicitly according to official baseball rules.
2. **Missing Test Coverage:** Insufficient test coverage around RBI edge cases (ROE, bases-loaded walks, sacrifice flies, walk-off clamping) and walk-off behavior.
3. **Silent Regression Risk:** Without comprehensive tests, future changes could break RBI logic without detection.

These gaps violate official baseball scoring rules and create maintenance risks.

### 1.3 Scope

**In Scope:**
- Implement explicit RBI calculation in [`InningScorekeeper`](src/DiamondSim/Scoring/InningScorekeeper.cs)
- Update [`BoxScore`](src/DiamondSim/Stats/BoxScore.cs) API to accept explicit RBI delta
- Remove any RBI inference logic from box score
- Create comprehensive RBI test suite (`RbiAttributionTests.cs`)
- Consolidate walk-off test coverage (`WalkoffTests.cs`)
- Optional: Add test categories and namespace alignment for better organization

**Out of Scope:**
- Earned/unearned run classification (handled by PRD-20251025-01)
- LOB computation (handled by PRD-20251026-02)
- Pitcher responsibility for inherited runners (deferred to post-v1)
- Inning transition logic (already implemented)

---

## 2. Current State Analysis

### 2.1 Existing Components

**[`InningScorekeeper`](src/DiamondSim/Scoring/InningScorekeeper.cs)** - Handles scoring logic:
- Computes runs scored from [`PaResolution`](src/DiamondSim/Models/PaResolution.cs)
- Applies walk-off clamping
- Updates box score statistics

**[`BoxScore`](src/DiamondSim/Stats/BoxScore.cs)** - Tracks player statistics:
- Current signature: `IncrementBatterStats(batterId, paType, runsScored, ...)`
- May be inferring RBI from runs or PA type
- Needs explicit RBI parameter

**[`PaResolution`](src/DiamondSim/Models/PaResolution.cs)** - Contains play details:
- Includes flags for special plays (sac fly, walk-off, etc.)
- Provides authoritative details needed for RBI calculation

### 2.2 Identified Gaps

1. **No explicit RBI calculation:** RBI may be inferred rather than computed per official rules
2. **Missing RBI tests:** No comprehensive test coverage for RBI edge cases
3. **Scattered walk-off tests:** Walk-off behavior tests may be duplicated across multiple files
4. **Test organization:** Lack of clear categorization makes regression detection harder

---

## 3. Requirements

### 3.1 Functional Requirements

#### FR-1: Explicit RBI Calculation

**Requirement:** Compute RBI explicitly in the scorer according to official baseball rules.

**Official Baseball Rules (Rule 9.04) - v1 Simplified:**

1. **ROE (Reach on Error):** 0 RBI to batter, regardless of runs scored
2. **Double Play Exception (optional v1.1):** If the batter grounds into a double play and a run scores, credit 0 RBI to the batter
3. **Bases-loaded BB/HBP:** Exactly 1 RBI to batter
4. **Sacrifice Fly:** Exactly 1 RBI to batter (even though batter is out)
5. **Home Run:** RBI equals all runs scoring on the play (all runners + batter)
6. **Non-HR Walk-off:** RBI equals minimum runs needed to win (clamped), not all potential runs
   - **Walk-off Clamp Formula:** `runsNeeded = (visitorRunsAfterTop - homeRunsBeforePlay) + 1`
7. **Ordinary Plays:** RBI equals runs scoring credited to batter's action, respecting any inning/game-end clamp

**Note:** This implementation intentionally omits bunts, interference/obstruction, and fielder's-choice nuances; those will be handled in a later PRD.

**Implementation Location:** [`InningScorekeeper`](src/DiamondSim/Scoring/InningScorekeeper.cs)

```csharp
private int CalculateRbi(PaResolution paResolution, int clampedRuns, BaseState baseOutStateBeforePlay)
{
    // Rule 1: ROE = 0 RBI
    if (paResolution.Type == PaType.ReachOnError) {
        return 0;
    }

    // Rule 2: Bases-loaded walk/HBP = 1 RBI
    if ((paResolution.Type == PaType.BB || paResolution.Type == PaType.HBP) &&
        baseOutStateBeforePlay.OnFirst && baseOutStateBeforePlay.OnSecond && baseOutStateBeforePlay.OnThird) {
        return 1;
    }

    // Rule 3: Sacrifice fly = 1 RBI
    if (paResolution.Flags?.IsSacFly == true) {
        return 1;
    }

    // Rule 4: Walk-off home run = all runs (already in clampedRuns)
    // Rule 5: Walk-off non-HR = clamped runs (already in clampedRuns)
    // Rule 6: Ordinary plays = runs credited to batter
    return clampedRuns;
}
```

**Order of Operations (Critical):**
1. Compute `clampedRuns` (walk-off logic if applicable)
2. Calculate `rbi` using `CalculateRbi(paResolution, clampedRuns, baseOutStateBeforePlay)`
3. Pass `rbi` to box score via updated API
4. Apply state mutation

#### FR-2: BoxScore API Update

**Requirement:** Update [`BoxScore`](src/DiamondSim/Stats/BoxScore.cs) to accept explicit RBI delta.

**Scope:** Only [`InningScorekeeper`](src/DiamondSim/Scoring/InningScorekeeper.cs) calls `IncrementBatterStats`. This is the only signature that needs to be changed.

**Current Signature:**
```csharp
IncrementBatterStats(batterId, paType, runsScored, ...)
```

**New Signature:**
```csharp
IncrementBatterStats(batterId, paType, runsScored, int rbiDelta, ...)
```

**Implementation Rules:**
- Box score internals: `line.RBI += rbiDelta;`
- Remove any RBI inference logic (e.g., inferring from `runsScored` or `paType`)
- If using `BatterLine`/`PitcherLine` models: No schema change needed, just ensure RBI is only mutated via `rbiDelta`

#### FR-3: Comprehensive RBI Test Coverage

**Requirement:** Create new test file with comprehensive RBI scenarios.

**File:** `tests/DiamondSim.Tests/Scoring/RbiAttributionTests.cs` (NEW)
**Namespace:** `DiamondSim.Tests.Scoring`

**Required Test Cases:**
1. `RBI_ROE_IsZero()` - ROE with runner on 3rd scores → 0 RBI
2. `RBI_Gidp_RunScores_CreditsZero()` - GIDP with run scoring → 0 RBI (optional v1.1)
3. `RBI_BasesLoadedWalk_IsOne()` - Bases-loaded walk → exactly 1 RBI
4. `RBI_BasesLoadedHbp_IsOne()` - Bases-loaded HBP → exactly 1 RBI
5. `RBI_SacFly_IsOne()` - Sac fly, R3 tags → exactly 1 RBI
6. `RBI_HomeRun_AllRunnersPlusBatter()` - HR with runners → RBI = all scoring
7. `RBI_WalkoffSingle_UsesClampedRuns()` - Walk-off single (tie, R3) → 1 RBI (clamped)
8. `RBI_WalkoffHomeRun_AllRunsCount()` - Walk-off HR (down 2, 3 on) → 4 RBI (HR exception)
9. `RBI_Double_TwoScore_CreditsTwo()` - Two-run double → exactly 2 RBI

**Test Implementation Guidelines:**
- Use existing [`TestSnapshot`](tests/DiamondSim.Tests/TestHelpers/TestSnapshot.cs) helper
- Keep test helpers file-local
- Assert both RBI and runs to verify independence

#### FR-4: Walk-off Test Consolidation

**Requirement:** Consolidate walk-off test coverage in dedicated file.

**File:** `tests/DiamondSim.Tests/Scoring/WalkoffTests.cs` (may already exist)
**Namespace:** `DiamondSim.Tests.Scoring`

**Coverage Requirements:**
1. Walk-off single, bases loaded, game tied → clamp 1 run; LOB=0
2. Walk-off double, down 1 with R2 → clamp 2 runs; LOB=0
3. Walk-off grand slam, tie game → 4 runs (HR exception); LOB=0
4. Solo walk-off HR, tie game → 1 run; LOB=0
5. Top 9th scoring does not end game (sanity check)

**Consolidation Rules:**
- Keep walk-off termination & clamp tests in `WalkoffTests.cs`
- Keep only one high-level sanity test in `InningScoreTests.cs` asserting correct termination; all clamp and LOB logic lives in `WalkoffTests.cs`
- Avoid split authority between test files

#### FR-5: Test Suite Hygiene (Optional but Recommended)

**Requirement:** Add test categories and align namespaces for better organization.

**Categories for CI Filtering:**
- `[Category("Scoring")]` - RBI, Walk-off, LineScore, InningScore, EarnedRun, BoxScore tests
- `[Category("Probabilities")]` - AtBat, Contact, BIP tests
- `[Category("Integration")]` - AtBatLoop tests

**Example Usage:**
```csharp
[Category("Scoring")]
public class RbiAttributionTests { /* ... */ }

[Category("Probabilities")]
public class BallInPlayTests { /* ... */ }

[Category("Integration")]
public class AtBatLoopTests { /* ... */ }
```

**Namespace Alignments:**
- `BallInPlayTests.cs` → `namespace DiamondSim.Tests.Probabilities`
- `AtBatLoopTests.cs` → `namespace DiamondSim.Tests.Integration`
- New scoring tests → `namespace DiamondSim.Tests.Scoring`

**Note:** No file moves required; flat tree structure is acceptable.

### 3.2 Non-Functional Requirements

#### NFR-1: Determinism
- Given identical inputs, RBI calculation must be deterministic
- No RNG in scoring/recording paths
- Same PA sequence must produce identical RBI totals

#### NFR-2: Performance
- RBI calculation adds O(1) computation
- No impact on simulation throughput
- Minimal overhead from explicit calculation

#### NFR-3: Backward Compatibility
- Signature change in `BoxScore.IncrementBatterStats` requires updating all call sites
- All call sites are in scorer only (limited blast radius)
- Existing tests remain green after changes

---

## 4. Detailed Design

### 4.1 Algorithm Specifications

#### 4.1.1 RBI Calculation Algorithm

**Location:** [`InningScorekeeper`](src/DiamondSim/Scoring/InningScorekeeper.cs)

```csharp
/// <summary>
/// Calculates RBI according to official baseball rules.
/// Must be called AFTER walk-off clamping is applied.
/// </summary>
/// <param name="paResolution">The plate appearance resolution</param>
/// <param name="clampedRuns">Runs after walk-off clamping (if applicable)</param>
/// <param name="baseOutStateBeforePlay">Base/out state before the play</param>
/// <returns>RBI to credit to the batter</returns>
private int CalculateRbi(
    PaResolution paResolution,
    int clampedRuns,
    BaseState baseOutStateBeforePlay)
{
    // Rule 1: Reach on error = 0 RBI (Rule 9.06(g))
    if (paResolution.Type == PaType.ReachOnError) {
        return 0;
    }

    // Rule 2 (optional v1.1): GIDP = 0 RBI
    // if (paResolution.Flags?.IsGroundedIntoDoublePlay == true) {
    //     return 0;
    // }

    // Rule 3: Bases-loaded walk or HBP = 1 RBI
    if ((paResolution.Type == PaType.BB || paResolution.Type == PaType.HBP) &&
        baseOutStateBeforePlay.OnFirst &&
        baseOutStateBeforePlay.OnSecond &&
        baseOutStateBeforePlay.OnThird) {
        return 1;
    }

    // Rule 4: Sacrifice fly = 1 RBI
    if (paResolution.Flags?.IsSacFly == true) {
        return 1;
    }

    // Rule 5: Home run = all runs scoring (batter + runners)
    // Rule 6: Walk-off non-HR = clamped runs (minimum needed to win)
    //         Formula: runsNeeded = (visitorRunsAfterTop - homeRunsBeforePlay) + 1
    // Rule 7: Ordinary plays = runs credited to batter's action
    // All these cases use clampedRuns (which already accounts for walk-off clamping)
    return clampedRuns;
}
```

**Key Design Decisions:**
- RBI calculation occurs AFTER walk-off clamping
- Uses `clampedRuns` not `paResolution.RunsScored` to respect walk-off rules
- Deterministic: no RNG, purely rule-based logic
- Does not duplicate run computation logic

#### 4.1.2 Integration with Scoring Flow

**Complete PA Resolution Flow:**

```
1. Capture pre-PA GameState snapshot (including base/out state)
2. CLAMP RUNS: Apply walk-off clamping → clampedRuns
3. CALCULATE RBI: rbi = CalculateRbi(paResolution, clampedRuns, baseOutStateBeforePlay)
4. CLASSIFY EARNED/UNEARNED: Split clampedRuns into earned vs unearned
5. UPDATE BOX SCORE:
   - BoxScore.IncrementBatterStats(batterId, paType, runsScored, rbi, ...)
   - Pass explicit rbi parameter
6. APPLY STATE MUTATION:
   - Add clampedRuns to batting team's score
   - Add outs, update bases, advance batting order
7. Check for half/inning end
8. Emit PA log event
```

**Critical:** The order CLAMP → RBI → BOX_SCORE must be strictly enforced.

### 4.2 API Changes

#### 4.2.1 BoxScore Method Signature

**File:** [`src/DiamondSim/Stats/BoxScore.cs`](src/DiamondSim/Stats/BoxScore.cs)

**Before:**
```csharp
public void IncrementBatterStats(
    int batterId,
    PaType paType,
    int runsScored,
    // ... other parameters
)
```

**After:**
```csharp
public void IncrementBatterStats(
    int batterId,
    PaType paType,
    int runsScored,
    int rbiDelta,  // NEW: Explicit RBI parameter
    // ... other parameters
)
```

**Implementation:**
```csharp
public void IncrementBatterStats(
    int batterId,
    PaType paType,
    int runsScored,
    int rbiDelta,
    // ... other parameters
)
{
    var line = GetOrCreateBatterLine(batterId);

    // ... existing stat updates ...

    // RBI: Use explicit delta, do NOT infer from runs or PA type
    line.RBI += rbiDelta;

    // ... remaining stat updates ...
}
```

**Migration Impact:**
- All call sites in [`InningScorekeeper`](src/DiamondSim/Scoring/InningScorekeeper.cs) must be updated
- Risk of double-counting eliminated by removing inference
- Single call site makes migration straightforward

---

## 5. Test Plan

### 5.1 Unit Test Cases

#### Test Suite: RBI Attribution Tests

**File:** `tests/DiamondSim.Tests/Scoring/RbiAttributionTests.cs` (NEW)
**Namespace:** `DiamondSim.Tests.Scoring`
**Category:** `[Category("Scoring")]`

| Test Case | Setup | Expected Result | Rule Validated |
|-----------|-------|-----------------|----------------|
| `RBI_ROE_IsZero` | R3, <2 outs, `ReachOnError` | Runs=1, RBI=0 | Rule 9.06(g) |
| `RBI_Gidp_RunScores_CreditsZero` | R3+R1, GIDP, R3 scores | Runs=1, RBI=0, Outs+2 | GIDP exception (optional v1.1) |
| `RBI_BasesLoadedWalk_IsOne` | Bases loaded, `BB` | Runs=1, RBI=1 | Rule 9.04(a)(2) |
| `RBI_BasesLoadedHbp_IsOne` | Bases loaded, `HBP` | Runs=1, RBI=1 | Rule 9.04(a)(2) |
| `RBI_SacFly_IsOne` | R3, <2 outs, `InPlayOut` + `IsSacFly=true` | Runs=1, RBI=1, Outs+1 | Rule 9.04(a)(3) |
| `RBI_HomeRun_AllRunnersPlusBatter` | Bases loaded, `HomeRun` | Runs=4, RBI=4 | Rule 9.04(a)(1) |
| `RBI_WalkoffSingle_UsesClampedRuns` | B9, tie, R3, `Single` | Runs=1, RBI=1 (clamped) | Walk-off clamping |
| `RBI_WalkoffHomeRun_AllRunsCount` | B9, down 2, bases loaded, `HomeRun` | Runs=4, RBI=4 (HR exception) | Walk-off HR exception |
| `RBI_Double_TwoScore_CreditsTwo` | R2+R3, `Double` (both score) | Runs=2, RBI=2 | Ordinary play |

**Test Implementation Pattern:**
```csharp
[Fact]
[Category("Scoring")]
public void RBI_ROE_IsZero()
{
    // Arrange: R3, <2 outs, reach on error
    var state = new GameState(/* ... */);
    state.OnThird = true;
    var resolution = new PaResolution(
        OutsAdded: 0,
        RunsScored: 1,
        NewBases: BaseState.Empty,
        Type: PaType.ReachOnError
    );

    // Act
    var result = scorer.ProcessPlateAppearance(state, resolution);

    // Assert
    Assert.Equal(1, result.RunsScored);  // Run counts
    Assert.Equal(0, result.RBI);         // But no RBI for ROE
}
```

#### Test Suite: Walk-off Consolidation Tests

**File:** `tests/DiamondSim.Tests/Scoring/WalkoffTests.cs` (may exist)
**Namespace:** `DiamondSim.Tests.Scoring`
**Category:** `[Category("Scoring")]`

| Test Case | Setup | Expected Result |
|-----------|-------|-----------------|
| `Walkoff_Single_BasesLoaded_ClampsToOne` | B9, tie, bases loaded, `Single` | Runs=1, RBI=1, IsFinal=true, LOB=0 |
| `Walkoff_Double_TrailingByOne_ClampsToTwo` | B9, down 1, R2+R3, `Double` | Runs=2, RBI=2, IsFinal=true, LOB=0 |
| `Walkoff_GrandSlam_TieGame_AllFourRuns` | B9, tie, bases loaded, `HomeRun` | Runs=4, RBI=4, IsFinal=true, LOB=0 |
| `Walkoff_SoloHomeRun_TieGame_OneRun` | B9, tie, bases empty, `HomeRun` | Runs=1, RBI=1, IsFinal=true, LOB=0 |
| `TopNinth_NoWalkoff` | T9, tie, R3, `Single` | Runs=1, RBI=1, IsFinal=false |

**Consolidation Actions:**
- Keep comprehensive walk-off tests in `WalkoffTests.cs`
- Keep only one high-level sanity test in `InningScoreTests.cs` asserting correct termination
- All clamp and LOB logic lives in `WalkoffTests.cs`

### 5.2 Test Organization

#### Optional: Test Categories

Add `[Category]` attributes to organize test execution:

```csharp
// Scoring tests
[Category("Scoring")]
public class RbiAttributionTests { /* ... */ }

[Category("Scoring")]
public class WalkoffTests { /* ... */ }

[Category("Scoring")]
public class BoxScoreTests { /* ... */ }

// Probability tests
[Category("Probabilities")]
public class BallInPlayTests { /* ... */ }

[Category("Probabilities")]
public class CountContactTests { /* ... */ }

// Integration tests
[Category("Integration")]
public class AtBatLoopTests { /* ... */ }
```

**Benefits:**
- Run test categories independently in CI
- Faster feedback for specific subsystems
- Clear test organization

### 5.3 Acceptance Criteria Validation

| Criterion | Validation Method |
|-----------|-------------------|
| Box score only updates RBI via explicit `rbiDelta` | Code review + unit tests |
| All new RBI tests pass | `RbiAttributionTests` suite green |
| Existing scoring/LOB/walk-off tests remain green | Full test suite regression |
| Walk-off tests assert clamp vs HR-exception and LOB=0 | `WalkoffTests` suite validation |
| Determinism preserved | No RNG added to scoring paths |
| One high-level walk-off sanity remains in `InningScoreTests` | Test consolidation review |

---

## 6. Implementation Plan

### 6.1 Phase 1: RBI Calculation Logic (1-2 hours)

**Tasks:**
1. Implement `CalculateRbi()` method in [`InningScorekeeper`](src/DiamondSim/Scoring/InningScorekeeper.cs)
2. Integrate into PA resolution flow (after walk-off clamping)
3. Ensure proper order: CLAMP → RBI → BOX_SCORE

**Validation:** Code compiles, logic is deterministic

### 6.2 Phase 2: BoxScore API Update (1 hour)

**Tasks:**
1. Update `IncrementBatterStats()` signature to accept `int rbiDelta`
2. Update method implementation: `line.RBI += rbiDelta;`
3. Remove any RBI inference logic
4. Update all call sites in scorer

**Validation:** Code compiles, existing tests pass

### 6.3 Phase 3: RBI Test Suite (2-3 hours)

**Tasks:**
1. Create `tests/DiamondSim.Tests/Scoring/RbiAttributionTests.cs`
2. Implement all 9 required test cases (8 core + 1 optional GIDP)
3. Use [`TestSnapshot`](tests/DiamondSim.Tests/TestHelpers/TestSnapshot.cs) for assertions
4. Add `[Category("Scoring")]` attributes

**Validation:** All RBI tests pass

### 6.4 Phase 4: Walk-off Test Consolidation (1-2 hours)

**Tasks:**
1. Review existing walk-off tests in `WalkoffTests.cs` and `InningScoreTests.cs`
2. Ensure comprehensive coverage in `WalkoffTests.cs`
3. Remove duplicate walk-off tests from `InningScoreTests.cs`
4. Keep one sanity check in `InningScoreTests.cs`
5. Add `[Category("Scoring")]` attributes

**Validation:** All walk-off tests pass, no duplicates

### 6.5 Phase 5: Test Organization (Optional, 1 hour)

**Tasks:**
1. Add `[Category]` attributes to all test classes
2. Align namespaces (no file moves required)
3. Update CI configuration to run categories independently

**Validation:** Tests can be run by category

### 6.6 Phase 6: Integration & Validation (1 hour)

**Tasks:**
1. Run full test suite
2. Verify determinism (same PA sequence → same RBI)
3. Code review
4. Update documentation

**Validation:** All tests pass, determinism verified

### 6.7 Total Estimated Effort

**6-9 hours** of development time

---

## 7. Dependencies & Risks

### 7.1 Dependencies

| Dependency | Status | Impact |
|------------|--------|--------|
| PRD-20251025-01 (Core Rules Corrections) | ✅ Complete | Provides walk-off clamping logic |
| PRD-20251026-02 (LOB at Instant of Third Out) | ✅ Complete | Provides LOB=0 enforcement for walk-offs |
| [`PaResolution`](src/DiamondSim/Models/PaResolution.cs) | ✅ Exists | Contains flags needed for RBI calculation |
| [`TestSnapshot`](tests/DiamondSim.Tests/TestHelpers/TestSnapshot.cs) | ✅ Exists | Test helper for safe assertions |

### 7.2 Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| **Signature change breaks call sites** | Low | Medium | Limited to scorer only, easy to update |
| **RBI inference still present** | Medium | High | Thorough code review, remove all inference |
| **Test coverage gaps** | Low | Medium | Comprehensive test plan with 8+ scenarios |
| **Walk-off test duplication** | Medium | Low | Explicit consolidation in Phase 4 |
| **Determinism violation** | Low | High | No RNG in scoring paths, verify in tests |

### 7.3 Known Limitations

1. **Pitcher-specific RBI tracking:** Deferred to pitching PRD (inherited runners)
2. **Advanced RBI edge cases:** Double plays (beyond optional GIDP), interference, obstruction, and sacrifices beyond fly balls are deferred to later versions
3. **Advanced RBI scenarios:** Complex multi-error plays deferred
4. **Historical RBI data:** No migration of existing game data

---

## 8. Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Test coverage | >95% for new code | Code coverage report |
| RBI accuracy | 100% correct attribution | RBI test suite (9 tests) |
| Walk-off accuracy | 100% correct clamping | Walk-off test suite (5 tests) |
| Determinism | 100% identical outputs | Repeated PA sequence validation |
| Regression | 0 broken existing tests | Full test suite green |

---

## 9. Future Enhancements

### 9.1 Deferred to Pitching PRD
- Pitcher-specific RBI tracking (who was pitching when RBI occurred)
- Inherited runner RBI attribution
- Relief pitcher statistics

### 9.2 Deferred to Advanced Scoring PRD
- Multi-error play RBI analysis
- Historical RBI data migration
- Advanced RBI scenarios (bunts, interference, obstruction, fielder's choice nuances)

---

## 10. Appendix

### 10.1 Official Baseball Rules References

- **Rule 9.04:** RBI attribution
- **Rule 9.04(a)(1):** Home run RBI
- **Rule 9.04(a)(2):** Bases-loaded walk/HBP RBI
- **Rule 9.04(a)(3):** Sacrifice fly RBI
- **Rule 9.06(g):** Reach on error does not credit RBI
- **Rule 5.08(a):** Game ending procedures (walk-off)

### 10.2 Related PRDs

- PRD-20251025-01: Core Rules Corrections (walk-off clamping)
- PRD-20251026-02: LOB at Instant of Third Out (LOB=0 for walk-offs)
- PRD-20251024-04: Inning Scoring (current implementation)

### 10.3 Code References

- [`InningScorekeeper.cs`](src/DiamondSim/Scoring/InningScorekeeper.cs) - Scoring logic
- [`BoxScore.cs`](src/DiamondSim/Stats/BoxScore.cs) - Player statistics tracking
- [`PaResolution.cs`](src/DiamondSim/Models/PaResolution.cs) - Play resolution details
- [`TestSnapshot.cs`](tests/DiamondSim.Tests/TestHelpers/TestSnapshot.cs) - Test helper

### 10.4 Test Scenarios Summary

**RBI Test Scenarios (9 tests):**
1. ROE with runner scoring → 0 RBI
2. GIDP with run scoring → 0 RBI (optional v1.1)
3. Bases-loaded walk → 1 RBI
4. Bases-loaded HBP → 1 RBI
5. Sacrifice fly → 1 RBI
6. Home run with runners → All runs = RBI
7. Walk-off single (tie, R3) → 1 RBI (clamped)
8. Walk-off HR (down 2, 3 on) → 4 RBI (HR exception)
9. Two-run double → 2 RBI

**Walk-off Test Scenarios (5 tests):**
1. Walk-off single, bases loaded, tie → 1 run, LOB=0
2. Walk-off double, down 1, R2+R3 → 2 runs, LOB=0
3. Walk-off grand slam, tie → 4 runs, LOB=0 (HR exception)
4. Solo walk-off HR, tie → 1 run, LOB=0
5. Top 9th scoring → No walk-off (sanity check)
