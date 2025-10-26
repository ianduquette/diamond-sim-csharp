# Product Requirements Document: Inning Scoring & Game State Management

**Document ID:** PRD-20251024-04
**Feature Name:** Inning Scoring & Game State Management
**Created:** 2025-10-24
**Status:** Draft
**Priority:** High
**Dependencies:** PRD-20251024-03 (Ball-In-Play Resolution)

---

## 1. Executive Summary

This PRD defines the requirements for applying resolved plate appearance (PA) results to the live game state, managing runs, outs, bases, half-inning transitions, walk-offs, and extra innings. This is purely deterministic bookkeeping with no randomness—all outcomes have already been determined by upstream components.

### Key Benefits
- **Complete Game Flow:** Transforms individual PA outcomes into full game progression
- **Deterministic State Management:** Predictable, testable game state transitions
- **Walk-off Detection:** Correctly handles game-ending scenarios in bottom 9th or extras
- **Line Score Tracking:** Maintains inning-by-inning run totals for both teams
- **Statistical Foundation:** Enables box score generation and player statistics
- **Extra Innings Support:** Handles tied games extending beyond 9 innings

---

## 2. Background & Context

### Current State
The DiamondSim project currently has:
- Count-conditioned contact probability (Part 1, completed)
- Complete at-bat loop producing terminal outcomes: K, BB, BIP (Part 2, completed)
- Ball-in-play resolution producing specific hit types and baserunner advancement (Part 3, completed)
- [`GameState`](src/DiamondSim/GameState.cs) class (currently only tracks balls/strikes count)
- [`BipOutcome`](src/DiamondSim/Outcomes.cs:56-81) enum with hit types
- [`BipResult`](src/DiamondSim/Outcomes.cs:87-89) record containing outcome information

### Problem Statement
The simulator can currently determine individual PA outcomes but cannot:
- Track game score, outs, and baserunners across multiple PAs
- Manage half-inning transitions when 3 outs are recorded
- Detect walk-off scenarios in bottom 9th or extra innings
- Maintain line scores (runs per inning) for both teams
- Handle extra innings when games are tied after 9
- Track batting order progression
- Generate box score statistics (hits, RBIs, runs, etc.)

### Baseball Context
In real baseball, game state management involves:
1. **Scoring:** Runs are credited immediately when runners cross home plate
2. **Outs:** Each half-inning continues until 3 outs are recorded
3. **Bases:** Runners occupy first, second, and/or third base
4. **Half-Inning Transitions:** After 3 outs, sides switch and bases clear
5. **Walk-offs:** Game ends immediately if home team takes lead in bottom 9th or later
6. **Extra Innings:** If tied after 9, play continues until one team leads after a completed inning
7. **Line Score:** Runs are recorded per inning for each team
8. **Batting Order:** Teams cycle through 9 batters in order

---

## 3. Goals & Objectives

### Primary Goals
1. Expand [`GameState`](src/DiamondSim/GameState.cs) to track full game context (score, outs, bases, inning, etc.)
2. Define `PaResolution` payload structure for PA outcomes
3. Implement deterministic state transition logic
4. Handle walk-off detection and game completion
5. Manage extra innings correctly
6. Track line scores per team
7. Implement basic box score statistics (v0.2 scope)

### Success Metrics
- All existing tests remain green (no regression)
- Walk-off scenarios end game immediately with correct final state
- Half-inning transitions reset outs and bases correctly
- Line scores sum to final team scores
- Extra innings continue until winner is determined
- All state transitions are deterministic and testable
- Box score statistics sum correctly (team hits = individual hits, etc.)

### Non-Goals (Out of Scope)
- Inherited runner tracking (deferred to later version)
- Earned vs. unearned run distinction (all runs counted as earned in v0.2)
- Pitcher substitutions and bullpen management
- Defensive substitutions
- Advanced statistics (OPS, ERA, etc.) - calculated externally
- Play-by-play narrative generation
- Replay or undo functionality

---

## 4. Functional Requirements

### FR-1: Enhanced GameState Structure
**Priority:** P0 (Critical)

**Description:** Expand [`GameState`](src/DiamondSim/GameState.cs) to track complete game context.

**Requirements:**
- FR-1.1: Add game situation fields:
  - `Inning: int` (1-based, starts at 1)
  - `Half: InningHalf` enum (Top, Bottom)
  - `Outs: int` (0-2, resets to 0 on half-inning change)
  - `IsFinal: bool` (true when game is complete)
- FR-1.2: Add base state:
  - `OnFirst: bool`
  - `OnSecond: bool`
  - `OnThird: bool`
- FR-1.3: Add score tracking:
  - `AwayScore: int`
  - `HomeScore: int`
- FR-1.4: Add batting order tracking:
  - `AwayBattingOrderIndex: int` (0-8, cycles through lineup)
  - `HomeBattingOrderIndex: int` (0-8, cycles through lineup)
- FR-1.5: Add team designation:
  - `Offense: Team` enum (Away, Home)
  - `Defense: Team` enum (Away, Home)
- FR-1.6: Maintain backward compatibility with existing count tracking (Balls, Strikes)
- FR-1.7: Add helper methods:
  - `GetOffenseScore()` - returns current batting team's score
  - `GetDefenseScore()` - returns current fielding team's score
  - `GetBattingOrderIndex()` - returns current batting team's lineup position
  - `IsWalkoffSituation()` - checks if walk-off is possible

**Acceptance Criteria:**
- ✅ All new fields are properly initialized
- ✅ Existing tests continue to pass
- ✅ Helper methods return correct values
- ✅ XML documentation is complete

---

### FR-2: PaResolution Payload Structure
**Priority:** P0 (Critical)

**Description:** Define the data structure that carries PA outcome information from resolution to state update.

**Requirements:**
- FR-2.1: Create `PaResolution` record with fields:
  - `OutsAdded: int` (0, 1, or 2)
  - `RunsScored: int` (includes batter on HR)
  - `NewBases: BaseState` record (OnFirst, OnSecond, OnThird)
  - `Type: PaType` enum (K, BB, HBP, InPlayOut, Single, Double, Triple, HomeRun, ReachOnError)
  - `Flags: PaFlags` record (IsDoublePlay, IsSacFly) - optional for v0.2
- FR-2.2: Create `BaseState` record:
  - `OnFirst: bool`
  - `OnSecond: bool`
  - `OnThird: bool`
- FR-2.3: Create `PaType` enum with all outcome types
- FR-2.4: Create `PaFlags` record for special situations (optional in v0.2)
- FR-2.5: Add XML documentation for all types

**Acceptance Criteria:**
- ✅ All payload types are properly defined
- ✅ Records are immutable (using `record` keyword)
- ✅ Enums cover all possible PA outcomes
- ✅ Documentation clearly explains each field

---

### FR-3: State Transition Logic
**Priority:** P0 (Critical)

**Description:** Implement deterministic logic for applying PA results to game state.

**Requirements:**
- FR-3.1: Create `InningScorekeeper` class (or similar) with method:
  - `ApplyPlateAppearance(GameState state, PaResolution resolution): GameState`
- FR-3.2: Implement transition rules in order:
  1. **Apply runs:** Add `resolution.RunsScored` to batting team's score
  2. **Apply outs:** Add `resolution.OutsAdded` to current outs
  3. **Apply bases:** Replace base state with `resolution.NewBases`
  4. **Advance lineup:** Increment batting team's order index, wrap at 9
  5. **Check walk-off:** If bottom half, inning ≥ 9, and home leads, set `IsFinal = true`
  6. **Check half close:** If outs == 3, perform half-inning transition
- FR-3.3: Half-inning transition logic:
  - Record LOB (left on base) = count of occupied bases at moment of 3rd out
  - Flush runs scored this half to line score
  - Reset outs to 0
  - Clear all bases (OnFirst = OnSecond = OnThird = false)
  - If Top → Bottom: switch offense/defense
  - If Bottom → Top: switch offense/defense AND increment inning
- FR-3.4a: Walk-off early termination (home team takes lead):
  - If walk-off detected, set `IsFinal = true` immediately
  - Do NOT process further PAs in that half-inning
  - Line score for partial inning includes only runs scored before walk-off
- FR-3.4b: Skip bottom 9th if home already leads:
  - If `Inning == 9` and the Top half has just ended (3rd out recorded) and `HomeScore > AwayScore`, set `IsFinal = true`
  - Do NOT start the bottom 9th half
  - Record 'X' for the home team's 9th inning in the line score
  - This is the standard baseball rule: home team doesn't bat in bottom 9th if already leading
  - If home is losing or tied after top 9th, bottom 9th **MUST** be played
- FR-3.5: Extra innings logic:
  - If tied after 9 complete innings, continue alternating halves
  - Walk-off rule still applies in bottom halves of extras
  - Game ends when one team leads after a completed inning
- FR-3.6: Loop safety (infinite extras guard):
  - Game loop **MUST** abort if `Inning > 99`
  - This is a safety mechanism to prevent infinite loops in tests or simulations
  - In practice, games should never reach this limit
  - When limit is reached, throw an exception or set error state (test-only failure)
- FR-3.7: No RNG calls (determinism requirement):
  - **CRITICAL:** No randomness is allowed in inning scoring logic
  - All random outcomes **MUST** occur upstream in PRD-02 (At-Bat Loop) and PRD-03 (Ball-In-Play Resolution)
  - Inning scoring is purely deterministic bookkeeping
  - Any RNG call in this module is a violation and **MUST** fail CI
  - Add lint/test that fails if `IRandomSource` or any RNG method is invoked in inning scoring code

**Acceptance Criteria:**
- ✅ All transition rules execute in correct order
- ✅ Walk-off detection works correctly
- ✅ Half-inning transitions reset state properly
- ✅ Extra innings continue until winner determined
- ✅ State transitions are deterministic

---

### FR-4: Line Score Management
**Priority:** P0 (Critical)

**Description:** Track runs scored per inning for both teams.

**Requirements:**
- FR-4.1: Create `LineScore` class with:
  - `AwayInnings: List<int>` (runs per inning for away team)
  - `HomeInnings: List<int>` (runs per inning for home team)
  - `AwayTotal: int` (computed property, sum of AwayInnings)
  - `HomeTotal: int` (computed property, sum of HomeInnings)
- FR-4.2: Track runs during each half-inning:
  - Maintain running total of runs scored in current half
  - On half close, append total to appropriate team's inning list
  - Reset running total to 0 for next half
- FR-4.3: Handle partial innings (walk-offs):
  - If game ends mid-inning due to walk-off, record actual runs scored in that partial inning
  - Losing team's incomplete bottom half is not recorded (no entry)
- FR-4.4: Line score symbols:
  - Use **'X'** for home team's 9th inning when bottom 9th is not played because home was already leading after top 9th
  - For walk-offs (bottom 9th or later), record the **actual runs** scored in that partial half-inning (not 'X')
  - Example: Home leads 4-3 after top 9th → Home 9th shows 'X'
  - Example: Home wins 4-3 with walk-off HR in bottom 9th → Home 9th shows actual runs (e.g., '1')
  - Do NOT use '-' symbol; standardize on 'X' only
- FR-4.5: Validation:
  - `AwayTotal` must equal `GameState.AwayScore`
  - `HomeTotal` must equal `GameState.HomeScore`
  - Sum of all innings must equal final score

**Acceptance Criteria:**
- ✅ Line score accurately reflects runs per inning
- ✅ Totals match game state scores
- ✅ Walk-off partial innings show actual runs (not 'X')
- ✅ Skipped bottom 9th shows 'X' symbol
- ✅ Extra innings extend line score appropriately

---

### FR-5: Box Score Statistics (v0.2 Scope)
**Priority:** P1 (High)

**Description:** Track basic player statistics during game simulation.

**Requirements:**
- FR-5.1: Define "stat tap" interface for incrementing tallies per PA
- FR-5.2: Batter statistics to track:
  - `AB` (at-bats) - excludes BB, HBP
  - Note: SF not modeled in v0.2; when SF is added later, reintroduce the AB exception
  - `H` (hits) - singles, doubles, triples, home runs
  - `1B, 2B, 3B, HR` (hit type breakdown)
  - `BB` (walks)
  - `HBP` (hit by pitch)
  - `K` (strikeouts)
  - `RBI` (runs batted in)
  - `R` (runs scored)
  - `PA` (plate appearances) - all PAs
  - `TB` (total bases) - 1B×1 + 2B×2 + 3B×3 + HR×4
- FR-5.3: Pitcher statistics to track:
  - `BF` (batters faced)
  - `OutsRecorded` (converts to IP: outs ÷ 3)
  - `H` (hits allowed)
  - `R` (runs allowed)
  - `ER` (earned runs) - all runs counted as earned in v0.2
  - `BB` (walks allowed)
  - `HBP` (hit batters)
  - `K` (strikeouts)
  - `HR` (home runs allowed)
- FR-5.4: Do NOT compute derived rates (AVG, OPS, ERA, etc.) in engine
  - Leave rate calculations to external analysis
- FR-5.5: Validation checks:
  - Team hits = sum of individual batter hits
  - Defensive outs: 27 per team in a full 9-inning game; 24 for away defense if home skips bottom 9th; extras add 3 per completed defensive half
  - Pitcher outs sum across defense = total outs made
  - Tests must mirror the v0.2 RBI-on-ROE simplification; official scorer logic may change later

**Acceptance Criteria:**
- ✅ All basic statistics are tracked correctly
- ✅ Stat tallies increment on each PA
- ✅ Validation checks pass
- ✅ No rate calculations in engine

---

### FR-6: Left on Base (LOB) Tracking
**Priority:** P1 (High)

**Description:** Track runners left on base at the end of each half-inning.

**Requirements:**
- FR-6.1: On 3rd out, count occupied bases:
  - LOB = (OnFirst ? 1 : 0) + (OnSecond ? 1 : 0) + (OnThird ? 1 : 0)
- FR-6.2: Record LOB per half-inning in line score or separate structure
- FR-6.3: Team LOB = sum of all half-inning LOB values
- FR-6.4: Walk-off scenarios:
  - If game ends mid-inning, LOB for that partial inning = 0
  - Losing team's incomplete bottom half has no LOB

**Acceptance Criteria:**
- ✅ LOB counted correctly at moment of 3rd out
- ✅ Walk-off scenarios handle LOB appropriately
- ✅ Team LOB totals are accurate

---

## 5. Technical Design

### 5.1 Architecture Overview

```
┌─────────────────────────────────────┐
│   BallInPlayResolver (Part 3)       │
│                                     │
│  Returns: BipOutcome                │
└─────────────────────────────────────┘
                │
                │ Outcome + Context
                ▼
┌─────────────────────────────────────┐
│   PaResolution Builder              │
│                                     │
│  Constructs PaResolution:           │
│  - OutsAdded                        │
│  - RunsScored                       │
│  - NewBases                         │
│  - Type                             │
└─────────────────────────────────────┘
                │
                │ PaResolution
                ▼
┌─────────────────────────────────────┐
│   InningScorekeeper                 │
│                                     │
│  ApplyPlateAppearance(              │
│    state: GameState,                │
│    resolution: PaResolution         │
│  ): GameState                       │
│                                     │
│  1. Apply runs                      │
│  2. Apply outs                      │
│  3. Apply bases                     │
│  4. Advance lineup                  │
│  5. Check walk-off                  │
│  6. Check half close                │
└─────────────────────────────────────┘
                │
                │ Updated GameState
                ▼
┌─────────────────────────────────────┐
│   LineScore & BoxScore              │
│                                     │
│  - Track runs per inning            │
│  - Track player statistics          │
│  - Validate totals                  │
└─────────────────────────────────────┘
```

### 5.2 File Changes

#### New Files
1. **[`src/DiamondSim/InningScorekeeper.cs`](src/DiamondSim/InningScorekeeper.cs)**
   - Purpose: Apply PA results to game state
   - Key method: `ApplyPlateAppearance()`
   - Dependencies: [`GameState`](src/DiamondSim/GameState.cs), `PaResolution`

2. **[`src/DiamondSim/PaResolution.cs`](src/DiamondSim/PaResolution.cs)**
   - Purpose: Define PA outcome payload structures
   - Types: `PaResolution`, `BaseState`, `PaType`, `PaFlags`

3. **[`src/DiamondSim/LineScore.cs`](src/DiamondSim/LineScore.cs)**
   - Purpose: Track runs per inning
   - Key properties: `AwayInnings`, `HomeInnings`, totals

4. **[`src/DiamondSim/BoxScore.cs`](src/DiamondSim/BoxScore.cs)**
   - Purpose: Track player statistics
   - Types: `BatterStats`, `PitcherStats`

5. **[`tests/DiamondSim.Tests/InningScoreTests.cs`](tests/DiamondSim.Tests/InningScoreTests.cs)**
   - Purpose: Validate state transitions and scoring logic
   - Key tests: Walk-offs, half transitions, extras, LOB

#### Modified Files
1. **[`src/DiamondSim/GameState.cs`](src/DiamondSim/GameState.cs)**
   - Add inning, half, outs, bases, score, lineup tracking
   - Maintain backward compatibility with existing count tracking
   - Add helper methods

2. **[`src/DiamondSim/Outcomes.cs`](src/DiamondSim/Outcomes.cs)**
   - Add `InningHalf` enum (Top, Bottom)
   - Add `Team` enum (Away, Home)

### 5.3 State Transition Pseudocode

```csharp
public class InningScorekeeper {
    public GameState ApplyPlateAppearance(
        GameState state,
        PaResolution resolution
    ) {
        // Create mutable copy for updates
        var newState = state.Clone();

        // 1. Apply runs to batting team
        if (newState.Offense == Team.Away) {
            newState.AwayScore += resolution.RunsScored;
        } else {
            newState.HomeScore += resolution.RunsScored;
        }

        // 2. Apply outs
        newState.Outs += resolution.OutsAdded;

        // 3. Apply bases
        newState.OnFirst = resolution.NewBases.OnFirst;
        newState.OnSecond = resolution.NewBases.OnSecond;
        newState.OnThird = resolution.NewBases.OnThird;

        // 4. Advance lineup
        if (newState.Offense == Team.Away) {
            newState.AwayBattingOrderIndex =
                (newState.AwayBattingOrderIndex + 1) % 9;
        } else {
            newState.HomeBattingOrderIndex =
                (newState.HomeBattingOrderIndex + 1) % 9;
        }

        // 5. Check walk-off
        if (newState.Half == InningHalf.Bottom &&
            newState.Inning >= 9 &&
            newState.HomeScore > newState.AwayScore) {
            newState.IsFinal = true;
            return newState; // Game over
        }

        // 6. Check half close (3 outs)
        if (newState.Outs >= 3) {
            // Record LOB
            int lob = (newState.OnFirst ? 1 : 0) +
                      (newState.OnSecond ? 1 : 0) +
                      (newState.OnThird ? 1 : 0);

            // Flush line score (implementation detail)
            FlushLineScore(newState);

            // Check if home is leading after top 9th
            if (newState.Inning == 9 &&
                newState.Half == InningHalf.Top &&
                newState.HomeScore > newState.AwayScore) {
                // Game over - home team doesn't bat in bottom 9th when already leading
                newState.IsFinal = true;
                return newState;
            }

            // Reset for next half
            newState.Outs = 0;
            newState.OnFirst = false;
            newState.OnSecond = false;
            newState.OnThird = false;

            // Transition half/inning
            if (newState.Half == InningHalf.Top) {
                newState.Half = InningHalf.Bottom;
                // Swap offense/defense
                (newState.Offense, newState.Defense) =
                    (newState.Defense, newState.Offense);
            } else {
                newState.Half = InningHalf.Top;
                newState.Inning++;
                // Swap offense/defense
                (newState.Offense, newState.Defense) =
                    (newState.Defense, newState.Offense);
            }
        }

        return newState;
    }
}
```

### 5.4 Walk-off Detection Logic

```csharp
private bool IsWalkoffSituation(GameState state) {
    return state.Half == InningHalf.Bottom &&
           state.Inning >= 9 &&
           state.HomeScore > state.AwayScore;
}
```

### 5.5 Extra Innings Logic

```csharp
// After 9 complete innings, if tied, continue
// Game ends when:
// 1. Home team leads after bottom half (walk-off), OR
// 2. Away team leads after top half AND home fails to tie/lead in bottom

private bool IsGameComplete(GameState state) {
    if (state.IsFinal) return true;

    // Must complete at least 9 innings
    if (state.Inning < 9) return false;

    // If in top half of inning, game not complete
    if (state.Half == InningHalf.Top) return false;

    // If in bottom half and home leads, walk-off
    if (state.HomeScore > state.AwayScore) {
        return true;
    }

    // If tied, continue to next inning
    return false;
}
```

---

## 6. Acceptance Criteria

### AC-1: Backward Compatibility
- ✅ All existing tests pass without modification
- ✅ Existing [`GameState`](src/DiamondSim/GameState.cs) count tracking still works
- ✅ No breaking changes to public APIs

### AC-2: State Transitions
- ✅ Runs are applied to correct team immediately
- ✅ Outs increment correctly (0 → 1 → 2 → 3)
- ✅ Bases update correctly based on PA outcome
- ✅ Batting order advances correctly (0-8, wraps to 0)
- ✅ Half-inning transitions occur on 3rd out
- ✅ Outs reset to 0 on half transition
- ✅ Bases clear on half transition

### AC-3: Walk-off Scenarios
- ✅ Walk-off detected in bottom 9th when home leads
- ✅ Walk-off detected in bottom extras when home leads
- ✅ Game ends immediately on walk-off (IsFinal = true)
- ✅ No further PAs processed after walk-off
- ✅ Line score reflects partial inning correctly

### AC-4: Extra Innings
- ✅ Game continues if tied after 9 complete innings
- ✅ Halves alternate correctly in extras
- ✅ Walk-off rule applies in bottom halves of extras
- ✅ Game ends when one team leads after completed inning

### AC-5: Line Score Accuracy
- ✅ Runs per inning tracked correctly
- ✅ Line score totals match final game scores
- ✅ Walk-off partial innings show actual runs scored (not 'X')
- ✅ Skipped bottom 9th shows 'X' symbol (home leading after top 9th)
- ✅ Bottom 9th is played when home is losing or tied after top 9th
- ✅ Extra innings extend line score appropriately

### AC-6: LOB Tracking
- ✅ LOB counted at moment of 3rd out
- ✅ Walk-off scenarios have LOB = 0 for partial inning
- ✅ Team LOB totals are accurate

### AC-7: Box Score Statistics (v0.2)
- ✅ Batter stats increment correctly per PA
- ✅ Pitcher stats increment correctly per PA
- ✅ Team hits = sum of individual hits
- ✅ Defensive outs: 27 per team in full game; 24 for away if home skips bottom 9th; extras add 3 per completed defensive half
- ✅ No rate calculations in engine
- ✅ Tests mirror v0.2 RBI-on-ROE simplification

### AC-8: Deterministic Behavior (CRITICAL)
- ✅ **No RNG calls in state transition logic** (enforced by test)
- ✅ Same input always produces same output
- ✅ Tests are fully deterministic
- ✅ Lint/test fails if `IRandomSource` is used in inning scoring code

### AC-9: Code Quality
- ✅ New code follows `.rules/style.md` (K&R braces, file-scoped namespaces)
- ✅ Tests follow `.rules/testing.md` (NUnit, deterministic)
- ✅ XML documentation comments for public APIs
- ✅ No compiler warnings or errors

---

## 7. Testing Strategy

### 7.1 Unit Tests

**Test File:** [`tests/DiamondSim.Tests/InningScoreTests.cs`](tests/DiamondSim.Tests/InningScoreTests.cs)

#### Test: Walk-off in Bottom 9th
```csharp
[Test]
public void ApplyPA_Bottom9thTieBreakingHomeRun_EndsGameImmediately() {
    // Arrange: Tie game, bottom 9th, 2 outs
    var state = new GameState {
        Inning = 9,
        Half = InningHalf.Bottom,
        Outs = 2,
        AwayScore = 3,
        HomeScore = 3,
        // ... other fields
    };

    var resolution = new PaResolution {
        OutsAdded = 0,
        RunsScored = 1, // Solo HR
        NewBases = new BaseState(false, false, false),
        Type = PaType.HomeRun
    };

    // Act
    var newState = scorekeeper.ApplyPlateAppearance(state, resolution);

    // Assert
    Assert.That(newState.HomeScore, Is.EqualTo(4));
    Assert.That(newState.IsFinal, Is.True);
    Assert.That(newState.Outs, Is.EqualTo(2)); // Outs don't reset on walk-off
}
```

#### Test: Double Play Ends Half
```csharp
[Test]
public void ApplyPA_DoublePlayWith1Out_EndsHalfAndClearsBases() {
    // Arrange: 1 out, runners on 1st and 2nd
    var state = new GameState {
        Inning = 5,
        Half = InningHalf.Top,
        Outs = 1,
        OnFirst = true,
        OnSecond = true,
        OnThird = false,
        // ... other fields
    };

    var resolution = new PaResolution {
        OutsAdded = 2, // Double play
        RunsScored = 0,
        NewBases = new BaseState(false, false, false),
        Type = PaType.InPlayOut,
        Flags = new PaFlags { IsDoublePlay = true }
    };

    // Act
    var newState = scorekeeper.ApplyPlateAppearance(state, resolution);

    // Assert
    Assert.That(newState.Outs, Is.EqualTo(0)); // Reset
    Assert.That(newState.OnFirst, Is.False); // Cleared
    Assert.That(newState.OnSecond, Is.False); // Cleared
    Assert.That(newState.OnThird, Is.False); // Cleared
    Assert.That(newState.Half, Is.EqualTo(InningHalf.Bottom)); // Flipped
}
```

#### Test: Bases Loaded Walk
```csharp
[Test]
public void ApplyPA_BasesLoadedWalk_Scores1RunAndBasesRemainLoaded() {
    // Arrange: Bases loaded, 1 out
    var state = new GameState {
        Inning = 3,
        Half = InningHalf.Bottom,
        Outs = 1,
        OnFirst = true,
        OnSecond = true,
        OnThird = true,
        HomeScore = 2,
        // ... other fields
    };

    var resolution = new PaResolution {
        OutsAdded = 0,
        RunsScored = 1, // Runner from 3rd scores
        NewBases = new BaseState(true, true, true), // Still loaded
        Type = PaType.BB  // Use PaType.BB consistently
    };

    // Act
    var newState = scorekeeper.ApplyPlateAppearance(state, resolution);

    // Assert
    Assert.That(newState.HomeScore, Is.EqualTo(3));
    Assert.That(newState.Outs, Is.EqualTo(1)); // No change
    Assert.That(newState.OnFirst, Is.True);
    Assert.That(newState.OnSecond, Is.True);
    Assert.That(newState.OnThird, Is.True);
}
```

#### Test: Extra Innings Continue Until Winner
```csharp
[Test]
public void ApplyPA_TiedAfter9Innings_ContinuesToExtras() {
    // Arrange: Bottom 9th, 3rd out, still tied
    var state = new GameState {
        Inning = 9,
        Half = InningHalf.Bottom,
        Outs = 2,
        AwayScore = 5,
        HomeScore = 5,
        // ... other fields
    };

    var resolution = new PaResolution {
        OutsAdded = 1, // 3rd out
        RunsScored = 0,
        NewBases = new BaseState(false, false, false),
        Type = PaType.InPlayOut
    };

    // Act
    var newState = scorekeeper.ApplyPlateAppearance(state, resolution);

    // Assert
    Assert.That(newState.IsFinal, Is.False); // Game continues
    Assert.That(newState.Inning, Is.EqualTo(10)); // Extra innings
    Assert.That(newState.Half, Is.EqualTo(InningHalf.Top));
    Assert.That(newState.Outs, Is.EqualTo(0)); // Reset
}
```

#### Test: Home Team Losing After Top 9th - Bottom 9th Must Be Played
```csharp
[Test]
public void ApplyPA_Top9thEndsWithHomeLosing_Bottom9thMustBePlayed() {
    // Arrange: Top 9th, 3rd out, home team losing
    var state = new GameState {
        Inning = 9,
        Half = InningHalf.Top,
        Outs = 2,
        AwayScore = 5,
        HomeScore = 3,
        // ... other fields
    };

    var resolution = new PaResolution {
        OutsAdded = 1, // 3rd out
        RunsScored = 0,
        NewBases = new BaseState(false, false, false),
        Type = PaType.InPlayOut
    };

    // Act
    var newState = scorekeeper.ApplyPlateAppearance(state, resolution);

    // Assert
    Assert.That(newState.IsFinal, Is.False); // Game continues
    Assert.That(newState.Inning, Is.EqualTo(9)); // Still 9th inning
    Assert.That(newState.Half, Is.EqualTo(InningHalf.Bottom)); // Transitioned to bottom
    Assert.That(newState.Outs, Is.EqualTo(0)); // Reset
    Assert.That(newState.AwayScore, Is.EqualTo(5));
    Assert.That(newState.HomeScore, Is.EqualTo(3));
    // Bottom 9th must be played when home is losing
}
```

#### Test: Home Team Leading After Top 9th - Skip Bottom 9th
```csharp
[Test]
public void ApplyPA_Top9thEndsWithHomeLeading_SkipsBottom9th() {
    // Arrange: Top 9th, 3rd out, home team leading
    var state = new GameState {
        Inning = 9,
        Half = InningHalf.Top,
        Outs = 2,
        AwayScore = 3,
        HomeScore = 4,
        // ... other fields
    };

    var resolution = new PaResolution {
        OutsAdded = 1, // 3rd out
        RunsScored = 0,
        NewBases = new BaseState(false, false, false),
        Type = PaType.InPlayOut
    };

    // Act
    var newState = scorekeeper.ApplyPlateAppearance(state, resolution);

    // Assert
    Assert.That(newState.IsFinal, Is.True); // Game over
    Assert.That(newState.Inning, Is.EqualTo(9)); // Still 9th inning
    Assert.That(newState.Half, Is.EqualTo(InningHalf.Top)); // Still top half
    Assert.That(newState.AwayScore, Is.EqualTo(3));
    Assert.That(newState.HomeScore, Is.EqualTo(4));
    // Line score should show 'X' for home 9th inning
}
```

#### Test: Top 9th Ends With Home Trailing - Bottom 9th Must Be Played
```csharp
[Test]
public void Top9_EndsWithHomeTrailing_PlaysBottom9() {
    // After top 9: Away 5, Home 3 → Bottom 9 must start
    // Arrange: Top 9th, 3rd out, home team trailing
    var state = new GameState {
        Inning = 9,
        Half = InningHalf.Top,
        Outs = 2,
        AwayScore = 5,
        HomeScore = 3,
        // ... other fields
    };

    var resolution = new PaResolution {
        OutsAdded = 1, // 3rd out
        RunsScored = 0,
        NewBases = new BaseState(false, false, false),
        Type = PaType.InPlayOut
    };

    // Act
    var newState = scorekeeper.ApplyPlateAppearance(state, resolution);

    // Assert
    Assert.That(newState.IsFinal, Is.False); // Game continues
    Assert.That(newState.Inning, Is.EqualTo(9)); // Still 9th inning
    Assert.That(newState.Half, Is.EqualTo(InningHalf.Bottom)); // Transitioned to bottom
    Assert.That(newState.Outs, Is.EqualTo(0)); // Reset for bottom 9th
}
```

#### Test: Skip Bottom 9th When Home Leads After Top 9th
```csharp
[Test]
public void SkipBottom9_WhenHomeLeadsAfterTop9_IsFinalTrueAndXRecorded() {
    // Simulate end of Top 9 with Home leading.
    // Arrange: Top 9th, 3rd out, home team leading
    var state = new GameState {
        Inning = 9,
        Half = InningHalf.Top,
        Outs = 2,
        AwayScore = 3,
        HomeScore = 4,
        // ... other fields
    };

    var resolution = new PaResolution {
        OutsAdded = 1, // 3rd out
        RunsScored = 0,
        NewBases = new BaseState(false, false, false),
        Type = PaType.InPlayOut
    };

    // Act
    var newState = scorekeeper.ApplyPlateAppearance(state, resolution);

    // Assert
    Assert.That(newState.IsFinal, Is.True); // Game over
    Assert.That(newState.Inning, Is.EqualTo(9)); // Still 9th inning
    Assert.That(newState.Half, Is.EqualTo(InningHalf.Top)); // Still top half
    // Expect: Bottom 9 not started; line score home[9] == 'X'
}
```

#### Test: Walk-off Mid-Inning Sets IsFinal, Records Partial Runs, LOB Zero
```csharp
[Test]
public void Walkoff_MidInningBottom10_SetsIsFinal_RecordsPartialRuns_LobZero() {
    // Bottom 10, tie game, 1 out, R2 scores on single.
    // Arrange: Bottom 10th, 1 out, runner on 2nd, tie game
    var state = new GameState {
        Inning = 10,
        Half = InningHalf.Bottom,
        Outs = 1,
        OnFirst = false,
        OnSecond = true,
        OnThird = false,
        AwayScore = 5,
        HomeScore = 5,
        // ... other fields
    };

    var resolution = new PaResolution {
        OutsAdded = 0,
        RunsScored = 1, // Runner from 2nd scores
        NewBases = new BaseState(true, false, false), // Batter on 1st
        Type = PaType.Single
    };

    // Act
    var newState = scorekeeper.ApplyPlateAppearance(state, resolution);

    // Assert
    Assert.That(newState.IsFinal, Is.True); // Game over (walk-off)
    Assert.That(newState.HomeScore, Is.EqualTo(6)); // Home wins 6-5
    Assert.That(newState.Outs, Is.EqualTo(1)); // Outs don't reset on walk-off
    // Expect: IsFinal = true; LOB = 0 (no 3rd out); partial inning runs recorded
}
```

#### Test: Infinite Extras Safety Guard
```csharp
[Test]
public void ApplyPA_InningExceeds99_ThrowsException() {
    // Arrange: Inning 100 (safety limit exceeded)
    var state = new GameState {
        Inning = 100,
        Half = InningHalf.Top,
        Outs = 0,
        AwayScore = 5,
        HomeScore = 5,
        // ... other fields
    };

    var resolution = new PaResolution {
        OutsAdded = 1,
        RunsScored = 0,
        NewBases = new BaseState(false, false, false),
        Type = PaType.InPlayOut
    };

    // Act & Assert
    Assert.Throws<InvalidOperationException>(() =>
        scorekeeper.ApplyPlateAppearance(state, resolution));
}
```

#### Test: No RNG Calls Allowed (Determinism Enforcement)
```csharp
[Test]
public void InningScoring_NeverCallsRNG_EnforcesDeterminism() {
    // This test ensures that inning scoring logic is purely deterministic
    // and does not make any RNG calls.

    // Approach 1: Mock IRandomSource and verify it's never called
    var mockRandom = new Mock<IRandomSource>();
    mockRandom.Setup(r => r.NextDouble()).Throws(
        new InvalidOperationException("RNG call detected in inning scoring! " +
        "All randomness must occur upstream in PRD-02/03."));

    // Arrange: Normal game state
    var state = new GameState {
        Inning = 5,
        Half = InningHalf.Top,
        Outs = 1,
        AwayScore = 3,
        HomeScore = 2,
        // ... other fields
    };

    var resolution = new PaResolution {
        OutsAdded = 1,
        RunsScored = 0,
        NewBases = new BaseState(false, false, false),
        Type = PaType.InPlayOut
    };

    // Act - should NOT throw because no RNG should be called
    var newState = scorekeeper.ApplyPlateAppearance(state, resolution);

    // Assert
    Assert.That(newState.Outs, Is.EqualTo(2));
    // If we get here, no RNG was called (test passes)

    // Verify mock was never called
    mockRandom.Verify(r => r.NextDouble(), Times.Never);
    mockRandom.Verify(r => r.Next(It.IsAny<int>()), Times.Never);
    mockRandom.Verify(r => r.Next(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
}
```

**Alternative Approach: Static Analysis**
```csharp
// Add to CI pipeline or as a separate test
[Test]
public void InningScoring_CodeAnalysis_NoRandomSourceReferences() {
    // Use reflection or static analysis to verify that InningScorekeeper
    // and related classes do not reference IRandomSource
    var assembly = typeof(InningScorekeeper).Assembly;
    var inningTypes = assembly.GetTypes()
        .Where(t => t.Namespace == "DiamondSim" &&
                   (t.Name.Contains("Inning") || t.Name.Contains("Score")));

    foreach (var type in inningTypes) {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var method in methods) {
            var parameters = method.GetParameters();
            Assert.That(parameters.Any(p => p.ParameterType == typeof(IRandomSource)),
                Is.False,
                $"Method {type.Name}.{method.Name} has IRandomSource parameter. " +
                "RNG calls are not allowed in inning scoring logic.");
        }
    }
}
```

### 7.2 Integration Tests

#### Test: Complete 9-Inning Game
```csharp
[Test]
public void SimulateGame_9Innings_ProducesValidLineScore() {
    // Simulate a complete 9-inning game
    // Verify:
    // - Line score totals match final scores
    // - All outs sum to 27 per team
    // - LOB is tracked correctly
    // - Box score stats are consistent
}
```

#### Test: Walk-off Home Run
```csharp
[Test]
public void SimulateGame_WalkoffHomeRun_EndsInBottom9th() {
    // Simulate game with walk-off scenario
    // Verify:
    // - Game ends immediately
    // - Partial inning line score is correct
    // - Losing team has no bottom 9th entry
}
```

### 7.3 Validation Tests

#### Test: Line Score Validation
```csharp
[Test]
public void LineScore_AfterGame_TotalsMatchFinalScores() {
    // Verify line score totals equal GameState scores
}
```

#### Test: Box Score Validation
```csharp
[Test]
public void BoxScore_AfterGame_TeamHitsEqualIndividualHits() {
    // Verify team stats sum correctly
}
```

---

## 8. Risks & Mitigation

### Risk 1: Walk-off Edge Cases
**

Description:** Walk-off scenarios have multiple edge cases that must be handled correctly.

**Likelihood:** Medium
**Impact:** High

**Mitigation:**
- Implement comprehensive walk-off tests covering all scenarios
- Test walk-off in bottom 9th, 10th, 11th, etc.
- Test walk-off with different run margins (1 run, multiple runs)
- Test walk-off with different outs (0, 1, 2 outs)
- Verify line score reflects partial inning correctly
- Document walk-off detection logic clearly

---

### Risk 2: Half-Inning Transition Complexity
**Description:** Managing state resets and side switches during half-inning transitions is complex and error-prone.

**Likelihood:** Medium
**Impact:** High

**Mitigation:**
- Create dedicated helper method for half-inning transitions
- Test all transition scenarios (Top→Bottom, Bottom→Top)
- Verify outs reset, bases clear, sides switch correctly
- Test transition at different innings (1st, 5th, 9th, extras)
- Add assertions to verify state consistency after transitions
- Document transition logic with clear comments

---

### Risk 3: Extra Innings Termination
**Description:** Extra innings logic must correctly determine when game is complete.

**Likelihood:** Medium
**Impact:** High

**Mitigation:**
- Implement clear game completion logic
- Test tied games extending to 10th, 11th, 12th innings
- Test away team winning in top of extra inning
- Test home team walk-off in bottom of extra inning
- Verify game doesn't end prematurely or continue indefinitely
- Add safety guard: abort if inning > 99 (test-only failure)
- Test that safety guard throws exception when triggered

---

### Risk 4: Line Score Synchronization
**Description:** Line score totals must always match game state scores.

**Likelihood:** Low
**Impact:** Medium

**Mitigation:**
- Implement validation checks after each half-inning
- Add assertions in tests to verify line score consistency
- Use computed properties for totals (not stored values)
- Test line score with various scoring patterns
- Document line score update logic clearly

---

### Risk 5: Box Score Stat Accuracy
**Description:** Player statistics must be tracked accurately and sum correctly.

**Likelihood:** Medium
**Impact:** Medium

**Mitigation:**
- Implement stat tap interface with clear increment rules
- Test each stat type individually (AB, H, BB, K, etc.)
- Add validation tests for team totals vs. individual totals
- Document stat counting rules (e.g., when AB increments)
- Follow official baseball scoring rules
- Test edge cases (SF, HBP, errors)

---

## 9. Implementation Notes

### 9.1 Implementation Phases

**Phase 1: Core State Structure**
1. Expand [`GameState`](src/DiamondSim/GameState.cs) with new fields
2. Add `InningHalf`, `Team` enums to [`Outcomes.cs`](src/DiamondSim/Outcomes.cs)
3. Create `PaResolution` and related types
4. Ensure backward compatibility with existing tests

**Phase 2: State Transition Logic**
5. Create [`InningScorekeeper.cs`](src/DiamondSim/InningScorekeeper.cs)
6. Implement `ApplyPlateAppearance()` method
7. Implement half-inning transition logic
8. Implement walk-off detection
9. Implement extra innings logic

**Phase 3: Line Score & LOB**
10. Create [`LineScore.cs`](src/DiamondSim/LineScore.cs)
11. Implement run tracking per inning
12. Implement LOB tracking
13. Add validation methods

**Phase 4: Box Score Statistics**
14. Create [`BoxScore.cs`](src/DiamondSim/BoxScore.cs)
15. Define `BatterStats` and `PitcherStats` types
16. Implement stat tap interface
17. Add stat increment logic per PA type

**Phase 5: Testing & Validation**
18. Create [`InningScoreTests.cs`](tests/DiamondSim.Tests/InningScoreTests.cs)
19. Implement walk-off tests
20. Implement half-transition tests
21. Implement extra innings tests
22. Implement line score validation tests
23. Implement box score validation tests
24. Verify all existing tests still pass

### 9.2 State Transition Order (Critical)

The order of operations in `ApplyPlateAppearance()` is critical:

1. **Apply runs FIRST** - Runs must be credited before checking walk-off
2. **Apply outs** - Increment out count
3. **Apply bases** - Update base state
4. **Advance lineup** - Move to next batter
5. **Check walk-off** - If conditions met, end game immediately
6. **Check half close** - If 3 outs, perform transition
7. **Check inning limit** - If inning > 99, throw exception (safety guard)

**Rationale:** Runs must be applied before walk-off check so that the score comparison is accurate. Walk-off check must occur before half-close check to prevent unnecessary state resets. Inning limit check prevents infinite loops in tests or simulations.

### 9.3 Walk-off and Game-Ending Rules

**Walk-off (home team takes lead in bottom 9+):**
Walk-off occurs when ALL conditions are met:
- `Half == InningHalf.Bottom`
- `Inning >= 9`
- `HomeScore > AwayScore`

When walk-off detected:
- Set `IsFinal = true`
- Do NOT reset outs or bases
- Do NOT advance to next half
- Record partial inning in line score
- Return immediately (no further processing)

**Skip bottom 9th (home team losing after top 9th):**
Game ends without bottom 9th when ALL conditions are met:
- `Inning == 9`
- `Half == InningHalf.Top`
- `Outs >= 3` (3rd out just recorded)
- `HomeScore < AwayScore`

When this occurs:
- Set `IsFinal = true`
- Do NOT transition to bottom 9th
- Line score shows 'X' or '-' for home team's 9th inning
- This is standard baseball: home team doesn't bat when already losing in 9th

### 9.4 Half-Inning Transition Rules

When `Outs >= 3`:
1. Count LOB (occupied bases at moment of 3rd out)
2. Flush runs scored this half to line score
3. Reset `Outs = 0`
4. Clear bases: `OnFirst = OnSecond = OnThird = false`
5. If `Half == Top`:
   - Set `Half = Bottom`
   - Swap `Offense` and `Defense`
6. If `Half == Bottom`:
   - Set `Half = Top`
   - Increment `Inning`
   - Swap `Offense` and `Defense`

### 9.4b Loop Safety Guard

To prevent infinite loops in extra innings (e.g., due to bugs in test data or simulation logic):

```csharp
// At the start of ApplyPlateAppearance or in game loop
if (state.Inning > 99) {
    throw new InvalidOperationException(
        $"Game exceeded maximum inning limit (99). Current inning: {state.Inning}. " +
        "This indicates a potential infinite loop in the game simulation.");
}
```

**When to check:**
- At the beginning of `ApplyPlateAppearance()` method
- OR in the main game loop before processing each PA

**Purpose:**
- Prevents runaway simulations in tests
- Catches bugs in extra innings logic
- In real baseball, no game has ever gone beyond 26 innings in modern era
- Limit of 99 innings is extremely conservative and should never be reached in practice

### 9.5 Box Score Stat Rules (v0.2)

**At-Bat (AB) increments when:**
- `InPlayOut` - Ball in play resulting in out
- `Single` - Batter reaches first base
- `Double` - Batter reaches second base
- `Triple` - Batter reaches third base
- `HomeRun` - Batter circles all bases
- `ReachOnError` - Batter reaches base on defensive error

**At-Bat does NOT increment when:**
- `Walk (BB)` - Four balls
- `HBP` - Hit by pitch
- **Note:** Sacrifice fly (SF) is not modeled in v0.2; treat as regular `InPlayOut` (counts as AB). SF will be added in a later version.

**Hits (H) increment when:**
- `Single`, `Double`, `Triple`, or `HomeRun`
- Does NOT increment on `ReachOnError` (error is not a hit)

**RBI (Runs Batted In) rules:**
- RBI equals the number of runs that score on the play
- **Batter's own run counts as RBI ONLY on Home Run**
- For other hit types (Single, Double, Triple), only count runners who scored, not the batter
- **v0.2 Simplification:** For `ReachOnError`, award RBI if any runner scores on the play (official scorer logic will be refined later)
- Examples:
  - Single with runner on 3rd who scores: RBI = 1 (runner scored, batter did not)
  - Home run with bases empty: RBI = 1 (batter's run counts)
  - Home run with 2 runners on: RBI = 3 (all 3 runs count)
  - Error with runner on 3rd who scores: RBI = 1 (v0.2 simplification)

**Runs (R) increments when:**
- Runner crosses home plate (tracked separately from RBI)
- Includes batter scoring on HR, runner scoring on hit, etc.

**Total Bases (TB) calculation:**
- Single: TB = 1
- Double: TB = 2
- Triple: TB = 3
- Home Run: TB = 4
- Walk, HBP, Error: TB = 0

### 9.6 Code Organization

```
src/DiamondSim/
├── GameState.cs (modified - add game context fields)
├── Outcomes.cs (modified - add InningHalf, Team enums)
├── PaResolution.cs (new - PA outcome payload)
├── InningScorekeeper.cs (new - state transition logic)
├── LineScore.cs (new - inning-by-inning runs)
└── BoxScore.cs (new - player statistics)

tests/DiamondSim.Tests/
└── InningScoreTests.cs (new - comprehensive state tests)
```

---

## 10. Future Enhancements (Out of Scope)

The following features are explicitly out of scope for this PRD but may be considered in future iterations:

1. **Inherited Runners:** Tracking which pitcher is responsible for runners on base
2. **Earned vs. Unearned Runs:** Distinguishing earned runs from unearned (error-related)
3. **Pitcher Substitutions:** Managing bullpen and pitcher changes
4. **Defensive Substitutions:** Tracking fielding changes and positions
5. **Pinch Hitters/Runners:** Managing offensive substitutions
6. **Advanced Statistics:** OPS, ERA, FIP, wOBA, etc. (calculated externally)
7. **Play-by-Play Narrative:** Generating human-readable game descriptions
8. **Replay/Undo:** Ability to rewind game state
9. **Save/Load Game State:** Serialization for pausing/resuming games
10. **Real-time Updates:** Event streaming for live game monitoring

---

## 11. Dependencies

### Internal Dependencies
- [`GameState.cs`](src/DiamondSim/GameState.cs) - Modified to add game context
- [`Outcomes.cs`](src/DiamondSim/Outcomes.cs) - Modified to add enums
- [`Model.cs`](src/DiamondSim/Model.cs) - Player definitions (unchanged)
- [`BallInPlayResolver.cs`](src/DiamondSim/BallInPlayResolver.cs) - Part 3 (completed)
- [`AtBatSimulator.cs`](src/DiamondSim/AtBatSimulator.cs) - Part 2 (completed)

### External Dependencies
- .NET 8 SDK
- NUnit 3.x test framework (per `.rules/testing.md`)
- No new external dependencies required

### Dependency Chain
```
Part 1: Count-Conditioned Contact (completed)
    ↓
Part 2: At-Bat Loop (completed)
    ↓
Part 3: Ball-In-Play Resolution (completed)
    ↓
Part 4: Inning Scoring (this PRD) ← YOU ARE HERE
    ↓
Future: Full game simulation with lineups, substitutions, etc.
```

---

## 12. Documentation Requirements

### Code Documentation
- XML documentation comments for all public APIs
- Inline comments explaining state transition logic
- Comments documenting walk-off detection rules
- Comments explaining half-inning transition sequence
- Clear explanation of LOB calculation
- Documentation of box score stat increment rules

### Test Documentation
- Clear test names describing scenarios
- Comments explaining expected outcomes
- Documentation of edge cases being tested
- Comments explaining state setup for each test
- Example calculations for validation checks

### Design Documentation
- State transition diagram (included in this PRD)
- Walk-off detection flowchart
- Half-inning transition flowchart
- Box score stat increment decision tree

---

## 13. Success Criteria Summary

This feature will be considered successfully implemented when:

1. ✅ [`GameState`](src/DiamondSim/GameState.cs) tracks full game context (inning, half, outs, bases, score, lineup)
2. ✅ `PaResolution` payload structure is defined
3. ✅ [`InningScorekeeper`](src/DiamondSim/InningScorekeeper.cs) implements state transition logic
4. ✅ Walk-off detection works correctly in bottom 9th and extras
5. ✅ Half-inning transitions reset outs and bases correctly
6. ✅ Extra innings continue until winner is determined
7. ✅ [`LineScore`](src/DiamondSim/LineScore.cs) tracks runs per inning accurately
8. ✅ Line score totals match final game scores
9. ✅ LOB is tracked correctly at end of each half
10. ✅ [`BoxScore`](src/DiamondSim/BoxScore.cs) tracks basic player statistics (v0.2 scope)
11. ✅ Team stats sum correctly (hits, outs, etc.)
12. ✅ All state transitions are deterministic (no RNG calls)
13. ✅ All existing tests pass without modification
14. ✅ New tests cover walk-offs, transitions, extras, LOB
15. ✅ Code follows `.rules/style.md` and `.rules/testing.md`
16. ✅ No compiler warnings or errors

---

## 14. Appendix

### A. State Transition Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    Start of Game                            │
│  Inning=1, Half=Top, Outs=0, Bases=Empty, Score=0-0        │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                  Plate Appearance                           │
│  Apply PaResolution to GameState                            │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
                    ┌───────────────┐
                    │ Apply Runs    │
                    └───────────────┘
                            │
                            ▼
                    ┌───────────────┐
                    │ Apply Outs    │
                    └───────────────┘
                            │
                            ▼
                    ┌───────────────┐
                    │ Apply Bases   │
                    └───────────────┘
                            │
                            ▼
                    ┌───────────────┐
                    │ Advance Lineup│
                    └───────────────┘
                            │
                            ▼
                ┌───────────────────────┐
                │ Walk-off Check?       │
                │ (Bottom, Inning≥9,    │
                │  Home leads)          │
                └───────────────────────┘
                    │               │
                   Yes             No
                    │               │
                    ▼               ▼
            ┌──────────────┐  ┌──────────────┐
            │ Set IsFinal  │  │ Outs >= 3?   │
            │ Game Over    │  └──────────────┘
            └──────────────┘        │       │
                                   Yes     No
                                    │       │
                                    ▼       ▼
                            ┌──────────────┐ │
                            │ Half Close   │ │
                            │ - Count LOB  │ │
                            │ - Flush Line │ │
                            │ - Reset Outs │ │
                            │ - Clear Bases│ │
                            │ - Flip Sides │ │
                            └──────────────┘ │
                                    │        │
                                    ▼        │
                            ┌──────────────┐ │
                            │ Next Half    │ │
                            │ or Inning    │ │
                            └──────────────┘ │
                                    │        │
                                    └────────┘
                                         │
                                         ▼
                                ┌──────────────┐
                                │ Next PA      │
                                └──────────────┘
```

### B. Walk-off Scenarios

**Scenario 1: Bottom 9th, Tie Game, Solo HR**
- Before: Inning=9, Half=Bottom, Outs=2, Score=3-3
- PA: Home run (1 run)
- After: Score=3-4, IsFinal=true
- Result: Home team wins 4-3

**Scenario 2: Bottom 10th, Away Leads, 2-Run Single**
- Before: Inning=10, Half=Bottom, Outs=1, Score=5-4, Runners on 2nd and 3rd
- PA: Single (2 runs score)
- After: Score=5-6, IsFinal=true
- Result: Home team wins 6-5 in 10 innings

**Scenario 3: Top 9th ends, Home LEADING — Bottom 9th skipped**
- Before: End of Top 9th, Score = Away 5, Home 6
- After: IsFinal = true; Bottom 9th not played
- Line score: Home 9th shows **'X'**

**Scenario 4: Bottom 9th, Home Leads, Routine Out**
- Before: Inning=9, Half=Bottom, Outs=0, Score=4-3
- PA: Ground out (0 runs)
- After: Outs=1, Score=4-3, IsFinal=false
- Result: Game continues (home already leading, but must complete inning)

### C. Half-Inning Transition Examples

**Example 1: Top 3rd → Bottom 3rd**
- Before: Inning=3, Half=Top, Outs=2, OnFirst=true, OnSecond=true
- PA: Strikeout (3rd out)
- After: Inning=3, Half=Bottom, Outs=0, Bases=Empty
- LOB: 2 (runners on 1st and 2nd)

**Example 2: Bottom 5th → Top 6th**
- Before: Inning=5, Half=Bottom, Outs=2, OnThird=true
- PA: Fly out (3rd out)
- After: Inning=6, Half=Top, Outs=0, Bases=Empty
- LOB: 1 (runner on 3rd)

### D. Extra Innings Examples

**Example 1: Tied After 9, Home Wins in 10th**
- End of 9: Score=5-5
- Top 10: Away scores 0, Score=5-5
- Bottom 10: Home scores 1, Score=5-6
- Result: Home wins 6-5 in 10 innings (walk-off)

**Example 2: Tied After 9, Away Wins in 11th**
- End of 9: Score=3-3
- Top 10: Away scores 0, Score=3-3
- Bottom 10: Home scores 0, Score=3-3
- Top 11: Away scores 2, Score=5-3
- Bottom 11: Home scores 1, Score=5-4
- Result: Away wins 5-4 in 11 innings (completed inning)

### E. Box Score Stat Examples (v0.2)

**Batter Example:**
- PA 1: Single → AB+1, H+1, 1B+1, PA+1, TB+1
- PA 2: Walk → BB+1, PA+1
- PA 3: Strikeout → AB+1, K+1, PA+1
- PA 4: Home Run (2 RBI) → AB+1, H+1, HR+1, RBI+2, R+1, PA+1, TB+4
- Totals: AB=3, H=2, 1B=1, HR=1, BB=1, K=1, RBI=2, R=1, PA=4, TB=5

**Pitcher Example:**
- Batter 1: Strikeout → BF+1, K+1, OutsRecorded+1
- Batter 2: Single → BF+1, H+1
- Batter 3: Home Run (2 runs) → BF+1, H+1, HR+1, R+2, ER+2
- Batter 4: Ground out → BF+1, OutsRecorded+1
- Totals: BF=4, OutsRecorded=2 (0.67 IP), H=2, R=2, ER=2, K=1, HR=1

### F. LOB Calculation Examples

**Example 1: 3rd Out with Bases Loaded**
- Before 3rd out: OnFirst=true, OnSecond=true, OnThird=true
- LOB = 3

**Example 2: 3rd Out with Runner on 2nd**
- Before 3rd out: OnFirst=false, OnSecond=true, OnThird=false
- LOB = 1

**Example 3: Walk-off (Partial Inning)**
- Game ends mid-inning
- LOB = 0 (no 3rd out recorded)

### G. References
- Part 1 PRD: [`.prd/20251024_01_CountConditionedContact.md`](.prd/20251024_01_CountConditionedContact.md)
- Part 2 PRD: [`.prd/20251024_02_AtBatLoop.md`](.prd/20251024_02_AtBatLoop.md)
- Part 3 PRD: [`.prd/20251024_03_BallInPlayResolution.md`](.prd/20251024_03_BallInPlayResolution.md)
- Game state: [`src/DiamondSim/GameState.cs`](src/DiamondSim/GameState.cs)
- Outcomes: [`src/DiamondSim/Outcomes.cs`](src/DiamondSim/Outcomes.cs)
- Style guide: [`.rules/style.md`](.rules/style.md)
- Testing guide: [`.rules/testing.md`](.rules/testing.md)

---

**Document End**
