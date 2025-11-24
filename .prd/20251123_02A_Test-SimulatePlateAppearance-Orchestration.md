# PRD: Unit Tests for SimulatePlateAppearance Orchestration Logic

**Status:** 🔴 Critical Gap - MUST BE DONE NEXT
**Priority:** P0
**Created:** 2025-11-23
**Parent PRD:** 20251123_02_Refactor-GameSimulator-Return-Object.md
**Estimated Effort:** 2-3 hours

---

## 1. Executive Summary

**Critical Test Coverage Gap Identified:** The `SimulatePlateAppearanceV2()` method in `GameSimulator.cs` has ZERO direct unit tests. This orchestration method is the glue that connects all simulation components, yet we only test it indirectly through full game integration tests.

**Risk:** Orchestration bugs (wrong batter selected, incorrect component call sequence, malformed PlayLogEntry) could slip through and only manifest in specific game scenarios.

**Solution:** Create comprehensive unit tests for the orchestration logic using the Testable Subclass pattern.

---

## 2. Problem Statement

### What SimulatePlateAppearanceV2() Does
This method orchestrates a single plate appearance by:
1. Getting current batter from lineup (based on game state)
2. Getting current pitcher (based on game state)
3. Calling `AtBatSimulator.SimulateAtBat()`
4. Conditionally calling `BallInPlayResolver` (if BIP)
5. Calling `BaseRunnerAdvancement.Resolve()`
6. Calling `InningScorekeeper.ApplyPlateAppearance()`
7. Constructing `PlayLogEntry` with all data
8. Adding entry to playLogEntries list
9. Returning updated GameState

### Current Test Coverage
- ✅ **Component tests:** AtBatSimulator, BallInPlayResolver, BaseRunnerAdvancement all tested
- ✅ **State management:** InningScorekeeper.ApplyPlateAppearance thoroughly tested
- ✅ **Integration tests:** Full game simulations via RunGameV2()
- ❌ **Orchestration logic:** ZERO direct tests

### Why This Matters
Without orchestration tests, we cannot verify:
- Correct batter/pitcher selection logic
- Correct component call sequence
- Correct data flow between components
- Correct PlayLogEntry construction
- Edge cases in orchestration (e.g., wrong team's batter selected)

---

## 3. Scope

### In Scope
- Unit tests for `SimulatePlateAppearanceV2()` orchestration
- Batter/pitcher selection verification
- Component integration verification
- PlayLogEntry construction verification
- State progression verification

### Out of Scope
- Component behavior testing (already covered)
- Full game simulation (already covered by integration tests)
- String formatting (deferred to GameReportFormatterTests)
- `SimulatePlateAppearance()` V1 method (legacy, will be deprecated)

---

## 4. Test Requirements

### Test File Location
**Path:** `tests/DiamondSim.Tests/GameLoop/SimulatePlateAppearanceTests.cs`

### Test Class Structure
```csharp
[TestFixture]
public class SimulatePlateAppearanceTests {
    // 6 required tests
    // Helper methods
    // Testable subclass for exposing protected methods
}
```

---

## 5. Required Tests (TDD Approach)

### Test 1: Batter Selection
**Name:** `SimulatePlateAppearanceV2_SelectsCorrectBatter_FromOffensiveTeamLineup`

**Scenario:**
- Top of 1st inning
- Away team batting (offense = Team.Away)
- AwayBattingOrderIndex = 3

**Test Steps:**
1. Create GameState with offense=Away, AwayBattingOrderIndex=3
2. Create simulator with known lineups
3. Call SimulatePlateAppearanceV2()
4. Verify PlayLogEntry.BatterName matches away lineup[3].Name

**Verification:**
```csharp
Assert.That(playLogEntry.BatterName, Is.EqualTo(awayLineup[3].Name),
    "Should select batter from away lineup at index 3");
```

**Implementation Approach:**
- Use testable subclass that captures batter selection
- Verify correct lineup (away vs home) is used
- Verify correct index is used

---

### Test 2: Pitcher Selection
**Name:** `SimulatePlateAppearanceV2_SelectsCorrectPitcher_FromDefensiveTeam`

**Scenario:**
- Top of 1st inning
- Home team pitching (defense = Team.Home)

**Test Steps:**
1. Create GameState with defense=Home
2. Create simulator with known pitchers
3. Call SimulatePlateAppearanceV2()
4. Verify PlayLogEntry.PitchingTeamName = "Home"

**Verification:**
```csharp
Assert.That(playLogEntry.PitchingTeamName, Is.EqualTo("Home"),
    "Should use home team name when home is defense");
```

**Implementation Approach:**
- Verify correct pitcher (home vs away) is selected based on defense
- Verify pitching team name is correct

---

### Test 3: Strikeout Flow (No Ball-In-Play)
**Name:** `SimulatePlateAppearanceV2_Strikeout_SkipsBallInPlayResolution`

**Scenario:**
- AtBatSimulator returns Terminal=Strikeout
- No ball in play

**Test Steps:**
1. Create controlled game state
2. Mock/spy on AtBatSimulator to return Strikeout
3. Call SimulatePlateAppearanceV2()
4. Verify BallInPlayResolver was NOT called
5. Verify BaseRunnerAdvancement.Resolve() was called with bipOutcome=null
6. Verify PlayLogEntry.Resolution.Tag = OutcomeTag.K

**Verification:**
```csharp
Assert.That(playLogEntry.Resolution.Tag, Is.EqualTo(OutcomeTag.K));
Assert.That(playLogEntry.Resolution.Type, Is.EqualTo(PaType.K));
// Verify BIP resolver was NOT called (implementation-specific)
```

**Implementation Approach:**
- Use testable subclass or dependency injection
- Verify component call sequence
- Verify resolution data is correct

---

### Test 4: Ball-In-Play Flow (Single)
**Name:** `SimulatePlateAppearanceV2_BallInPlay_CallsBallInPlayResolver`

**Scenario:**
- AtBatSimulator returns Terminal=BallInPlay
- BallInPlayResolver returns Single

**Test Steps:**
1. Create controlled game state
2. Mock/spy on components to return BallInPlay → Single
3. Call SimulatePlateAppearanceV2()
4. Verify BallInPlayResolver WAS called
5. Verify BaseRunnerAdvancement.Resolve() was called with bipOutcome=Single
6. Verify PlayLogEntry.Resolution.Tag = OutcomeTag.Single

**Verification:**
```csharp
Assert.That(playLogEntry.Resolution.Tag, Is.EqualTo(OutcomeTag.Single));
Assert.That(playLogEntry.Resolution.Type, Is.EqualTo(PaType.Single));
// Verify BIP resolver WAS called (implementation-specific)
```

**Implementation Approach:**
- Verify full BIP flow: AtBat → BIP → Resolve → Apply
- Verify resolution data flows correctly

---

### Test 5: PlayLogEntry Construction
**Name:** `SimulatePlateAppearanceV2_ConstructsPlayLogEntry_WithAllCorrectFields`

**Scenario:**
- Any plate appearance with known inputs

**Test Steps:**
1. Create controlled GameState (inning=5, half=Bottom, etc.)
2. Create known batter ("Test Batter")
3. Create known pitcher team ("TestTeam")
4. Call SimulatePlateAppearanceV2()
5. Verify ALL PlayLogEntry fields

**Verification:**
```csharp
var entry = playLogEntries[0];
Assert.That(entry.Inning, Is.EqualTo(5), "Inning from state");
Assert.That(entry.Half, Is.EqualTo(InningHalf.Bottom), "Half from state");
Assert.That(entry.BatterName, Is.EqualTo("Test Batter"), "Batter name from current batter");
Assert.That(entry.PitchingTeamName, Is.EqualTo("TestTeam"), "Pitching team from defense");
Assert.That(entry.Resolution, Is.Not.Null, "Resolution from BaseRunnerAdvancement");
Assert.That(entry.IsWalkoff, Is.EqualTo(expectedWalkoff), "IsWalkoff from ApplyResult");
Assert.That(entry.OutsAfter, Is.EqualTo(expectedOuts), "OutsAfter from ApplyResult");
```

**Implementation Approach:**
- Use predictable inputs
- Verify each field individually
- Test both walkoff and non-walkoff scenarios

---

### Test 6: State Progression
**Name:** `SimulatePlateAppearanceV2_ReturnsUpdatedState_AndAddsPlayLogEntry`

**Scenario:**
- Any plate appearance

**Test Steps:**
1. Create initial GameState
2. Create empty playLogEntries list
3. Call SimulatePlateAppearanceV2()
4. Verify returned state is updated (from ApplyResult)
5. Verify playLogEntries list grew by 1

**Verification:**
```csharp
// Before
int initialCount = playLogEntries.Count;
var initialState = state;

// Act
var newState = simulator.SimulatePlateAppearanceV2(state, playLogEntries);

// Assert
Assert.That(playLogEntries.Count, Is.EqualTo(initialCount + 1),
    "Should add exactly one PlayLogEntry");
Assert.That(newState, Is.Not.EqualTo(initialState),
    "Should return updated state");
Assert.That(newState.IsFinal, Is.EqualTo(expectedFinal),
    "State.IsFinal should match ApplyResult");
```

**Implementation Approach:**
- Verify list mutation
- Verify state progression
- Test both mid-inning and inning-ending scenarios

---

## 6. Implementation Strategy

### Approach: Testable Subclass Pattern

**Why:** `SimulatePlateAppearanceV2()` is `protected virtual`, allowing test subclasses to override or spy on it.

**Pattern:**
```csharp
internal class TestableGameSimulatorForOrchestration : GameSimulator {
    public Batter? LastBatterSelected { get; private set; }
    public Pitcher? LastPitcherSelected { get; private set; }
    public bool BallInPlayResolverCalled { get; private set; }

    public TestableGameSimulatorForOrchestration(string home, string away, int seed)
        : base(home, away, seed) {
    }

    // Expose protected methods for testing
    public Batter GetBatterForTesting(GameState state) => GetCurrentBatter(state);
    public Pitcher GetPitcherForTesting(GameState state) => GetCurrentPitcher(state);

    // Override to capture calls (if needed)
    protected override GameState SimulatePlateAppearanceV2(GameState state, List<PlayLogEntry> playLogEntries) {
        LastBatterSelected = GetCurrentBatter(state);
        LastPitcherSelected = GetCurrentPitcher(state);
        return base.SimulatePlateAppearanceV2(state, playLogEntries);
    }
}
```

### Alternative: Make Methods Internal
- Change `GetCurrentBatter()` and `GetCurrentPitcher()` from `private` to `internal`
- Add `[assembly: InternalsVisibleTo("DiamondSim.Tests")]`
- Test helper methods directly

**Recommendation:** Use Testable Subclass pattern (less invasive, doesn't expose internals)

---

## 7. Test Data Helpers

### Helper: Create Controlled GameState
```csharp
private GameState CreateTestGameState(
    int inning = 1,
    InningHalf half = InningHalf.Top,
    Team offense = Team.Away,
    int awayBattingIndex = 0,
    int homeBattingIndex = 0) {

    return new GameState(
        balls: 0, strikes: 0,
        inning: inning, half: half, outs: 0,
        onFirst: false, onSecond: false, onThird: false,
        awayScore: 0, homeScore: 0,
        awayBattingOrderIndex: awayBattingIndex,
        homeBattingOrderIndex: homeBattingIndex,
        offense: offense,
        defense: offense == Team.Away ? Team.Home : Team.Away,
        isFinal: false
    );
}
```

### Helper: Create Known Lineups
```csharp
private (List<Batter> home, List<Batter> away) CreateKnownLineups() {
    var home = Enumerable.Range(1, 9)
        .Select(i => new Batter($"Home {i}", BatterRatings.Average))
        .ToList();

    var away = Enumerable.Range(1, 9)
        .Select(i => new Batter($"Away {i}", BatterRatings.Average))
        .ToList();

    return (home, away);
}
```

---

## 8. Acceptance Criteria

### Must Have
- ✅ All 6 tests created
- ✅ All 6 tests passing
- ✅ All existing 287 tests still passing
- ✅ Test coverage for orchestration verified
- ✅ PlayLogEntry construction validated
- ✅ Component integration verified

### Should Have
- ✅ Clear, descriptive test names
- ✅ Comprehensive assertions
- ✅ Reusable helper methods
- ✅ Good test documentation

### Nice to Have
- Test for edge cases (e.g., batting order wraps from 8 to 0)
- Test for both teams (away and home batting)
- Performance benchmarks

---

## 9. Implementation Steps (TDD)

### Step 1: Create Test File
1. Create `tests/DiamondSim.Tests/GameLoop/SimulatePlateAppearanceTests.cs`
2. Add test fixture and using statements
3. Create testable subclass

### Step 2: Implement Tests (One at a Time)
1. **Test 1:** Batter selection (RED → GREEN → REFACTOR)
2. **Test 2:** Pitcher selection (RED → GREEN → REFACTOR)
3. **Test 3:** Strikeout flow (RED → GREEN → REFACTOR)
4. **Test 4:** Ball-in-play flow (RED → GREEN → REFACTOR)
5. **Test 5:** PlayLogEntry construction (RED → GREEN → REFACTOR)
6. **Test 6:** State progression (RED → GREEN → REFACTOR)

### Step 3: Verify & Refactor
1. Run all tests (should be 293 total: 287 existing + 6 new)
2. Refactor for clarity and reusability
3. Extract common helpers
4. Update this PRD status to ✅ Complete

---

## 10. Success Metrics

### Test Quality
- Each test focuses on ONE aspect of orchestration
- Tests are independent (can run in any order)
- Tests use predictable, controlled inputs
- Assertions are specific and meaningful

### Code Quality
- No production code changes needed (tests verify existing behavior)
- Helper methods are reusable
- Test code is clean and maintainable

### Coverage
- 100% coverage of orchestration logic
- All code paths tested (BIP vs non-BIP)
- Both teams tested (away and home)

---

## 11. Risk Mitigation

### Risk: Tests are too coupled to implementation
**Mitigation:** Focus on behavior, not implementation details. Test WHAT happens, not HOW.

### Risk: Tests are brittle
**Mitigation:** Use helper methods for test data creation. Avoid magic numbers.

### Risk: Tests don't catch real bugs
**Mitigation:** Include edge cases (batting order wrap, walk-off scenarios, etc.)

---

## 12. Future Enhancements (Out of Scope)

After these tests are in place, we can:
1. Refactor orchestration logic with confidence
2. Add pitcher substitution logic (knowing tests will catch breaks)
3. Add defensive substitutions
4. Optimize component calls

---

## 13. Related Documents

- Parent PRD: `.prd/20251123_02_Refactor-GameSimulator-Return-Object.md`
- Source Code: `src/DiamondSim/GameSimulator.cs` (lines 167-215)
- Existing Tests: `tests/DiamondSim.Tests/GameLoop/GameSimulatorV2Tests.cs`

---

## 14. Checklist

- [ ] Create SimulatePlateAppearanceTests.cs
- [ ] Implement Test 1: Batter selection
- [ ] Implement Test 2: Pitcher selection
- [ ] Implement Test 3: Strikeout flow
- [ ] Implement Test 4: Ball-in-play flow
- [ ] Implement Test 5: PlayLogEntry construction
- [ ] Implement Test 6: State progression
- [ ] All 293 tests passing (287 + 6 new)
- [ ] Code review
- [ ] Update PRD status to Complete

---

**Priority Justification:** This is P0 because the orchestration layer is the foundation of game simulation. Without these tests, we have a significant gap in our test pyramid that could hide critical bugs.

**Next Action:** Create SimulatePlateAppearanceTests.cs and implement Test 1.
