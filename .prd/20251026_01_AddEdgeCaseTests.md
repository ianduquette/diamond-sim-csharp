
# PRD: Scoring & Walk-Off Edge Case Tests

**Status:** Draft
**Created:** 2025-10-26
**Epic:** Baseball Simulation Engine - Scoring & Recording
**Dependencies:** PRD-20251025-01 (Core Rules Corrections)

---

## 1. Overview

### 1.1 Purpose

This PRD defines additional edge case tests to be added to existing test fixtures in the DiamondSim test suite. These tests will validate critical boundary conditions in walk-off scenarios, earned/unearned run classification, and rare game situations, ensuring comprehensive coverage of the corrected scoring rules.

### 1.2 Key Definitions

**AdvanceOnError Semantics:** The `AdvanceOnError` flags in [`PaResolution`](src/DiamondSim/PaResolution.cs) are keyed to the runner's **starting base** on the play. For example, if a runner on 2nd base scores due to an error, `AdvanceOnError.OnSecond = true`.

### 1.3 Problem Statement

While the core scoring rules have been corrected and basic test coverage exists, the current test suite lacks coverage for specific edge cases:

1. **Walk-off scenarios with errors:** ROE and HBP in game-ending situations
2. **Walk-off sacrifice fly:** RBI attribution when error enables the score
3. **Mixed error involvement:** Multiple runners with partial error attribution (v1-light policy)
4. **Triple plays:** Rare outs and LOB timing
5. **Non-winning home runs:** HR in bottom 9th that doesn't end the game
6. **Skip-bottom-9th with runners:** Base state independence

These edge cases represent real baseball scenarios that must be handled correctly to prevent regression.

### 1.4 Scope

**In Scope:**
- Add 12 new test methods to existing test fixtures
- Test walk-off clamping vs HR exception in edge scenarios
- Validate RBI attribution in game-ending situations (ROE, HBP, SF)
- Test earned/unearned classification with mixed error involvement
- Validate triple play handling and LOB timing
- Test skip-bottom-9th with various base states

**Out of Scope:**
- Creating new test files (tests added to existing fixtures)
- Pitcher substitutions and inherited runner attribution
- Win/Loss/Save/Blown Save decisions
- Full earned run reconstruction beyond v1-light policy

---

## 2. Test Mapping to Existing Fixtures

### 2.1 Test Distribution

| Existing Fixture | New Tests | Focus Area |
|------------------|-----------|------------|
| [`WalkoffTests.cs`](tests/DiamondSim.Tests/WalkoffTests.cs) | 5 tests | Walk-off edge cases with errors and special outcomes |
| [`EarnedRunTests.cs`](tests/DiamondSim.Tests/EarnedRunTests.cs) | 3 tests | Earned/unearned classification edge cases |
| [`InningScoreTests.cs`](tests/DiamondSim.Tests/InningScoreTests.cs) | 4 tests | Triple plays, LOB timing, skip-bottom-9th variants |

**Total:** 12 new test methods across 3 existing fixtures

---

## 3. Detailed Test Specifications

### 3.1 Walk-off Edge Cases (Add to [`WalkoffTests.cs`](tests/DiamondSim.Tests/WalkoffTests.cs))

#### Test 1: `Walkoff_Roe_Tie_BasesLoaded_PlatesOne`

**Purpose:** Verify walk-off on reach-on-error credits no RBI and marks run as unearned.

**Setup:**
```csharp
// Bottom 9th, tied 5-5, bases loaded, 1 out
var state = new GameState(
    balls: 0, strikes: 0,
    inning: 9, half: InningHalf.Bottom, outs: 1,
    onFirst: true, onSecond: true, onThird: true,
    awayScore: 5, homeScore: 5,
    awayBattingOrderIndex: 0, homeBattingOrderIndex: 4,
    offense: Team.Home, defense: Team.Away
);

var resolution = new PaResolution(
    OutsAdded: 0,
    RunsScored: 1,
    NewBases: new BaseState(OnFirst: true, OnSecond: true, OnThird: true),
    Type: PaType.ReachOnError,
    HadError: true,
    AdvanceOnError: new BaseState(OnFirst: false, OnSecond: false, OnThird: true)
);
```

**Expected Results:**
```csharp
var snapshot = result.ToTestSnapshot();
Assert.Multiple(() => {
    Assert.That(snapshot.HomeScore, Is.EqualTo(6), "Only 1 run needed to win");
    Assert.That(snapshot.AwayScore, Is.EqualTo(5), "Away score unchanged");
    Assert.That(snapshot.IsFinal, Is.True, "Game ends on walk-off");
    Assert.That(_scorekeeper.HomeLOB.Last(), Is.EqualTo(0), "Walk-off LOB always 0");
    Assert.That(snapshot.OnFirst, Is.False, "Bases cleared on walk-off");
    Assert.That(snapshot.OnSecond, Is.False, "Bases cleared on walk-off");
    Assert.That(snapshot.OnThird, Is.False, "Bases cleared on walk-off");
    // RBI and earned/unearned would be verified via BoxScore if available
});
```

**MLB Rule Reference:** Reach-on-error does not credit RBI; walk-off game ending when winning run scores

---

#### Test 2: `Walkoff_HBP_Tie_BasesLoaded_ForcedInRun`

**Purpose:** Verify walk-off on bases-loaded HBP credits exactly 1 RBI and marks run as earned.

**Setup:**
```csharp
// Bottom 9th, tied 3-3, bases loaded, 2 outs
var state = new GameState(
    balls: 0, strikes: 0,
    inning: 9, half: InningHalf.Bottom, outs: 2,
    onFirst: true, onSecond: true, onThird: true,
    awayScore: 3, homeScore: 3,
    awayBattingOrderIndex: 0, homeBattingOrderIndex: 7,
    offense: Team.Home, defense: Team.Away
);

var resolution = new PaResolution(
    OutsAdded: 0,
    RunsScored: 1,
    NewBases: new BaseState(OnFirst: true, OnSecond: true, OnThird: true),
    Type: PaType.HBP,
    HadError: false
);
```

**Expected Results:**
```csharp
var snapshot = result.ToTestSnapshot();
Assert.Multiple(() => {
    Assert.That(snapshot.HomeScore, Is.EqualTo(4), "Only 1 run needed");
    Assert.That(snapshot.AwayScore, Is.EqualTo(3));
    Assert.That(snapshot.IsFinal, Is.True);
    Assert.That(_scorekeeper.HomeLOB.Last(), Is.EqualTo(0));
    Assert.That(snapshot.OnFirst, Is.False, "Bases cleared");
    Assert.That(snapshot.OnSecond, Is.False, "Bases cleared");
    Assert.That(snapshot.OnThird, Is.False, "Bases cleared");
    Assert.That(result.HomeEarnedRuns, Is.GreaterThan(0), "Run is earned (no error)");
    // RBI assertion: Bases-loaded HBP credits exactly 1 RBI
    // Verify via BoxScore: _scorekeeper.BoxScore.HomeBatters[7].RBI == 1
});
```

**MLB Rule Reference:** Bases-loaded HBP credits 1 RBI (forced run)

---

#### Test 3: `Walkoff_SacFly_WithError_CreditsRbiButUnearned`

**Purpose:** Verify walk-off sacrifice fly credits RBI even if error enabled the score.

**Setup:**
```csharp
// Bottom 9th, tied 2-2, runner on 3rd, 1 out
var state = new GameState(
    balls: 0, strikes: 0,
    inning: 9, half: InningHalf.Bottom, outs: 1,
    onFirst: false, onSecond: false, onThird: true,
    awayScore: 2, homeScore: 2,
    awayBattingOrderIndex: 0, homeBattingOrderIndex: 3,
    offense: Team.Home, defense: Team.Away
);

// Sacrifice fly with error enabling score
var resolution = new PaResolution(
    OutsAdded: 1,
    RunsScored: 1,
    NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
    Type: PaType.InPlayOut,
    Flags: new PaFlags(IsSacFly: true),
    HadError: true,
    AdvanceOnError: new BaseState(OnFirst: false, OnSecond: false, OnThird: true)
);
```

**Expected Results:**
```csharp
var snapshot = result.ToTestSnapshot();
Assert.Multiple(() => {
    Assert.That(snapshot.HomeScore, Is.EqualTo(3), "Walk-off on sac fly");
    Assert.That(snapshot.IsFinal, Is.True);
    Assert.That(_scorekeeper.HomeLOB.Last(), Is.EqualTo(0));
    Assert.That(result.HomeUnearnedRuns, Is.GreaterThan(0), "Run is unearned (error-assisted)");
    // RBI assertion: Sacrifice fly credits exactly 1 RBI even with error
    // Verify via BoxScore: _scorekeeper.BoxScore.HomeBatters[3].RBI == 1
});
```

**MLB Rule Reference:** Sacrifice fly credits RBI regardless of error; error-assisted advancement makes run unearned

---

#### Test 4: `Walkoff_Double_TrailingByOne_ClampsToTwo`

**Purpose:** Verify non-HR walk-off clamps to minimum runs needed, suppressing extra runners.

**Setup:**
```csharp
// Bottom 9th, home down 1 (4-5), runners on 2nd and 3rd, 0 outs
var state = new GameState(
    balls: 0, strikes: 0,
    inning: 9, half: InningHalf.Bottom, outs: 0,
    onFirst: false, onSecond: true, onThird: true,
    awayScore: 5, homeScore: 4,
    awayBattingOrderIndex: 0, homeBattingOrderIndex: 2,
    offense: Team.Home, defense: Team.Away
);

var resolution = new PaResolution(
    OutsAdded: 0,
    RunsScored: 2,  // Both runners would score
    NewBases: new BaseState(OnFirst: false, OnSecond: true, OnThird: false),
    Type: PaType.Double,
    HadError: false
);
```

**Expected Results:**
```csharp
var snapshot = result.ToTestSnapshot();
Assert.Multiple(() => {
    Assert.That(snapshot.HomeScore, Is.EqualTo(6), "Exactly 2 runs to win");
    Assert.That(snapshot.AwayScore, Is.EqualTo(5));
    Assert.That(snapshot.IsFinal, Is.True);
    Assert.That(_scorekeeper.HomeLOB.Last(), Is.EqualTo(0), "Walk-off LOB always 0");
    Assert.That(snapshot.OnFirst, Is.False, "Bases cleared on walk-off");
    Assert.That(snapshot.OnSecond, Is.False, "Batter not on 2nd (walk-off suppresses)");
    Assert.That(result.HomeEarnedRuns, Is.EqualTo(2), "Both runs earned");
    // RBI assertion: Walk-off double credits RBI equal to clamped runs (2)
    // Verify via BoxScore: _scorekeeper.BoxScore.HomeBatters[2].RBI == 2
});
```

**MLB Rule Reference:** Game ends when winning run scores on non-home run play; only minimum runs needed are credited

---

#### Test 5: `Walkoff_GrandSlam_TrailingByTwo_AllFourRunsCount`

**Purpose:** Verify walk-off grand slam exception - all runs count (dead ball rule).

**Setup:**
```csharp
// Bottom 9th, home down 2 (3-5), bases loaded, 1 out
var state = new GameState(
    balls: 0, strikes: 0,
    inning: 9, half: InningHalf.Bottom, outs: 1,
    onFirst: true, onSecond: true, onThird: true,
    awayScore: 5, homeScore: 3,
    awayBattingOrderIndex: 0, homeBattingOrderIndex: 6,
    offense: Team.Home, defense: Team.Away
);

var resolution = new PaResolution(
    OutsAdded: 0,
    RunsScored: 4,  // Grand slam
    NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
    Type: PaType.HomeRun,
    HadError: false
);
```

**Expected Results:**
```csharp
var snapshot = result.ToTestSnapshot();
Assert.Multiple(() => {
    Assert.That(snapshot.HomeScore, Is.EqualTo(7), "ALL 4 runs count (HR exception)");
    Assert.That(snapshot.AwayScore, Is.EqualTo(5));
    Assert.That(snapshot.IsFinal, Is.True);
    Assert.That(_scorekeeper.HomeLOB.Last(), Is.EqualTo(0));
    Assert.That(result.HomeEarnedRuns, Is.EqualTo(4), "All runs earned");
    // RBI assertion: Walk-off grand slam credits all 4 RBI (no clamping for HR)
    // Verify via BoxScore: _scorekeeper.BoxScore.HomeBatters[6].RBI == 4
});
```

**MLB Rule Reference:** Home run is dead ball; all runners must touch all bases; all runs count even in walk-off situations

---

### 3.2 Earned/Unearned Edge Cases (Add to [`EarnedRunTests.cs`](tests/DiamondSim.Tests/EarnedRunTests.cs))

#### Test 6: `Earned_MultiRun_Single_ErrorOnlyEnablesLeadRunner_V1MarksAllUnearned`

**Purpose:** Document v1-light policy: if ANY runner advances on error, ALL runs are unearned.

**Setup:**
```csharp
// Top 5th, runners on 1st and 2nd, 1 out
var state = new GameState(
    balls: 0, strikes: 0,
    inning: 5, half: InningHalf.Top, outs: 1,
    onFirst: true, onSecond: true, onThird: false,
    awayScore: 2, homeScore: 1,
    awayBattingOrderIndex: 4, homeBattingOrderIndex: 0,
    offense: Team.Away, defense: Team.Home
);

var resolution = new PaResolution(
    OutsAdded: 0,
    RunsScored: 2,  // Both runners score
    NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
    Type: PaType.Single,
    HadError: true,
    AdvanceOnError: new BaseState(OnFirst: false, OnSecond: true, OnThird: false)  // Only R2 advanced on error
);
```

**Expected Results:**
```csharp
Assert.Multiple(() => {
    Assert.That(result.AwayScore, Is.EqualTo(4), "2 runs added");
    Assert.That(result.AwayUnearnedRuns, Is.EqualTo(2), "v1 policy: any error = all unearned");
    Assert.That(result.AwayEarnedRuns, Is.EqualTo(0), "No earned runs");
    // RBI assertion: Clean single credits RBI equal to runs scored (2)
    // Verify via BoxScore: _scorekeeper.BoxScore.AwayBatters[4].RBI == 2
});
```

**Note:** This test documents the v1-light simplification. Full reconstruction is deferred.

**MLB Rule Reference:** Earned run determination (simplified v1-light implementation)

---

#### Test 7: `Unearned_ROE_MultipleRuns_AllUnearned_NoRBI`

**Purpose:** Verify ROE with multiple runners always produces all unearned runs and no RBI.

**Setup:**
```csharp
// Top 7th, runners on 2nd and 3rd, 1 out
var state = new GameState(
    balls: 0, strikes: 0,
    inning: 7, half: InningHalf.Top, outs: 1,
    onFirst: false, onSecond: true, onThird: true,
    awayScore: 3, homeScore: 4,
    awayBattingOrderIndex: 2, homeBattingOrderIndex: 0,
    offense: Team.Away, defense: Team.Home
);

var resolution = new PaResolution(
    OutsAdded: 0,
    RunsScored: 2,  // Both runners score on error
    NewBases: new BaseState(OnFirst: true, OnSecond: false, OnThird: false),
    Type: PaType.ReachOnError,
    HadError: true,
    AdvanceOnError: new BaseState(OnFirst: false, OnSecond: true, OnThird: true)
);
```

**Expected Results:**
```csharp
Assert.Multiple(() => {
    Assert.That(result.AwayScore, Is.EqualTo(5), "2 runs added");
    Assert.That(result.AwayUnearnedRuns, Is.EqualTo(2), "ROE = all unearned");
    Assert.That(result.AwayEarnedRuns, Is.EqualTo(0), "No earned runs");
    // RBI assertion: ROE never credits RBI regardless of runs scored
    // Verify via BoxScore: _scorekeeper.BoxScore.AwayBatters[2].RBI == 0
});
```

**MLB Rule Reference:** Reach-on-error does not credit RBI; error-caused runs are unearned

---

#### Test 8: `Earned_SacFly_WithError_RunUnearnedButRbiCredited`

**Purpose:** Verify sacrifice fly credits RBI even when error makes run unearned.

**Setup:**
```csharp
// Bottom 6th, runner on 3rd, 1 out
var state = new GameState(
    balls: 0, strikes: 0,
    inning: 6, half: InningHalf.Bottom, outs: 1,
    onFirst: false, onSecond: false, onThird: true,
    awayScore: 3, homeScore: 2,
    awayBattingOrderIndex: 0, homeBattingOrderIndex: 5,
    offense: Team.Home, defense: Team.Away
);

var resolution = new PaResolution(
    OutsAdded: 1,
    RunsScored: 1,
    NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
    Type: PaType.InPlayOut,
    Flags: new PaFlags(IsSacFly: true),
    HadError: true,
    AdvanceOnError: new BaseState(OnFirst: false, OnSecond: false, OnThird: true)
);
```

**Expected Results:**
```csharp
Assert.Multiple(() => {
    Assert.That(result.HomeScore, Is.EqualTo(3), "Run scores");
    Assert.That(result.HomeUnearnedRuns, Is.EqualTo(1), "Run is unearned (error-assisted)");
    Assert.That(result.HomeEarnedRuns, Is.EqualTo(0), "No earned runs");
    // RBI assertion: Sacrifice fly credits exactly 1 RBI even when error makes run unearned
    // Verify via BoxScore: _scorekeeper.BoxScore.HomeBatters[5].RBI == 1
});
```

**MLB Rule Reference:** Sacrifice fly credits RBI regardless of error involvement

---

### 3.3 Triple Play & LOB Timing (Add to [`InningScoreTests.cs`](tests/DiamondSim.Tests/InningScoreTests.cs))

#### Test 9: `TriplePlay_OutsAdded3_EndsHalf_LOBFromInstantOfThirdOut`

**Purpose:** Verify triple play ends half immediately with correct LOB calculation.

**Setup:**
```csharp
// Top 4th, bases loaded, 0 outs
var state = new GameState(
    balls: 0, strikes: 0,
    inning: 4, half: InningHalf.Top, outs: 0,
    onFirst: true, onSecond: true, onThird: true,
    awayScore: 2, homeScore: 1,
    awayBattingOrderIndex: 6, homeBattingOrderIndex: 0,
    offense: Team.Away, defense: Team.Home
);

var resolution = new PaResolution(
    OutsAdded: 3,  // Triple play
    RunsScored: 0,
    NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
    Type: PaType.InPlayOut,
    HadError: false
);
```

**Expected Results:**
```csharp
var snapshot = result.ToTestSnapshot();
Assert.Multiple(() => {
    Assert.That(snapshot.AwayScore, Is.EqualTo(2), "No runs");
    Assert.That(snapshot.Outs, Is.EqualTo(0), "Outs reset");
    Assert.That(snapshot.OnFirst, Is.False, "Bases cleared");
    Assert.That(snapshot.OnSecond, Is.False, "Bases cleared");
    Assert.That(snapshot.OnThird, Is.False, "Bases cleared");
    Assert.That(snapshot.Half, Is.EqualTo(InningHalf.Bottom), "Transition to bottom");
    Assert.That(snapshot.Inning, Is.EqualTo(4), "Still 4th inning");
    Assert.That(_scorekeeper.AwayLOB[^1], Is.EqualTo(3), "LOB = 3 (bases loaded at 3rd out)");
    // Line score flush verification: ensure runs recorded for away team in 4th inning
    Assert.That(_scorekeeper.LineScore.GetInningRuns(Team.Away, 4), Is.EqualTo(0),
        "Line score should show 0 runs for away in 4th (triple play, no runs scored)");
});
```

**MLB Rule Reference:** Three outs end half-inning; LOB counted at moment of final out

---

#### Test 10: `NonWalkoffHomeRun_NotEnoughToWin_GameContinues`

**Purpose:** Verify home run in bottom 9th that doesn't win the game continues play.

**Setup:**
```csharp
// Bottom 9th, home down 3 (2-5), runner on 1st, 1 out
var state = new GameState(
    balls: 0, strikes: 0,
    inning: 9, half: InningHalf.Bottom, outs: 1,
    onFirst: true, onSecond: false, onThird: false,
    awayScore: 5, homeScore: 2,
    awayBattingOrderIndex: 0, homeBattingOrderIndex: 4,
    offense: Team.Home, defense: Team.Away
);

var resolution = new PaResolution(
    OutsAdded: 0,
    RunsScored: 2,  // Two-run HR, not enough to win
    NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: false),
    Type: PaType.HomeRun,
    HadError: false
);
```

**Expected Results:**
```csharp
var snapshot = result.ToTestSnapshot();
Assert.Multiple(() => {
    Assert.That(snapshot.HomeScore, Is.EqualTo(4), "All runs count for HR");
    Assert.That(snapshot.AwayScore, Is.EqualTo(5), "Still leading");
    Assert.That(snapshot.IsFinal, Is.False, "Game continues");
    Assert.That(snapshot.Outs, Is.EqualTo(1), "Outs unchanged");
    Assert.That(snapshot.OnFirst, Is.False, "Bases cleared");
    Assert.That(snapshot.Half, Is.EqualTo(InningHalf.Bottom), "Still bottom 9th");
});
```

**MLB Rule Reference:** Home run credits all runs; game continues if home team does not take the lead

---

#### Test 11: `SkipBottom9th_WithRunnersOnAtT9End_BasesDoNotAffectFinal`

**Purpose:** Verify skip-bottom-9th logic is independent of base state at end of top 9th.

**Setup:**
```csharp
// Top 9th, 2 outs, runners on 1st and 2nd, home leading 6-4
var state = new GameState(
    balls: 0, strikes: 0,
    inning: 9, half: InningHalf.Top, outs: 2,
    onFirst: true, onSecond: true, onThird: false,
    awayScore: 4, homeScore: 6,
    awayBattingOrderIndex: 5, homeBattingOrderIndex: 0,
    offense: Team.Away, defense: Team.Home
);

// Need to record previous innings first
for (int i = 0; i < 9; i++) {
    _scorekeeper.LineScore.RecordInning(Team.Away, 0);
    if (i < 8) {
        _scorekeeper.LineScore.RecordInning(Team.Home, 0);
    }
}

// Final out (strikeout)
var resolution = new PaResolution(
    OutsAdded: 1,
    RunsScored: 0,
    NewBases: new BaseState(OnFirst: true, OnSecond: true, OnThird: false),
    Type: PaType.K,
    HadError: false
);
```

**Expected Results:**
```csharp
var snapshot = result.ToTestSnapshot();
Assert.Multiple(() => {
    Assert.That(snapshot.IsFinal, Is.True, "Game over (home leads)");
    Assert.That(_scorekeeper.AwayLOB[^1], Is.EqualTo(2), "Away LOB = 2");
    Assert.That(_scorekeeper.LineScore.GetInningDisplay(Team.Home, 9), Is.EqualTo("X"), "Home 9th shows X");
    Assert.That(snapshot.HomeScore, Is.EqualTo(6), "Final: Home 6");
    Assert.That(snapshot.AwayScore, Is.EqualTo(4), "Final: Away 4");
});
```

**MLB Rule Reference:** Home team does not bat in bottom of 9th if leading after top half

---

#### Test 12: `SkipBottom9th_HomeLeadsAfterT9_WithRunnerOnThird`

**Purpose:** Verify skip-bottom-9th with runner on 3rd at end of top 9th.

**Setup:**
```csharp
// Top 9th, 2 outs, runner on 3rd, home leading 5-3
var state = new GameState(
    balls: 0, strikes: 0,
    inning: 9, half: InningHalf.Top, outs: 2,
    onFirst: false, onSecond: false, onThird: true,
    awayScore: 3, homeScore: 5,
    awayBattingOrderIndex: 8, homeBattingOrderIndex: 0,
    offense: Team.Away, defense: Team.Home
);

// Need to record previous innings first
for (int i = 0; i < 9; i++) {
    _scorekeeper.LineScore.RecordInning(Team.Away, 0);
    if (i < 8) {
        _scorekeeper.LineScore.RecordInning(Team.Home, 0);
    }
}

// Final out
var resolution = new PaResolution(
    OutsAdded: 1,
    RunsScored: 0,
    NewBases: new BaseState(OnFirst: false, OnSecond: false, OnThird: true),
    Type: PaType.InPlayOut,
    HadError: false
);
```

**Expected Results:**
```csharp
var snapshot = result.ToTestSnapshot();
Assert.Multiple(() => {
    Assert.That(snapshot.IsFinal, Is.True, "Game ends");
    Assert.That(_scorekeeper.AwayLOB[^1], Is.EqualTo(1), "Away LOB = 1 (R3)");
    Assert.That(_scorekeeper.LineScore.GetInningDisplay(Team.Home, 9), Is.EqualTo("X"));
    Assert.That(snapshot.HomeScore, Is.EqualTo(5));
    Assert.That(snapshot.AwayScore, Is.EqualTo(3));
});
```

**MLB Rule Reference:** Home team does not bat in bottom of 9th if leading; base state at end of top 9th does not affect this rule

---

## 4. Implementation Plan

### 4.1 Phase 1: Walk-off Edge Tests (2-3 hours)

**Tasks:**
1. Add 5 tests to [`WalkoffTests.cs`](tests/DiamondSim.Tests/WalkoffTests.cs)
2. Verify walk-off clamping vs HR exception in edge scenarios
3. Validate RBI and earned/unearned in walk-off situations

**Validation:** All 5 new tests pass

### 4.2 Phase 2: Earned/Unearned Edge Tests (1-2 hours)

**Tasks:**
1. Add 3 tests to [`EarnedRunTests.cs`](tests/DiamondSim.Tests/EarnedRunTests.cs)
2. Document v1-light policy behavior with mixed errors
3. Verify sacrifice fly RBI with error-caused unearned run

**Validation:** All 3 new tests pass

### 4.3 Phase 3: Inning Score Edge Tests (2-3 hours)

**Tasks:**
1. Add 4 tests to [`InningScoreTests.cs`](tests/DiamondSim.Tests/InningScoreTests.cs)
2. Verify triple play LOB timing
3. Test non-winning HR and skip-bottom-9th variants

**Validation:** All 4 new tests pass

### 4.4 Total Estimated Effort

**5-8 hours** of test development time

---

## 5. Acceptance Criteria

### 5.1 Test Coverage

| Test Fixture | New Tests | Coverage |
|--------------|-----------|----------|
| [`WalkoffTests.cs`](tests/DiamondSim.Tests/WalkoffTests.cs) | 5 | ROE, HBP, SF with error, double clamp, grand slam |
| [`EarnedRunTests.cs`](tests/DiamondSim.Tests/EarnedRunTests.cs) | 3 | Mixed error, multi-run ROE, SF with error |
| [`InningScoreTests.cs`](tests/DiamondSim.Tests/InningScoreTests.cs) | 4 | Triple play, non-winning HR, skip-B9 variants |
| **Total** | **12** | **Comprehensive edge case coverage** |

### 5.2 Quality Standards

All tests must:
- ✅ Use [`TestSnapshot`](src/DiamondSim/TestSnapshot.cs) for state assertions
- ✅ Include explicit assertions for: scores, RBI, earned/unearned, LOB, `IsFinal`
- ✅ Document MLB rule references in comments
- ✅ Use deterministic setups (no randomness)
- ✅ Pass consistently (100% pass rate)
- ✅ Follow existing test fixture patterns and NUnit conventions

### 5.3 Validation Checklist

- [ ] All 12 tests implemented in appropriate fixtures
- [ ] All tests pass on first run
- [ ] No `GameState.Equals()` usage
- [ ] Clear test names describing scenario
- [ ] Comprehensive assertions (not just final score)
- [ ] MLB rule references documented
- [ ] Code review completed
- [ ] Integration with existing test suite verified

---

## 6. Dependencies & Risks

### 6.1 Dependencies

| Dependency | Status | Impact |
|------------|--------|--------|
| PRD-20251025-01 (Core Rules Corrections) | ✅ Complete | Provides walk-off clamping, RBI rules, earned/unearned logic |
| [`TestSnapshot`](src/DiamondSim/TestSnapshot.cs) | ✅ Exists | Required for safe state assertions |
| [`InningScorekeeper`](src/DiamondSim/InningScorekeeper.cs) | ✅ Exists | Applies PA resolutions |
| [`PaResolution`](src/DiamondSim/PaResolution.cs) | ✅ Extended | Includes error tracking fields |
| Existing test fixtures | ✅ Exist | Tests added to existing files |

### 6.2 Risks

| Risk | Probability | Impact | Mitigation |
