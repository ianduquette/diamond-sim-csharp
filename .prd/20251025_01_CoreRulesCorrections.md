# PRD: Core Rules Corrections

**Status:** Draft
**Created:** 2025-10-26
**Epic:** Baseball Simulation Engine - Scoring & Recording
**Dependencies:** PRD-20251024-03 (Ball In Play Resolution)

---

## 1. Overview

### 1.1 Purpose

This PRD defines the implementation plan for correcting fundamental baseball scoring and recording rules in the DiamondSim engine. These corrections address critical gaps in RBI attribution, earned/unearned run classification, and walk-off game ending logic that must be resolved before implementing pitcher-specific tracking.

### 1.2 Problem Statement

The current implementation has three critical rule gaps:

1. **RBI on Reach-On-Error (ROE):** The system does not distinguish between runs scored on clean hits versus errors, incorrectly awarding RBI for ROE plays.
2. **Earned vs Unearned Runs:** No mechanism exists to classify runs as earned or unearned based on whether errors contributed to scoring.
3. **Walk-off Clamping:** Games ending in walk-off situations credit all runs that would score on the play, rather than only the minimum runs needed to win.

These gaps violate official baseball scoring rules and must be corrected before adding pitcher-specific earned run tracking.

### 1.3 Scope

**In Scope:**
- Extend [`PaResolution`](src/DiamondSim/PaResolution.cs) with error tracking fields
- Implement walk-off run clamping logic
- Add team-level earned/unearned run tracking
- Correct RBI attribution to exclude ROE
- Create `TestSnapshot` helper for safer test assertions
- Update [`GameState`](src/DiamondSim/GameState.cs) transition logic

**Out of Scope:**
- Pitcher-specific earned run attribution (deferred to pitching PRD)
- Inherited runner ownership tracking (deferred to pitching PRD)
- Full earned run reconstruction across multiple errors (v1-light approach only)
- Advanced defensive statistics (errors, fielding percentage)

---

## 2. Current State Analysis

### 2.1 Existing Components

**[`PaResolution`](src/DiamondSim/PaResolution.cs:33-39)** - Current structure:
```csharp
public sealed record PaResolution(
    int OutsAdded,
    int RunsScored,
    BaseState NewBases,
    PaType Type,
    PaFlags? Flags = null
);
```

**[`GameState`](src/DiamondSim/GameState.cs:6)** - Tracks game situation including:
- Inning, half, outs, bases
- Scores (away/home)
- Batting order positions
- Offense/defense designation
- `IsFinal` flag

**[`PaType`](src/DiamondSim/Outcomes.cs:116-161)** - Includes `ReachOnError` enum value

### 2.2 Identified Gaps

1. **No error attribution in `PaResolution`:** Cannot distinguish which runs scored due to errors
2. **No earned/unearned tracking:** [`GameState`](src/DiamondSim/GameState.cs) only tracks total runs
3. **Walk-off logic incomplete:** [`IsWalkoffSituation()`](src/DiamondSim/GameState.cs:309-314) exists but no clamping implementation
4. **Test equality hazard:** [`GameState.Equals()`](src/DiamondSim/GameState.cs:228-233) only compares balls/strikes, masking bugs

---

## 3. Requirements

### 3.1 Functional Requirements

#### FR-1: Error Tracking in PaResolution

**Requirement:** Extend [`PaResolution`](src/DiamondSim/PaResolution.cs) to include error information.

**New Fields:**
```csharp
public sealed record PaResolution(
    int OutsAdded,
    int RunsScored,
    BaseState NewBases,
    PaType Type,
    PaFlags? Flags = null,
    bool HadError = false,                    // NEW: Play involved fielding error
    BaseState? AdvanceOnError = null          // NEW: Which runners advanced due to error
);
```

**Rules:**
- `HadError = true` when `Type == PaType.ReachOnError` OR when any runner advancement was caused by error
- `AdvanceOnError` indicates which base runners (1st, 2nd, 3rd) advanced specifically due to the error
  - **Semantics:** Flags correspond to the **starting base** the runner occupied before the play
  - Example: Runner on 2nd scores due to error → `AdvanceOnError.OnSecond = true`
- `AdvanceOnError` is `null` when `HadError = false`

#### FR-2: Walk-off Run Clamping

**Requirement:** When a game-ending play occurs in the bottom of the 9th inning or later, apply special rules based on the play type.

**MLB Official Rules (Rule 5.06(b)(4)(A) & Rule 5.08(a)):**
- **Walk-off HOME RUN:** ALL runs count. The batter and all runners must touch all bases (dead ball rule).
- **Walk-off NON-HOME RUN:** Only the minimum runs needed to win are credited. Game ends when winning run crosses home plate.

**Algorithm:**
```
IF (Half == Bottom AND Inning >= 9 AND Offense == Home):
    offenseScore = GetOffenseScore()
    defenseScore = GetDefenseScore()

    IF (offenseScore <= defenseScore):  // Tied or trailing
        runsNeededToWin = (defenseScore - offenseScore) + 1

        // EXCEPTION: Home runs are dead balls - all runs count
        IF (Type == PaType.HomeRun):
            clampedRuns = runsScored  // No clamping for HR
            walkoffApplied = true
        ELSE:
            // Non-HR: Clamp to minimum needed
            clampedRuns = Min(runsScored, runsNeededToWin)
            walkoffApplied = (runsScored >= runsNeededToWin)

        // Apply clamped runs
        // Set IsFinal = true
        // LOB = 0 (ALWAYS enforced on walk-off, game ends mid-play)
        // Suppress further base state updates (except for HR - runners complete circuit)
```

**Order of Operations (Critical):**
1. **Clamp runs** (walk-off logic if applicable)
2. **Calculate RBI** (using clamped runs)
3. **Classify Earned/Unearned** (using clamped runs)
4. **Apply state mutation** (update scores, bases, outs, etc.)

**Edge Cases:**
- Home team already leading: No bottom half played, game ends after top of 9th
- Extra innings: Walk-off logic applies in any bottom half after 9th inning
- Walk-off grand slam: All 4 runs count (HR exception)
- Walk-off single with bases loaded: Only runs needed to win count (non-HR clamping)
- Multiple runners scoring on non-HR: Only count runs until lead is taken

#### FR-3: RBI Attribution Rules

**Requirement:** Correctly attribute RBI according to official baseball rules.

**Rules:**
1. **ROE = 0 RBI:** If `Type == ReachOnError`, credit **0 RBI** regardless of runs scored
2. **Bases-loaded walk/HBP = 1 RBI:** If bases loaded and `Type ∈ {BB, HBP}`, credit **1 RBI**
3. **Clean BIP:** For hits without errors, credit RBI equal to runs scored (capped by actual runs)
4. **Sacrifice fly:** If `Flags.IsSacFly == true`, credit **1 RBI** (even though batter is out)
   - **Edge case:** If the run required an error (throw/drop), the run is unearned but RBI still credited

**Implementation Note:** RBI calculation occurs AFTER walk-off clamping is applied, using the clamped run total.

#### FR-4: Earned vs Unearned Run Classification (Team-Level v1)

**Requirement:** Track earned and unearned runs at the team level.

**Classification Rules (v1-light):**

A run is **UNEARNED** if:
- The play type is `ReachOnError` (`Type == PaType.ReachOnError`), OR
- The scoring runner has `AdvanceOnError[base] == true` for their starting base

Otherwise, the run is **EARNED**.

**Team Tracking:**
```csharp
// In GameState or scoring component
public int AwayEarnedRuns { get; set; }
public int AwayUnearnedRuns { get; set; }
public int HomeEarnedRuns { get; set; }
public int HomeUnearnedRuns { get; set; }
```

**Deferred Complexity:**
- Full earned run reconstruction (hypothetical outs without errors) is deferred
- Pitcher-specific earned run tracking is deferred to pitching PRD
- Inherited runner ownership is deferred to pitching PRD

#### FR-5: Test Safety - TestSnapshot Helper

**Requirement:** Create a test helper to avoid false positives from [`GameState.Equals()`](src/DiamondSim/GameState.cs:228-233).

**Problem:** Current `Equals()` only compares balls/strikes, allowing tests to pass when game state is actually wrong.

**Solution:** Create `TestSnapshot` record for test assertions:

```csharp
public sealed record TestSnapshot(
    int Inning,
    InningHalf Half,
    int Outs,
    bool OnFirst,
    bool OnSecond,
    bool OnThird,
    int AwayScore,
    int HomeScore,
    Team Offense,
    Team Defense,
    bool IsFinal
);

// Extension method
public static TestSnapshot ToTestSnapshot(this GameState state) {
    return new TestSnapshot(
        state.Inning,
        state.Half,
        state.Outs,
        state.OnFirst,
        state.OnSecond,
        state.OnThird,
        state.AwayScore,
        state.HomeScore,
        state.Offense,
        state.Defense,
        state.IsFinal
    );
}
```

**Usage in Tests:**
```csharp
var expected = new TestSnapshot(
    Inning: 9,
    Half: InningHalf.Bottom,
    Outs: 0,
    OnFirst: false,
    OnSecond: false,
    OnThird: true,
    AwayScore: 3,
    HomeScore: 3,
    Offense: Team.Home,
    Defense: Team.Away,
    IsFinal: false
);

Assert.Equal(expected, gameState.ToTestSnapshot());
```

### 3.2 Non-Functional Requirements

#### NFR-1: Determinism
- Given identical PA sequences, all outputs (scores, RBI, earned/unearned, line score) must be identical
- No randomness in scoring logic (RNG decisions already made upstream)

#### NFR-2: Performance
- Walk-off clamping adds O(1) computation
- Error tracking adds minimal overhead (boolean flags)
- No impact on simulation throughput

#### NFR-3: Backward Compatibility
- Existing tests using [`PaResolution`](src/DiamondSim/PaResolution.cs) without error fields continue to work (default values)
- [`GameState`](src/DiamondSim/GameState.cs) API remains compatible

---

## 4. Detailed Design

### 4.1 Data Model Changes

#### 4.1.1 PaResolution Extensions

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

#### 4.1.2 GameState Extensions

**File:** [`src/DiamondSim/GameState.cs`](src/DiamondSim/GameState.cs)

Add earned/unearned run tracking:

```csharp
/// <summary>
/// Gets or sets the away team's earned runs.
/// </summary>
public int AwayEarnedRuns { get; set; }

/// <summary>
/// Gets or sets the away team's unearned runs.
/// </summary>
public int AwayUnearnedRuns { get; set; }

/// <summary>
/// Gets or sets the home team's earned runs.
/// </summary>
public int HomeEarnedRuns { get; set; }

/// <summary>
/// Gets or sets the home team's unearned runs.
/// </summary>
public int HomeUnearnedRuns { get; set; }
```

Update constructors to initialize these fields to 0.

#### 4.1.3 TestSnapshot Helper

**File:** `src/DiamondSim/TestSnapshot.cs` (NEW)

```csharp
namespace DiamondSim;

/// <summary>
/// Represents a snapshot of game state for testing purposes.
/// Includes all relevant game situation fields but excludes count (balls/strikes)
/// to enable accurate test assertions without false positives.
/// </summary>
public sealed record TestSnapshot(
    int Inning,
    InningHalf Half,
    int Outs,
    bool OnFirst,
    bool OnSecond,
    bool OnThird,
    int AwayScore,
    int HomeScore,
    Team Offense,
    Team Defense,
    bool IsFinal
);

/// <summary>
/// Extension methods for GameState testing.
/// </summary>
public static class GameStateTestExtensions {
    /// <summary>
    /// Creates a TestSnapshot from the current GameState.
    /// </summary>
    public static TestSnapshot ToTestSnapshot(this GameState state) {
        return new TestSnapshot(
            state.Inning,
            state.Half,
            state.Outs,
            state.OnFirst,
            state.OnSecond,
            state.OnThird,
            state.AwayScore,
            state.HomeScore,
            state.Offense,
            state.Defense,
            state.IsFinal
        );
    }
}
```

### 4.2 Algorithm Specifications

#### 4.2.1 Walk-off Clamping Algorithm

**Location:** Game state transition logic (likely in `InningScorekeeper` or similar)

```csharp
private (int clampedRuns, bool walkoffApplied) ApplyWalkoffClamping(
    GameState state,
    PaResolution resolution)
{
    // Check if walk-off situation is possible
    if (state.Half != InningHalf.Bottom || state.Inning < 9 || state.Offense != Team.Home) {
        return (resolution.RunsScored, false);
    }

    int homeScore = state.HomeScore;
    int awayScore = state.AwayScore;

    // Home team already winning - no clamping needed
    if (homeScore > awayScore) {
        return (resolution.RunsScored, false);
    }

    // Calculate runs needed to win
    int runsNeededToWin = (awayScore - homeScore) + 1;

    // CRITICAL: Home runs are dead balls - all runs count (MLB Rule 5.06(b)(4)(A))
    if (resolution.Type == PaType.HomeRun) {
        // Walk-off home run: credit all runs, game ends
        if (resolution.RunsScored >= runsNeededToWin) {
            return (resolution.RunsScored, true);  // All runs count for HR
        }
        return (resolution.RunsScored, false);  // Not enough to win yet
    }

    // Non-home run: Clamp to minimum needed (game ends when winning run scores)
    if (resolution.RunsScored >= runsNeededToWin) {
        return (runsNeededToWin, true);
    }

    // Not enough runs to win yet
    return (resolution.RunsScored, false);
}
```

#### 4.2.2 RBI Calculation Algorithm

```csharp
private int CalculateRbi(PaResolution resolution, GameState priorState)
{
    // Rule 1: ROE = 0 RBI
    if (resolution.Type == PaType.ReachOnError) {
        return 0;
    }

    // Rule 2: Bases-loaded walk/HBP = 1 RBI
    if ((resolution.Type == PaType.BB || resolution.Type == PaType.HBP) &&
        priorState.OnFirst && priorState.OnSecond && priorState.OnThird) {
        return 1;
    }

    // Rule 3: Sacrifice fly = 1 RBI
    if (resolution.Flags?.IsSacFly == true) {
        return 1;
    }

    // Rule 4: Clean BIP - credit runs scored (after walk-off clamping)
    // Note: This is called AFTER walk-off clamping has been applied
    return resolution.RunsScored;
}
```

#### 4.2.3 Earned/Unearned Classification Algorithm

```csharp
private (int earned, int unearned) ClassifyRuns(PaResolution resolution)
{
    // If no runs scored, nothing to classify
    if (resolution.RunsScored == 0) {
        return (0, 0);
    }

    // Rule 1: ROE = all runs unearned
    if (resolution.Type == PaType.ReachOnError) {
        return (0, resolution.RunsScored);
    }

    // Rule 2: Check for error-assisted advancement
    // In v1-light, if ANY runner advanced on error, mark ALL runs as unearned
    // (Full reconstruction deferred to later PRD)
    if (resolution.HadError && resolution.AdvanceOnError != null) {
        bool anyAdvanceOnError =
            resolution.AdvanceOnError.OnFirst ||
            resolution.AdvanceOnError.OnSecond ||
            resolution.AdvanceOnError.OnThird;

        if (anyAdvanceOnError) {
            return (0, resolution.RunsScored);
        }
    }

    // Rule 3: Clean play = all runs earned
    return (resolution.RunsScored, 0);
}
```

### 4.3 State Transition Logic

**Complete PA Resolution Flow (Order of Operations):**

```
1. Capture pre-PA GameState snapshot
2. CLAMP RUNS: Apply walk-off clamping to resolution.RunsScored → clampedRuns
3. CALCULATE RBI: Use clampedRuns to determine RBI attribution
4. CLASSIFY EARNED/UNEARNED: Use clampedRuns to split earned vs unearned
5. APPLY STATE MUTATION:
   - Add clampedRuns to batting team's score
   - Add earned/unearned to batting team's totals
   - Add outs
   - Update bases (unless walk-off applied)
   - Advance batting order
6. Check for half/inning end:
   - If walk-off applied: Set IsFinal=true, LOB=0 (ALWAYS), end immediately
   - If outs==3: Record LOB, flip sides, reset bases/outs
   - If tied after 9 full: Continue to extras
7. Emit PA log event with before/after snapshots
```

**Critical:** The order CLAMP → RBI → CLASSIFY → MUTATE must be strictly enforced to ensure correct statistics.

---

## 5. Test Plan

### 5.1 Unit Test Cases

#### Test Suite: RBI Attribution Tests

**File:** `tests/DiamondSim.Tests/RbiAttributionTests.cs` (NEW)

| Test Case | Setup | Expected Result |
|-----------|-------|-----------------|
| `RoeScoresRunner_CreditsZeroRbi` | R3, <2 outs, `ReachOnError` | Runs=1, RBI=0 |
| `BasesLoadedWalk_CreditsOneRbi` | Bases loaded, `BB` | Runs=1, RBI=1 |
| `BasesLoadedHbp_CreditsOneRbi` | Bases loaded, `HBP` | Runs=1, RBI=1 |
| `CleanSingle_CreditsRbi` | R3, `Single` (no error) | Runs=1, RBI=1 |
| `SacFly_CreditsOneRbi` | R3, <2 outs, `InPlayOut` + `IsSacFly=true` | Runs=1, RBI=1, Outs+1 |
| `HomeRun_CreditsAllRbi` | Bases loaded, `HomeRun` | Runs=4, RBI=4 |

#### Test Suite: Earned/Unearned Classification Tests

**File:** `tests/DiamondSim.Tests/EarnedRunTests.cs` (NEW)

| Test Case | Setup | Expected Result |
|-----------|-------|-----------------|
| `RoeScoresRunner_UnearnedRun` | R3, `ReachOnError` | ER=0, Unearned=1 |
| `CleanSingle_EarnedRun` | R3, `Single` (no error) | ER=1, Unearned=0 |
| `AdvanceOnError_UnearnedRun` | R2, `Single` + `AdvanceOnError[R2]=true` | ER=0, Unearned=1 |
| `ErrorButNoAdvance_EarnedRun` | R3, `Single` + `HadError=true` but `AdvanceOnError=null` | ER=1, Unearned=0 |

#### Test Suite: Walk-off Clamping Tests

**File:** `tests/DiamondSim.Tests/WalkoffTests.cs` (NEW)

| Test Case | Setup | Expected Result |
|-----------|-------|-----------------|
| `WalkoffSingle_TiedGame_ClampsToOne` | B9, tie, R3, `Single` (would score 1) | Runs=1, IsFinal=true, LOB=0 |
| `WalkoffHomeRun_TrailingByTwo_AllRunsCount` | B9, down 2, bases loaded, `HomeRun` | Runs=4, RBI=4, IsFinal=true, LOB=0 (HR exception) |
| `WalkoffGrandSlam_TiedGame_AllFourRuns` | B9, tie, bases loaded, `HomeRun` | Runs=4, RBI=4, IsFinal=true (HR exception) |
| `WalkoffSoloHomeRun_TiedGame_OneRun` | B9, tie, bases empty, `HomeRun` | Runs=1, RBI=1, IsFinal=true (HR exception) |
| `WalkoffDouble_TrailingByOne_ClampsToTwo` | B9, down 1, R2+R3, `Double` | Runs=2, IsFinal=true, LOB=0 |
| `WalkoffSingle_BasesLoaded_ClampsToOne` | B9, tie, bases loaded, `Single` | Runs=1, IsFinal=true, LOB=0 (non-HR clamping) |
| `TopNinth_NoWalkoff` | T9, tie, R3, `Single` | Runs=1, IsFinal=false |
| `BottomNinth_AlreadyLeading_NoBottomHalf` | After T9, home leads | No B9 played, IsFinal=true |
| `ExtraInnings_WalkoffStillApplies` | B10, tie, R3, `Single` | Runs=1, IsFinal=true |
| `ExtraInnings_WalkoffHomeRun_AllRuns` | B10, down 1, R1+R3, `HomeRun` | Runs=3, RBI=3, IsFinal=true (HR exception) |

#### Test Suite: TestSnapshot Safety Tests

**File:** `tests/DiamondSim.Tests/TestSnapshotTests.cs` (NEW)

| Test Case | Purpose |
|-----------|---------|
| `TestSnapshot_CapturesAllRelevantFields` | Verify all game situation fields are captured |
| `TestSnapshot_IgnoresBallsStrikes` | Verify count differences don't affect equality |
| `TestSnapshot_DetectsDifferences` | Verify actual state differences are detected |

### 5.2 Integration Test Cases

#### Test Suite: Full Game Scenarios

**File:** `tests/DiamondSim.Tests/GameScenarioTests.cs` (NEW)

| Scenario | Description | Validation |
|----------|-------------|------------|
| `WalkoffWin_CorrectScoring` | Full 9-inning game ending in walk-off | Final score, ER/Unearned totals, line score |
| `ExtraInningsGame_EarnedTracking` | Game with errors in extras | Correct ER/Unearned split across innings |
| `SkipBottomNinth_HomeLeading` | Home leads after T9 | No B9 played, IsFinal=true immediately |

### 5.3 Acceptance Criteria Validation

| Criterion | Validation Method |
|-----------|-------------------|
| Walk-off correctness | `WalkoffTests` suite - verify only necessary runs credited |
| RBI accuracy | `RbiAttributionTests` suite - verify ROE=0, bases-loaded=1 |
| Earned split | `EarnedRunTests` suite - verify error-based classification |
| Determinism | Run same PA sequence 100x, verify identical outputs |
| Test safety | All new tests use `TestSnapshot`, not `GameState.Equals()` |

---

## 6. Implementation Plan

### 6.1 Phase 1: Data Model Extensions (1-2 hours)

**Tasks:**
1. Extend [`PaResolution`](src/DiamondSim/PaResolution.cs) with `HadError` and `AdvanceOnError` fields
2. Add earned/unearned run fields to [`GameState`](src/DiamondSim/GameState.cs)
3. Create `TestSnapshot.cs` with helper extension method
4. Update [`GameState`](src/DiamondSim/GameState.cs) constructors to initialize new fields

**Validation:** Code compiles, existing tests pass

### 6.2 Phase 2: Walk-off Clamping Logic (2-3 hours)

**Tasks:**
1. Implement `ApplyWalkoffClamping()` method
2. Integrate into PA resolution flow
3. Add walk-off detection and game ending logic
4. Write `WalkoffTests` suite

**Validation:** All walk-off tests pass

### 6.3 Phase 3: RBI Attribution (1-2 hours)

**Tasks:**
1. Implement `CalculateRbi()` method
2. Integrate with stat tracking
3. Write `RbiAttributionTests` suite

**Validation:** All RBI tests pass

### 6.4 Phase 4: Earned/Unearned Classification (2-3 hours)

**Tasks:**
1. Implement `ClassifyRuns()` method
2. Update stat tracking to maintain ER/Unearned totals
3. Write `EarnedRunTests` suite

**Validation:** All earned run tests pass

### 6.5 Phase 5: Integration & Validation (2-3 hours)

**Tasks:**
1. Write full game scenario tests
2. Run determinism validation (100x same sequence)
3. Update existing tests to use `TestSnapshot`
4. Code review and documentation

**Validation:** All tests pass, determinism verified

### 6.6 Total Estimated Effort

**8-13 hours** of development time

---

## 7. Dependencies & Risks

### 7.1 Dependencies

| Dependency | Status | Impact |
|------------|--------|--------|
| PRD-20251024-03 (Ball In Play Resolution) | ✅ Complete | Provides [`PaResolution`](src/DiamondSim/PaResolution.cs) structure |
| [`GameState`](src/DiamondSim/GameState.cs) class | ✅ Exists | Foundation for state tracking |
| [`PaType`](src/DiamondSim/Outcomes.cs:116-161) enum | ✅ Exists | Includes `ReachOnError` |

### 7.2 Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| **Equality hazard in tests** | High | High | Mandate `TestSnapshot` in all new tests |
| **Walk-off edge cases** | Medium | Medium | Comprehensive test coverage of all scenarios |
| **v1-light ER classification insufficient** | Low | Low | Document limitations, plan for v2 reconstruction |
| **Breaking changes to [`PaResolution`](src/DiamondSim/PaResolution.cs)** | Low | Low | Use default parameters for backward compatibility |
| **Probabilities type name collision** | Medium | Low | Rename local DTO to `BipProbabilities` to avoid shadowing helper |

### 7.3 Known Limitations (Deferred)

1. **Full earned run reconstruction:** Hypothetical "what if no errors" analysis deferred
2. **Pitcher-specific ER tracking:** Requires pitching roster and substitution logic
3. **Inherited runner ownership:** Requires pitcher change tracking
4. **Advanced error scenarios:** Multiple errors in single play, complex advancement

---

## 8. Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Test coverage | >95% for new code | Code coverage report |
| Determinism | 100% identical outputs | 100-run validation |
| Walk-off accuracy | 100% correct clamping | Walk-off test suite |
| RBI accuracy | 100% correct attribution | RBI test suite |
| ER classification | 100% correct v1-light | Earned run test suite |

---

## 9. Future Enhancements

### 9.1 Deferred to Pitching PRD
- Pitcher-specific earned run tracking
- Inherited runner ownership
- Relief pitcher ER attribution
- Pitcher substitution mid-inning

### 9.2 Deferred to Advanced Scoring PRD
- Full earned run reconstruction (hypothetical outs)
- Multi-error play analysis
- Defensive statistics (errors, fielding %)
- Advanced base running (stolen bases, caught stealing)

---

## 10. Appendix

### 10.1 Official Baseball Rules References

- **Rule 9.16:** Earned runs and runs allowed
- **Rule 9.04:** RBI attribution
- **Rule 9.06(g):** Reach on error does not credit RBI
- **Rule 7.10:** Game ending procedures (walk-off)

### 10.2 Related PRDs

- PRD-20251024-01: Count Conditioned Contact
- PRD-20251024-02: At-Bat Loop
- PRD-20251024-03: Ball In Play Resolution
- PRD-20251024-04: Inning Scoring (current implementation)

### 10.3 Code References

- [`PaResolution.cs`](src/DiamondSim/PaResolution.cs:33-39) - Current structure
- [`GameState.cs`](src/DiamondSim/GameState.cs:6) - Game state tracking
- [`Outcomes.cs`](src/DiamondSim/Outcomes.cs:116-161) - Enum definitions
- [`GameState.Equals()`](src/DiamondSim/GameState.cs:228-233) - Problematic equality check
- [`GameState.IsWalkoffSituation()`](src/DiamondSim/GameState.cs:309-314) - Walk-off detection helper
