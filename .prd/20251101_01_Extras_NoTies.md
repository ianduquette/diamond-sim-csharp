+++
id = "PRD-EXTRAS-NO-TIES-V1"
title = "Extra Innings & No-Ties — Product Requirements"
context_type = "prd"
scope = "Defines requirements for implementing unlimited extra innings and walk-off functionality, eliminating tie games"
target_audience = ["developers", "testers", "product-owners"]
status = "active"
created_date = "2025-11-01"
updated_date = "2025-11-01"
version = "1.0"
tags = ["prd", "extra-innings", "walk-off", "game-rules", "tdd"]
related_context = [
    ".prp/20251101_01_ExtraInnings_NoTies_TDD.prp",
    "tests/DiamondSim.Tests/Scoring/GameLoop_ExtrasAndWalkOffs_Tests.cs"
]
depends_on = [
    "20251024_04_Inning_Scoring_PRD",
    "Game_Loop_PRD_Latest"
]
+++

# Extra Innings & No-Ties — Product Requirements

**Document ID:** PRD-EXTRAS-NO-TIES-V1
**Owner:** DiamondSim Development Team
**Status:** Active
**Created:** 2025-11-01
**Implementation Approach:** Test-Driven Development (TDD)

---

## 1. Executive Summary

This PRD defines the requirements for removing tie games from DiamondSim and implementing unlimited extra innings with proper walk-off functionality. Currently, games end after 9 innings regardless of score, potentially resulting in ties. This enhancement ensures every game produces a winner by continuing play into extra innings (10th, 11th, etc.) until one team has an unassailable lead. Walk-off scenarios—where the home team wins in the bottom of the 9th or later innings—will end the game immediately when the winning run scores. This change aligns the simulation with standard baseball rules and provides more realistic game outcomes.

---

## 2. User Stories & Acceptance Criteria

### User Story 1: No Tie Games
**As a** simulation user
**I want** every game to produce a winner
**So that** results match real baseball where ties don't exist in regulation or extra innings

**Acceptance Criteria:**
- AC1: No terminal tie state exists in the codebase (enums, return values, or game states)
- AC2: Any scenario that previously resulted in a tie now continues to extra innings
- AC3: Documentation and error messages contain no references to tie games

### User Story 2: Walk-Off in Bottom 9th
**As a** simulation user
**I want** games to end immediately when the home team takes the lead in the bottom of the 9th
**So that** the simulation accurately reflects walk-off scenarios

**Acceptance Criteria:**
- AC4: When home team trails by 1 entering bottom 9th, a single that scores the tying and winning runs ends the game immediately
- AC5: Only the necessary runs to win are credited (no additional runner advancement beyond the winning run, except for home runs)
- AC6: Game state shows `IsFinal=true`, `Winner=Home`, `FinalizationMethod=WalkOff`
- AC7: Example: Home trails 3-4 entering bottom 9th; runner on 2nd; single scores 1 run to tie 4-4, then winning run scores to make it 5-4; game ends immediately without advancing other runners

### User Story 3: Extra Innings Continuation
**As a** simulation user
**I want** tied games after 9 innings to continue into extra innings
**So that** a winner is determined through extended play

**Acceptance Criteria:**
- AC8: If score is tied after bottom of 9th inning, game proceeds to top of 10th inning
- AC9: Extra innings continue alternating (top/bottom) until finalization occurs
- AC10: Inning counter increments correctly (10, 11, 12, ... N)
- AC11: Example: Game tied 2-2 after 9 innings; top 10th begins with Away team batting

### User Story 4: Walk-Off in Extra Innings
**As a** simulation user
**I want** walk-off rules to apply in any bottom half of extra innings
**So that** games can end dramatically in the 10th, 11th, or later innings

**Acceptance Criteria:**
- AC12: Walk-off detection works in bottom of any inning >= 10
- AC13: Game ends immediately when home team takes lead in bottom of extra inning
- AC14: Example: Tied 5-5 entering bottom 12th; home run gives home team 6-5 lead; game ends with `FinalizationMethod=WalkOff`

### User Story 5: Dynamic Line Score Display
**As a** simulation user
**I want** line scores to show all innings played
**So that** I can see the complete scoring progression regardless of game length

**Acceptance Criteria:**
- AC15: Line score columns expand dynamically to show all innings (e.g., 12 columns for a 12-inning game)
- AC16: Both Away and Home line scores have equal column counts
- AC17: Column totals match final scores
- AC18: Formatting remains readable with variable column counts

### User Story 6: Deterministic Outcomes
**As a** simulation developer
**I want** games with the same seed to produce identical results
**So that** testing and debugging are reliable

**Acceptance Criteria:**
- AC19: Same seed produces same winner, final score, and line score across runs
- AC20: Extra innings progression is deterministic under seeded randomness
- AC21: Walk-off scenarios occur at the same point with the same seed

### User Story 7: Completion Logging
**As a** simulation operator
**I want** clear logging of how games end
**So that** I can analyze game completion patterns

**Acceptance Criteria:**
- AC22: Log entry shows inning and half when game ends
- AC23: Log entry shows finalization method: `WalkOff` or `CompletedInning`
- AC24: Example log: "Game final: Bottom 9, Method: WalkOff, Winner: Home, Score: 5-4"

---

## 3. Rules of Play

### 3.1 Regulation Play (Innings 1-9)
- Games begin in the 1st inning and proceed through the 9th inning
- Each inning has two halves: Top (Away team bats) and Bottom (Home team bats)
- After the bottom of the 9th inning:
  - If Away leads: Game is final, Away wins (`FinalizationMethod=CompletedInning`)
  - If Home leads: Game is final, Home wins (`FinalizationMethod=CompletedInning`)
  - If tied: Game proceeds to extra innings

### 3.2 Extra Innings (Inning >= 10)
- Extra innings begin with the top of the 10th inning
- Play continues with alternating halves (Top 10, Bottom 10, Top 11, Bottom 11, etc.)
- After each completed inning in extras:
  - If Away leads: Game is final, Away wins (`FinalizationMethod=CompletedInning`)
  - If Home leads: Game is final, Home wins (`FinalizationMethod=CompletedInning`)
  - If tied: Next inning begins
- **No ghost runner rule:** Innings begin with bases empty (ghost runner on 2nd is explicitly out of scope)

### 3.3 Walk-Off Rules
- Walk-off can occur in bottom of 9th or any bottom half of extra innings
- Game ends **immediately** when the home team takes an unassailable lead
- **Runner advancement truncation:** When the winning run scores, no additional runners advance beyond what is necessary for the win
- **Home run exception:** On a walk-off home run, all runners on base plus the batter score (standard home run rules apply)
- Walk-off detection occurs after applying run deltas from each play result

### 3.4 Finalization
A game reaches final state when:
1. **Walk-off occurs:** Home team takes lead in bottom of 9th or later (`FinalizationMethod=WalkOff`)
2. **Completed inning with leader:** Any completed inning (9th or later) ends with unequal scores (`FinalizationMethod=CompletedInning`)

### 3.5 Inning Numbering
- Innings are numbered sequentially: 1, 2, 3, ..., 9, 10, 11, 12, ..., N
- No special notation for extra innings (they are simply numbered 10+)
- Line score displays all innings played

---

## 4. Edge Cases

### 4.1 Runner Advancement Truncation on Walk-Off
**Scenario:** Bottom 9th, Home trails 4-5, runners on 1st and 2nd, 1 out. Batter hits double.
- **Expected:** Runner from 2nd scores (tie 5-5), runner from 1st scores (win 6-5), game ends
- **Truncation:** Batter does NOT advance to 2nd base; game ends when winning run crosses plate
- **Exception:** If this were a home run, all runners + batter would score

### 4.2 Home Run Walk-Off Exception
**Scenario:** Bottom 10th, tied 3-3, runner on 1st, batter hits home run.
- **Expected:** Runner from 1st scores (4-3), batter scores (5-3), game ends
- **No truncation:** Home runs always score all runners plus batter, even in walk-off situations

### 4.3 Error/Wild Throw/Balk Anomalies
**Scenario:** Bottom 9th, Home trails 2-3, runner on 3rd, wild pitch allows runner to score.
- **Expected:** Runner scores (tie 3-3), game continues if no other runners score
- **Walk-off:** If another runner subsequently scores in the same half-inning to make it 4-3, game ends immediately

### 4.4 Determinism Note
- All walk-off and extra inning scenarios must be deterministic under seeded randomness
- Same seed must produce same sequence of plays, same walk-off timing, same final score
- Test suite must verify determinism across multiple runs with identical seeds

### 4.5 Multiple Runs in Walk-Off Play
**Scenario:** Bottom 9th, Home trails 2-4, bases loaded, batter hits double.
- **Expected:** Runs score until Home takes lead (3rd run makes it 5-4), game ends
- **Truncation:** 4th runner does not score; game ends when winning run crosses plate

---

## 5. Data Model Impacts

### 5.1 GameState Changes
```csharp
public class GameState
{
    public int Inning { get; set; }           // 1..N (no upper limit)
    public InningHalf Half { get; set; }      // Top | Bottom
    public Score Score { get; set; }          // Away:int, Home:int
    public RunsByInning RunsByInning { get; set; }  // Dynamic lists
    public bool IsFinal { get; set; }
    public Team? Winner { get; set; }
    public FinalizationMethod FinalizationMethod { get; set; }  // NEW
}

public class RunsByInning
{
    public List<int> Away { get; set; }  // Expands dynamically
    public List<int> Home { get; set; }  // Expands dynamically
}

public enum FinalizationMethod
{
    CompletedInning,  // Game ended after a completed inning with unequal scores
    WalkOff          // Game ended mid-inning when home team took lead
}
```

### 5.2 Enum Removals
- **Remove:** Any `Tie` enum value from `GameOutcome`, `GameResult`, or similar enums
- **Remove:** Any code paths that set or check for tie conditions
- **Remove:** Any documentation or error messages referencing ties

### 5.3 Line Score / Box Score Impacts
- `RunsByInning.Away` and `RunsByInning.Home` lists grow dynamically
- Printers must handle variable column counts (9, 10, 11, ..., N columns)
- Width-safe formatting required for readability
- Box score snapshots taken at end of each half-inning
- Walk-off ends the half-inning when winning run scores

### 5.4 Serialization Considerations
- JSON/XML serializers must handle variable-length arrays
- No hard-coded 9-column assumptions in serialization logic
- Deserialization must support games of any length

---

## 6. Out of Scope

The following features are explicitly **NOT** included in this implementation:

### 6.1 Ghost Runner on Second
- No automatic runner placed on 2nd base to start extra innings
- Extra innings begin with bases empty, same as regulation innings
- This rule may be added in a future enhancement

### 6.2 Pitcher Changes / Fatigue
- No automatic pitcher substitutions in extra innings
- No fatigue modeling that affects pitcher performance over time
- Pitchers continue indefinitely with same performance characteristics

### 6.3 Suspended Games
- No support for games suspended due to weather, darkness, or other reasons
- All games play to completion in a single session

### 6.4 Time Limits
- No time-based game termination
- Games continue until a winner is determined, regardless of duration

### 6.5 Playoff Variants
- No special playoff rules (e.g., different extra inning rules)
- All games follow the same extra inning rules

---

## 7. Telemetry & Logging

### 7.1 Game End State Logging
**Required log entries at game completion:**
```
Game final: [Half] [Inning], Method: [FinalizationMethod], Winner: [Team], Score: [Away]-[Home]
```

**Examples:**
- `Game final: Bottom 9, Method: WalkOff, Winner: Home, Score: 4-5`
- `Game final: Bottom 10, Method: CompletedInning, Winner: Away, Score: 6-5`
- `Game final: Bottom 12, Method: WalkOff, Winner: Home, Score: 7-6`

### 7.2 Inning Progression Logging
**Optional but recommended:**
- Log entry when entering extra innings: `Entering extra innings: Top 10, Score tied [N]-[N]`
- Log entry for each extra inning: `Starting inning [N], Score: [Away]-[Home]`

### 7.3 Walk-Off Event Logging
**Required for walk-off scenarios:**
```
Walk-off: [Play description], Winning run scored by [Runner], Final: [Away]-[Home]
```

**Example:**
- `Walk-off: Single to center, Winning run scored by Runner from 2nd, Final: 4-5`

### 7.4 Performance Metrics
**Track for analysis:**
- Distribution of game lengths (9 innings, 10 innings, 11+)
- Frequency of walk-offs vs. completed inning wins
- Average extra innings per game
- Longest game in simulation run

---

## 8. Risks & Mitigations

### 8.1 Risk: Double-Credit RBIs on Walk-Offs
**Description:** Non-home-run walk-offs might incorrectly credit RBIs for runners who don't score due to truncation.

**Mitigation:**
- Implement explicit walk-off detection before runner advancement
- Track which runners actually score vs. which are on base
- Unit tests verify correct RBI attribution in walk-off scenarios
- Test cases: 7.4 (WalkOff_BottomNine), 7.6 (WalkOff_HomeRun_CreditsAll)

### 8.2 Risk: Fixed 9-Column Assumptions
**Description:** Printers, serializers, or UI components may assume exactly 9 innings.

**Mitigation:**
- Audit all code that formats or displays line scores
- Replace hard-coded `9` with dynamic column count
- Add tests for 10+, 12+, and 15+ inning games
- Test case: 7.7 (LineScore_ExpandsBeyondNine)

### 8.3 Risk: Longer Games Changing Baselines
**Description:** Historical seeds that previously ended in 9 innings may now extend to extras, changing regression test expectations.

**Mitigation:**
- Re-baseline all regression tests with new behavior
- Update golden files to reflect extra innings
- Document which seeds now produce longer games
- Consider separate test suites for 9-inning vs. extra-inning scenarios

### 8.4 Risk: Infinite Game Loop
**Description:** Bug in finalization logic could cause games to never end.

**Mitigation:**
- Implement conservative safety cap (e.g., 30 innings)
- If cap is reached, throw clear exception (never mark as Tie)
- Log warning at 20 innings for monitoring
- Test case: 7.9 (Guardrail_NoInfiniteGames)

### 8.5 Risk: Walk-Off Detection Timing
**Description:** Walk-off might be detected too early or too late in play resolution.

**Mitigation:**
- Walk-off check occurs after run deltas are applied
- Check happens before advancing additional runners
- Explicit test cases for various walk-off scenarios
- Test cases: 7.4, 7.5, 7.6

---

## 9. Test Plan (TDD Order)

All tests will be implemented in: `tests/DiamondSim.Tests/Scoring/GameLoop_ExtrasAndWalkOffs_Tests.cs`

**TDD Protocol:**
1. Write exactly one failing test
2. Implement minimum code to pass that test
3. Run full test suite to ensure no regressions
4. Commit with message: `TDD: <TestName>`
5. Repeat for next test

### Test 7.1: RemovesTieOutcome_ComprehensiveSweep
**Purpose:** Verify no tie outcomes exist in codebase

**Arrange:**
- None (reflection-based test)

**Act:**
- Scan all enums for `Tie` value
- Run scenario that previously could end tied (e.g., 2-2 after 9 innings)

**Assert:**
- No `Tie` enum value found
- No terminal tie state in game result
- Game continues to extra innings instead of ending tied

**Expected Initial Failure:** Existing `Tie` enum or tie-handling code paths are found

**Implementation:** Remove all tie-related enums, replace with continued play logic

---

### Test 7.2: ProceedsToExtras_WhenTiedAfterNine
**Purpose:** Verify game continues to 10th inning when tied after 9

**Arrange:**
- Force game to be tied after bottom of 9th inning (e.g., 3-3)

**Act:**
- Complete bottom of 9th inning
- Check game state

**Assert:**
- `IsFinal == false`
- `Inning == 10`
- `Half == Top`
- Game continues

**Expected Initial Failure:** Game ends or marks final after 9 innings

**Implementation:** Modify game loop to check for tie and continue to inning 10

---

### Test 7.3: CompletesExtras_WhenAwayLeadsAfterBottom
**Purpose:** Verify game ends correctly when Away team leads after completed extra inning

**Arrange:**
- Force tie after 9 innings (e.g., 4-4)
- Top 10: Away scores 1 run (5-4)
- Bottom 10: Home scores 0 runs (5-4)

**Act:**
- Complete bottom of 10th inning

**Assert:**
- `IsFinal == true`
- `Winner == Away`
- `FinalizationMethod == CompletedInning`
- `Score.Away == 5, Score.Home == 4`

**Expected Initial Failure:** Finalization logic doesn't handle extra innings

**Implementation:** Add finalization check after each completed inning in extras

---

### Test 7.4: WalkOff_BottomNine
**Purpose:** Verify walk-off in bottom 9th with runner advancement truncation

**Arrange:**
- Home trails 4-5 entering bottom 9th
- Runner on 2nd base
- Batter hits single

**Act:**
- Process single play result

**Assert:**
- `IsFinal == true`
- `Winner == Home`
- `FinalizationMethod == WalkOff`
- `Score.Home == 6` (only +1 run, not +2)
- Runner from 2nd scored, batter did NOT advance to 2nd

**Expected Initial Failure:** Walk-off detection doesn't exist or doesn't truncate advancement

**Implementation:** Add walk-off detection in `ApplyPlayResult()` for bottom 9+

---

### Test 7.5: WalkOff_BottomTwelve
**Purpose:** Verify walk-off works in extra innings and line score expands

**Arrange:**
- Game tied 5-5 entering bottom 12th
- Non-home-run scoring play for Home (e.g., single with runner on 3rd)

**Act:**
- Process scoring play

**Assert:**
- `IsFinal == true`
- `Winner == Home`
- `FinalizationMethod == WalkOff`
- `Score.Home == 6`
- `RunsByInning.Away.Count == 12`
- `RunsByInning.Home.Count == 12`
- Line score displays 12 columns

**Expected Initial Failure:** Walk-off detection doesn't work in extras or line score doesn't expand

**Implementation:** Ensure walk-off logic works for any inning >= 9, verify dynamic line score

---

### Test 7.6: WalkOff_HomeRun_CreditsAll
**Purpose:** Verify home run walk-off scores all runners (no truncation)

**Arrange:**
- Game tied 3-3 in bottom 10th
- Runners on 1st and 2nd
- Batter hits home run

**Act:**
- Process home run play result

**Assert:**
- `IsFinal == true`
- `Winner == Home`
- `FinalizationMethod == WalkOff`
- `Score.Home == 6` (all 3 runners scored: +3 runs)
- All runners plus batter credited with scoring

**Expected Initial Failure:** Home run walk-off might truncate runners incorrectly

**Implementation:** Add home run exception to walk-off truncation logic

---

### Test 7.7: LineScore_ExpandsBeyondNine
**Purpose:** Verify line score dynamically expands for long games

**Arrange:**
- Force game to 14 innings (e.g., tied through 13, Away wins in 14)

**Act:**
- Complete game

**Assert:**
- `RunsByInning.Away.Count == 14`
- `RunsByInning.Home.Count == 14`
- Sum of `RunsByInning.Away` equals `Score.Away`
- Sum of `RunsByInning.Home` equals `Score.Home`
- Line score formatter displays all 14 columns correctly

**Expected Initial Failure:** Line score fixed at 9 columns or formatting breaks

**Implementation:** Make line score lists dynamic, update formatters for variable width

---

### Test 7.8: Determinism_PreservedAcrossExtras
**Purpose:** Verify same seed produces identical results including extras

**Arrange:**
- Seed S that produces extra innings game
- Run full game with seed S, capture snapshot (winner, score, line score)

**Act:**
- Re-run game with same seed S
- Capture second snapshot

**Assert:**
- Snapshots are identical:
  - Same winner
  - Same final score
  - Same line score (inning-by-inning runs)
  - Same inning of completion
  - Same finalization method

**Expected Initial Failure:** Non-deterministic behavior in extras or walk-offs

**Implementation:** Verify all randomness is seeded, no time-based or non-deterministic logic

---

### Test 7.9: Guardrail_NoInfiniteGames
**Purpose:** Verify safety cap prevents infinite games without marking ties

**Arrange:**
- Run multiple games with various seeds (e.g., 100 games)

**Act:**
- Complete all games

**Assert:**
- No game exceeds safety cap (e.g., 30 innings)
- No exceptions thrown
- All games produce a winner
- If any game approaches cap, log warning but continue

**Expected Initial Failure:** No safety cap exists

**Implementation:** Add conservative safety cap with clear exception if reached

---

## 10. Rollback Plan

### 10.1 Feature Flag (Temporary)
**If needed for gradual rollout:**
- Add configuration flag: `EnableExtraInnings` (default: `true`)
- When `false`: Enforce 9-inning limit, but do NOT re-introduce ties
  - If tied after 9, Away team wins (or Home team wins, or random—document clearly)
  - This is a temporary stabilization measure only

**Flag removal:**
- Feature flag should be removed after 1-2 release cycles
- Never permanently support tie games

### 10.2 Rollback Procedure
**If critical issues are discovered:**
1. Set `EnableExtraInnings = false` in configuration
2. Deploy hotfix with flag disabled
3. Document known issues and workarounds
4. Fix issues in development branch
5. Re-enable flag in next release
6. Remove flag entirely once stable

### 10.3 No Tie Re-Introduction
**Critical constraint:**
- Rollback must NOT re-introduce tie games as a valid outcome
- If extra innings are disabled, games must still produce a winner
- Acceptable temporary workarounds:
  - Sudden death: First team to score in extras wins
  - Coin flip: Random winner if tied after 9
  - Away advantage: Away team wins ties
- Document clearly that these are temporary measures

---

## 11. Implementation Checklist

### Phase 1: PRD & Setup
- [x] Create PRD document (this file)
- [ ] Review and approve PRD
- [ ] Create test fixture: `GameLoop_ExtrasAndWalkOffs_Tests.cs`
- [ ] Set up TDD workflow and commit conventions

### Phase 2: TDD Implementation (Sequential)
- [ ] Test 7.1: RemovesTieOutcome_ComprehensiveSweep
- [ ] Test 7.2: ProceedsToExtras_WhenTiedAfterNine
- [ ] Test 7.3: CompletesExtras_WhenAwayLeadsAfterBottom
- [ ] Test 7.4: WalkOff_BottomNine
- [ ] Test 7.5: WalkOff_BottomTwelve
- [ ] Test 7.6: WalkOff_HomeRun_CreditsAll
- [ ] Test 7.7: LineScore_ExpandsBeyondNine
- [ ] Test 7.8: Determinism_PreservedAcrossExtras
- [ ] Test 7.9: Guardrail_NoInfiniteGames

### Phase 3: Integration & Polish
- [ ] Update printers/formatters for dynamic columns
- [ ] Update serialization logic
- [ ] Re-baseline regression tests
- [ ] Update golden files
- [ ] Remove tie-related documentation
- [ ] Add logging for game completion events

### Phase 4: Validation & Release
- [ ] Run full test suite (all tests pass)
- [ ] Performance testing (ensure no significant slowdown)
- [ ] Code review
- [ ] Update changelog: "No ties; extra innings; walk-offs implemented"
- [ ] Merge to main branch

---

## 12. Success Metrics

### Functional Metrics
- **Zero tie games:** 100% of games produce a winner
- **Walk-off accuracy:** Walk-offs end game immediately with correct score
- **Extra innings frequency:** ~10-15% of games extend beyond 9 innings (validate against real baseball statistics)
- **Determinism:** 100% reproducibility with same seed

### Quality Metrics
- **Test coverage:** 100% coverage of new code paths
- **Regression tests:** All existing tests pass or are re-baselined
- **Performance:** No more than 5% slowdown in average game simulation time

### Operational Metrics
- **Zero infinite loops:** No games exceed safety cap
- **Logging completeness:** 100% of games log finalization method
- **Documentation accuracy:** All references to ties removed

---

## 13. Appendix: Example Scenarios

### Scenario A: Walk-Off Single in Bottom 9th
```
Situation: Bottom 9, 2 outs, Home trails 3-4, runner on 2nd
Play: Single to right field
Result:
  - Runner from 2nd scores (tie 4-4)
  - Batter reaches 1st, runner from 1st scores (win 5-4)
  - Game ends immediately (WalkOff)
  - Batter does NOT advance to 2nd
Final: Home wins 5-4, Bottom 9, WalkOff
```

### Scenario B: Extra Innings, Away Wins
```
Situation: Tied 2-2 after 9 innings
Top 10: Away scores 2 runs (4-2)
Bottom 10: Home scores 1 run (4-3)
Result:
  - Inning completes with Away leading
  - Game ends (CompletedInning)
Final: Away wins 4-3, Bottom 10, CompletedInning
```

### Scenario C: Walk-Off Home Run in 12th
```
Situation: Bottom 12, tied 5-5, runners on 1st and 3rd
Play: Home run to left field
Result:
  - Runner from 3rd scores (6-5)
  - Runner from 1st scores (7-5)
  - Batter scores (8-5)
  - Game ends immediately (WalkOff)
  - All runners score (home run exception)
Final: Home wins 8-5, Bottom 12, WalkOff
```

### Scenario D: Long Extra Innings Game
```
Situation: Tied 3-3 after 9 innings
Innings 10-13: Both teams score 0 runs each inning
Top 14: Away scores 1 run (4-3)
Bottom 14: Home scores 0 runs (4-3)
Result:
  - Game ends after 14 complete innings
  - Line score shows 14 columns for both teams
Final: Away wins 4-3, Bottom 14, CompletedInning
```

---

**End of PRD**
