# Box Score Runs (R) Column Limitation - Analysis & Options

## Executive Summary

The box score shows incorrect individual batter runs (R column) because the current architecture only tracks whether a batter scored **during their own plate appearance** (i.e., home runs). It does not track when a batter who reached base earlier subsequently scores as a runner.

## Root Cause Analysis

### Current Implementation

In [`InningScorekeeper.ApplyPlateAppearance()`](src/DiamondSim/InningScorekeeper.cs:324), the `batterScored` flag is determined by:

```csharp
bool batterScored = resolution.Moves?.Any(m => m.FromBase == 0 && m.ToBase == 4 && m.Scored) ?? false;
```

This checks if the **current batter** (FromBase=0) scored during **this plate appearance**. This only happens for home runs.

### What's Missing

When a batter reaches base (e.g., via single, walk, error) and later scores as a runner on a subsequent plate appearance, there is no mechanism to:

1. **Track which lineup position occupies each base** - The current `BaseState` only tracks boolean occupancy (OnFirst, OnSecond, OnThird), not WHO is on each base
2. **Credit the run to the correct batter** - When a runner scores, we don't know which lineup position to increment

### Evidence from Code

**BaseRunnerAdvancement.cs** populates `Moves` with runner movements:
- Line 329: `moves.Add(new RunnerMove(0, 1, false, false));` - Batter to first on single
- Line 334: `moves.Add(new RunnerMove(3, 4, true, false));` - R3 scores

But `RunnerMove` only tracks base positions (FromBase, ToBase), not lineup positions:

```csharp
public sealed record RunnerMove(
    int FromBase,    // 0=batter, 1=first, 2=second, 3=third
    int ToBase,      // 1-4 where 4=home/scored
    bool Scored,
    bool WasForced
);
```

## Impact on Box Score

### Observed Issues (Seed 42)

1. **TOTALS R mismatch:**
   - Comets: Shows 0 R in TOTALS, but final score is 4 R
   - Sharks: Shows 1 R in TOTALS, but final score is 2 R

2. **Individual batter R values:**
   - Most batters show 0 R even when they scored
   - Only batters who hit HRs show R=1

3. **RBI attribution:**
   - RBI is correctly calculated (based on runs scored during PA)
   - But individual R credits are missing for runners who scored

### Why RBI Works But R Doesn't

- **RBI**: Calculated per plate appearance based on `resolution.RunsScored` - works correctly
- **R (Runs)**: Requires tracking which specific batters scored - currently impossible without base-to-lineup mapping

## Solution Options

### Option A: Accept Limitation for v1 (RECOMMENDED)

**Pros:**
- No code changes required
- Maintains current architecture simplicity
- Team totals (runs scored) are correct
- RBI attribution is correct

**Cons:**
- Individual batter R column is inaccurate (only shows HR runs)
- Box score TOTALS R won't match final score

**Documentation Required:**
- Add comment in `BoxScore.cs` explaining limitation
- Update box score output to show disclaimer
- Document in README or release notes

### Option B: Implement Base-to-Lineup Tracking

**Requires:**

1. **Extend BaseState to track lineup positions:**
   ```csharp
   public sealed record BaseState(
       bool OnFirst,
       bool OnSecond,
       bool OnThird,
       int? LineupPosOnFirst,   // NEW: 0-8 or null
       int? LineupPosOnSecond,  // NEW: 0-8 or null
       int? LineupPosOnThird    // NEW: 0-8 or null
   );
   ```

2. **Update GameState to maintain lineup-to-base mapping:**
   - Track which lineup position is on each base
   - Update on every base advancement

3. **Modify BaseRunnerAdvancement to populate lineup positions:**
   - All `Resolve*` methods must track lineup positions
   - `RunnerMove` may need to include `LineupPosition`

4. **Update InningScorekeeper to credit runs correctly:**
   - When a runner scores (FromBase=1/2/3, ToBase=4), look up their lineup position
   - Increment R for that lineup position in BoxScore

**Pros:**
- Accurate individual batter R statistics
- Box score TOTALS R matches final score
- More realistic simulation

**Cons:**
- Significant refactoring required (10+ files)
- Increased complexity in state management
- More test updates needed
- Higher risk of introducing bugs

**Estimated Effort:** 4-6 hours

### Option C: Post-Process Approximation (HYBRID)

**Approach:**
- Keep current architecture
- After game completion, distribute team runs across batters proportionally based on:
  - Times on base (H + BB + HBP)
  - Position in lineup
  - Runs scored by team

**Pros:**
- Minimal code changes
- Provides "reasonable" R values
- No state tracking complexity

**Cons:**
- Not accurate (approximation only)
- Doesn't reflect actual game events
- May confuse users expecting real statistics

## Recommendation

**Choose Option A** for the current release:

1. **Document the limitation clearly** in code comments and user-facing documentation
2. **Add a note to box score output** explaining that individual R values only reflect home runs
3. **Plan Option B for a future release** when more comprehensive statistics are needed

### Rationale

- The current architecture is clean and working well for its intended scope
- Team-level statistics (runs, RBI) are correct
- Individual R tracking requires architectural changes that should be planned carefully
- Better to ship with documented limitations than rush a complex refactor

## Implementation Plan (Option A)

1. Add detailed comment in `BoxScore.cs` explaining the limitation
2. Update `GameReportFormatter.cs` to add disclaimer to box score output
3. Create this documentation file for future reference
4. Update test expectations to reflect current behavior

## Future Work (Option B)

If implementing full base-to-lineup tracking:

1. Create PRD for enhanced statistics tracking
2. Design new `BaseState` structure with lineup positions
3. Update all base advancement logic
4. Add comprehensive tests for runner tracking
5. Validate against real baseball scenarios
