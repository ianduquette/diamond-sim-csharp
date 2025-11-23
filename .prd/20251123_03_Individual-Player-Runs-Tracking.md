# PRD: Individual Player Runs (R) Tracking - Simplified Approach

**Date:** 2025-11-23
**Priority:** High (Demo Feature)
**Type:** Feature Enhancement
**Estimated Effort:** 2-3 hours
**Dependencies:** None (can be implemented independently)

## Executive Summary

Implement tracking of individual player Runs (R) using **Batter object references** on bases. Currently, individual player R only shows home runs. This simple approach uses existing `Batter` objects instead of lineup position integers.

**Note:** Individual LOB is NOT a real MLB stat (it's team-only), so we're skipping it. Team LOB is already tracked correctly.

## MLB Box Score Format Research

### Official MLB.com Box Score Format (2024)

**Batting Statistics Shown Per Player:**
```
BATTING
Player Name          AB  R  H  RBI  BB  SO  AVG
```

**Key Stats:**
- **AB** (At Bats) - ✅ We have this
- **R** (Runs) - ❌ We need to fix this
- **H** (Hits) - ✅ We have this
- **RBI** (Runs Batted In) - ✅ We have this
- **BB** (Walks) - ✅ We have this
- **SO/K** (Strikeouts) - ✅ We have this
- **AVG** (Batting Average) - ⏸️ Can calculate (H/AB)

**NOT Shown Per Player:**
- ❌ Individual LOB - This is a TEAM stat only
- ❌ Individual PA (Plate Appearances) - Sometimes shown in expanded stats
- ❌ Individual HR - Sometimes shown separately

**Team Totals Line:**
```
TOTALS              AB  R  H  RBI  BB  SO  LOB
```
- Team LOB is shown in totals
- Individual LOB is NOT shown

### What We Currently Show

**Our Current Format:**
```
BATTING                 PA  AB  H  RBI  BB  K  HR
Player Name             4   3   1   0    1   1   0
TOTALS                 36  30  8   4    6   8   2
```

**Comparison:**
- ✅ We show PA (good for analysis, not standard MLB)
- ✅ We show HR separately (good, some MLB sites do this)
- ❌ We're missing R column
- ❌ We don't show AVG (but can calculate)
- ✅ Team LOB is shown correctly

### Recommendation for Demo

**Minimal Change - Add R Column:**
```
BATTING                 PA  AB  R  H  RBI  BB  K  HR
Player Name             4   3   1  1   0    1   1   0
TOTALS                 36  30  4  8   4    6   8   2
```

**Benefits:**
- Matches MLB standard (R is always shown)
- Minimal code changes
- No need to calculate AVG yet
- Keep PA and HR columns (useful for analysis)

### MLB Rules Verification

**Runs (R) - Official MLB Rule 9.07:**
✅ **A player gets credit for 1 run when they cross home plate**
- Doesn't matter HOW they got on base (hit, walk, error, HBP)
- Doesn't matter HOW they scored (hit, error, wild pitch, etc.)
- Simple rule: Did this player touch home plate? If yes, R = R + 1

**Left On Base (LOB) - Official MLB Rule 10.21(b):**
❌ **Individual LOB is NOT an official MLB statistic**
- LOB is tracked at the TEAM level only
- Shown only in team totals line
- Individual "Runners Left In Scoring Position" (RISP) exists but is advanced/optional

## Current Problem

### What Works
- Team runs are correct (AwayScore, HomeScore)
- RBI attribution is correct
- Team LOB is correct

### What's Broken
From [`BoxScore.cs:70-75`](src/DiamondSim/BoxScore.cs:70-75):
```csharp
/// LIMITATION (v1): Only tracks runs scored during the batter's own plate appearance (home runs).
/// Does not track when a batter who reached base earlier scores as a runner on a subsequent play.
public int R { get; init; }
```

**Example (Seed 42):**
- Team scores 4 runs
- Box score shows only 1 R (the home run)
- Missing 3 runs from players who reached base and later scored

## Root Cause

**Current BaseState:**
```csharp
public sealed record BaseState(
    bool OnFirst,
    bool OnSecond,
    bool OnThird
);
```

**Problem:** We know SOMEONE is on base, but not WHO.

## Proposed Solution: Store Batter References

### Simple & Object-Oriented Approach

```csharp
/// <summary>
/// Represents base occupancy with Batter tracking.
/// </summary>
public sealed record BaseState(
    bool OnFirst,
    bool OnSecond,
    bool OnThird,
    Batter? BatterOnFirst,   // The actual Batter object (or null)
    Batter? BatterOnSecond,
    Batter? BatterOnThird
) {
    // Factory for empty bases
    public static BaseState Empty => new(false, false, false, null, null, null);

    // Validation helper
    public void Validate() {
        if (OnFirst && BatterOnFirst == null)
            throw new InvalidOperationException("OnFirst=true but BatterOnFirst=null");
        if (!OnFirst && BatterOnFirst != null)
            throw new InvalidOperationException("OnFirst=false but BatterOnFirst has value");
        // Similar for second and third
    }
}
```

**Why This is Simple:**
- ✅ We already have `Batter` objects in the lineup
- ✅ Just store references (8 bytes each, trivial memory)
- ✅ No need to map lineup positions
- ✅ More object-oriented and readable

### Enhanced RunnerMove

```csharp
public sealed record RunnerMove(
    int FromBase,           // 0=batter, 1=first, 2=second, 3=third
    int ToBase,             // 1-4 where 4=home/scored
    bool Scored,
    bool WasForced,
    Batter Runner           // NEW: Which batter is moving
);
```

### Updated GameState

```csharp
public sealed record GameState(
    // ... existing fields ...
    bool OnFirst,
    bool OnSecond,
    bool OnThird,
    Batter? BatterOnFirst,   // NEW
    Batter? BatterOnSecond,  // NEW
    Batter? BatterOnThird,   // NEW
    // ... rest of fields ...
) {
    // Helper to get BaseState
    public BaseState GetBaseState() => new(
        OnFirst, OnSecond, OnThird,
        BatterOnFirst, BatterOnSecond, BatterOnThird
    );
}
```

## Implementation Plan (TDD)

### Phase 1: Enhanced BaseState (30 min)

**Test 1.1:**
```csharp
[Test]
public void BaseState_WithBatterReferences_StoresCorrectly() {
    var batter = new Batter("Player 1", BatterRatings.Average);
    var state = new BaseState(true, false, false, batter, null, null);

    Assert.That(state.BatterOnFirst, Is.EqualTo(batter));
}
```

### Phase 2: BaseRunnerAdvancement Updates (1 hour)

**Key Change:** Pass current batter and base state with Batter references

```csharp
public PaResolution Resolve(
    AtBatTerminal terminal,
    BipOutcome? bipOutcome,
    BipType? bipType,
    BaseState currentBases,      // Now includes Batter references
    int currentOuts,
    Batter currentBatter,        // NEW: Who is batting
    IRandomSource rng)
```

**Example - Single with R2:**
```csharp
// R2 scores
moves.Add(new RunnerMove(2, 4, true, false, currentBases.BatterOnSecond!));

// Batter to first
moves.Add(new RunnerMove(0, 1, false, false, currentBatter));
```

### Phase 3: InningScorekeeper R Tracking (30 min)

```csharp
public ApplyResult ApplyPlateAppearance(GameState state, PaResolution resolution) {
    // ... existing logic ...

    // Credit runs to individual players
    foreach (var move in resolution.Moves.Where(m => m.Scored)) {
        Batter scorer = move.Runner;
        Team team = state.Offense;

        // Find this batter in the box score and increment R
        var batters = team == Team.Away ? BoxScore.AwayBatters : BoxScore.HomeBatters;

        // Find lineup position for this batter
        var lineup = team == Team.Away ? _awayLineup : _homeLineup;
        int lineupPos = lineup.IndexOf(scorer);

        if (batters.TryGetValue(lineupPos, out var stats)) {
            batters[lineupPos] = stats with { R = stats.R + 1 };
        }
    }

    // ... rest of logic ...
}
```

### Phase 4: Integration Test (30 min)

```csharp
[Test]
public void FullGame_IndividualRunsMatchTeamTotal() {
    var simulator = new GameSimulator("Home", "Away", 42);
    var result = simulator.RunGameV2();

    // Individual runs must equal team total
    int awayIndividualRuns = result.BoxScore.AwayBatters.Values.Sum(b => b.R);
    Assert.That(awayIndividualRuns, Is.EqualTo(result.FinalState.AwayScore));

    int homeIndividualRuns = result.BoxScore.HomeBatters.Values.Sum(b => b.R);
    Assert.That(homeIndividualRuns, Is.EqualTo(result.FinalState.HomeScore));
}
```

## Files to Modify

1. **Model.cs** - BaseState enhancement (add Batter? fields)
2. **GameState.cs** - Add Batter? fields
3. **BaseRunnerAdvancement.cs** - Update Resolve signature, populate Batter in RunnerMove
4. **InningScorekeeper.cs** - Credit runs to individual batters
5. **GameSimulator.cs** - Pass Batter references when calling Resolve
6. **Tests** - Update to use new BaseState constructor

## Success Criteria

- [ ] Individual player R matches team total runs
- [ ] Box score TOTALS R row shows correct sum
- [ ] All 266+ existing tests still pass
- [ ] New integration test passes
- [ ] No performance degradation

## Benefits

### For Demo
- ✅ **Complete Statistics**: Box score shows accurate individual runs
- ✅ **Realistic Output**: Matches real MLB box scores
- ✅ **Simple Implementation**: Uses existing Batter objects

### For Future
- ✅ **Foundation**: Enables advanced stats (OBP, SLG)
- ✅ **Extensible**: Easy to add more per-player tracking

## Memory Impact

**Trivial:** 18 Batter references × 8 bytes = 144 bytes total
- 9 batters per team × 2 teams = 18 references
- Modern systems have gigabytes of RAM
- This is completely negligible

## Related Documents

- Current Limitation: `.docs/box_score_runs_limitation.md`
- GameSimulator Refactor: `.prd/20251123_02_Refactor-GameSimulator-Return-Object.md`

---

**Approval Required:** Yes
**Approach:** Test-Driven Development (TDD)
**Estimated Time:** 2-3 hours
**Breaking Changes:** BaseState constructor signature changes (all callers must be updated)
