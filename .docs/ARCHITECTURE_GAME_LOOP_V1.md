# Game Loop Implementation Architecture - v1 Final

**Document Version:** 1.2 Final (Ready to Implement)
**Date:** 2025-10-29
**Status:** Ready for Implementation

---

## Executive Summary

This document defines the complete architecture for implementing the single-game simulator per PRD set (v1.1 Review‑Locked + v1.2 Impl+Tests). The design emphasizes **minimal layers**, **single responsibility**, and **deterministic execution**.

---

## Core Principles

1. **Minimize Layers** — no pass‑through “micro classes.”
2. **Single State Mutator** — `InningScorekeeper` is the **only** component that mutates game state.
3. **Pure Functions** — helpers return values, never mutate.
4. **Single RNG** — one `IRandomSource` instance threaded end‑to‑end.
5. **Determinism** — same seed ⇒ identical output (timestamp excluded).

---

## Component Architecture

### 1) PlayByPlayPhrases (Static Utility)
**File:** `src/DiamondSim/PlayByPlayPhrases.cs` (NEW)
**Purpose:** Centralize all fixed phrases to prevent drift.

**Constants & Methods (exact strings per PRD):**
```csharp
public static class PlayByPlayPhrases {
    // Constants (parameterless)
    public const string Walk = "Walk";
    public const string HitByPitch = "HBP";
    public const string WalkoffPrefix = "Walk-off: ";

    // Parameterized
    public static string Strikeout(bool looking) => looking ? "K looking" : "K swinging";
    public static string Single(string field) => $"Single to {field}";
    public static string Double(string field) => $"Double to {field}";
    public static string Triple(string field) => $"Triple to {field}";
    public static string HomeRun(string field) => $"Home run to {field}";
    public static string Groundout(string positions) => $"Groundout {positions}";
    public static string Flyout(string field) => $"Flyout to {field}";
    public static string Lineout(string position) => $"Lineout to {position}";
    public static string ReachOnError(int position) => $"Reaches on E{position}";
    public static string SacrificeFly(string field) => $"Sacrifice fly to {field}";
    public static string GroundsIntoDP(string positions) => $"Grounds into DP {positions}";
    public static string OutsPhrase(int outs) => outs == 1 ? "1 out." : $"{outs} outs.";
}
```

---

### 2) BaseRunnerAdvancement (Pure Math Helper)
**File:** `src/DiamondSim/BaseRunnerAdvancement.cs` (NEW)
**Purpose:** Pure calculation of runner advancement and outcomes.

**Inputs:**
- `AtBatTerminal` (K, BB, HBP, or BallInPlay)
- `BipOutcome?` (from BallInPlayResolver; null for non‑BIP)
- Current `BaseState` (OnFirst/OnSecond/OnThird), current `outs`
- `IRandomSource` (for ROE, DP probabilities if applicable)

**Output:** Enhanced `PaResolution`
```csharp
public enum OutcomeTag { K, BB, HBP, Single, Double, Triple, HR, ROE, SF, DP, TP, InPlayOut }

public sealed record RunnerMove(
    int FromBase,   // 0=batter, 1..3 for R1..R3 pre-play
    int ToBase,     // 1..4 (4=home)
    bool Scored,
    bool WasForced
);

public sealed record PaResolution(
    int OutsAdded,                  // 0..3
    int RunsScored,                 // 0..4
    BaseState NewBases,             // post-play occupancy
    PaType Type,                    // K, BB, HBP, Single, Double, etc.
    OutcomeTag Tag,                 // for formatter/box-score
    PaFlags? Flags,                 // IsDoublePlay, IsSacFly
    bool HadError,                  // true if ROE
    int RbiForBatter,               // explicit RBI count (walk-off clamp applied upstream)
    BaseState? AdvanceOnError,      // null in v1 (no explicit advance errors)
    BaseState? BasesAtThirdOut,     // snapshot BEFORE third-out mutation
    IReadOnlyList<RunnerMove> Moves // detailed runner movements for logging
);
```

**Responsibilities:**
- Translate BIP/terminal outcomes into `RunnerMove` list, `NewBases`, `RunsScored`, `OutsAdded`.
- Determine DP/TP/SF when applicable.
- ROE handling: small % of outs become `HadError=true`; if no fielder context, default `E6`.
- Compute `RbiForBatter` per rules (ROE=0; BL BB/HBP=1; SF=1; HR=all; hits credit per scoring runner).
- Compute `BasesAtThirdOut` **before** any state mutation.
- **No stat increments** here; purely descriptive.

---

### 3) GameSimulator (Main Loop — Consolidated)
**File:** `src/DiamondSim/GameSimulator.cs` (NEW)
**Purpose:** Orchestrate PreGame → GameComplete.

**Constructor:**
```csharp
public GameSimulator(string homeTeamName, string awayTeamName, int seed)
```
Creates the single RNG instance.

**Inline lineup generation:**
```csharp
private List<Batter> GenerateLineup(string teamName, IRandomSource rng) {
    var batters = new List<Batter>();
    for (int i = 1; i <= 9; i++)
        batters.Add(new Batter($"{teamName} {i}", BatterRatings.Average));
    Shuffle(batters, rng); // Fisher–Yates with provided RNG
    return batters; // display shows #9 as (DH); pitcher never bats
}
```

**Plate appearance loop (correct order):**
```csharp
while (!state.IsFinal) {
    var batter = GetCurrentBatter(state);
    var pitcher = GetCurrentPitcher(state);

    var terminal = atBatSimulator.SimulateAtBat(pitcher.Ratings, batter.Ratings); // terminal only

    BipOutcome? bip = null;
    if (terminal == AtBatTerminal.BallInPlay) {
        bip = BallInPlayResolver.ResolveBallInPlay(batter.Ratings.Power, pitcher.Ratings.Stuff, rng);
    }

    var resolution = baseRunnerAdvancement.Resolve(
        terminal, bip,
        new BaseState(state.OnFirst, state.OnSecond, state.OnThird),
        state.Outs, rng);

    var apply = inningScorekeeper.ApplyPlateAppearance(state, resolution); // mutates, walk-off clamp + LOB=0

    // Build log AFTER apply, using defense (pitcher’s team) in the “vs ____ P” slot
    var log = BuildPlayLogEntry(
        state.Inning, state.Half, batter,
        GetTeamName(state.Defense), // FIXED: use defense, not offense
        resolution, apply.IsWalkoff, apply.OutsAfter);
    playLog.Add(log);

    state = apply.StateAfter;
}
```

**Apply result:**
```csharp
public sealed record ApplyResult(GameState StateAfter, bool IsWalkoff, int OutsAfter);
```

**Log construction (summary):**
- Runner text from `resolution.Moves`:
  - show only existing runners (R1/R2/R3) moves and any “Batter scores”
  - omit routine batter → 1B/2B/3B movement text on hits
- Prefix `Walk-off:` when `apply.IsWalkoff` is true.
- Outs phrase uses `apply.OutsAfter`.

---

### 4) GameReportFormatter (Output Only)
**File:** `src/DiamondSim/GameReportFormatter.cs` (NEW)
**Purpose:** Format final text per PRD.

**Inputs:** final `GameState`, `LineScore`, `BoxScore`, `List<string> playLog`, seed, team names, timestamp.
**Outputs:** full report string.

**Timestamp:** `yyyy-MM-dd HH:mm zzz` (numeric offset; no seconds).
**LogHash:** SHA‑256 over **normalized** play‑log lines (trim end, join with `\n`).

```csharp
private string CalculateLogHash(List<string> playLog) {
    using var sha256 = SHA256.Create();
    var normalized = playLog.Select(l => l.TrimEnd());
    var combined = string.Join("\n", normalized);
    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
}
```

---

### 5) Program.cs (CLI Wrapper)
**File:** `src/DiamondSim/Program.cs` (NEW or MODIFY)

**Args:**
```
--home <name>   (required)
--away <name>   (required)
--seed <int>    (optional; if omitted, generate and print)
```
**Exit codes:** `0` success; `2` bad args; `1` unexpected error.
**Behavior:** generate seed if omitted, pass down; print seed in **header + footer**.

---

## Critical Implementation Details

- **Walk-off clamp & LOB=0** in `InningScorekeeper.ApplyPlateAppearance` (returns `ApplyResult`).
- **Errors “E”** counted **only** by `InningScorekeeper` from `resolution.HadError`.
- **BasesAtThirdOut** captured in `BaseRunnerAdvancement.Resolve` **before** third-out mutation.
- **Fixed phrasing** lives only in `PlayByPlayPhrases` + simple formatter; no synonyms.

---

## Test Requirements (must-ship set)

### Unit
- Non‑HR walk‑off clamp (minimum runs; `LOB=0`).
- Walk‑off HR (all runners+batter; `LOB=0`).
- Skip B9 when home leads after T9 (line score `X`).
- Batting: `AB = PA - BB - HBP - SF`.
- Pitching: IP thirds `.0/.1/.2`.
- Errors: `E = ROE` when no explicit advance errors exist.
- **Snapshot of `BasesAtThirdOut`** behavior on inning-ending plays.

### Integration / Snapshot
- Seeds: `42, 8675309, 12345, 20251028, 314159`.
- Assert byte‑for‑byte report (excluding timestamp) **or** assert `LogHash` + `R/H/E/LOB`.

### Contract (CLI)
- Missing `--home`/`--away` → exit `2`, usage to STDERR.
- Unknown flag → exit `2`, usage to STDERR.
- Unexpected exception → exit `1`.
- Success → exit `0` and prints full report to STDOUT.

---

## Determinism Checklist
- [ ] Single RNG instance created in `GameSimulator`.
- [ ] RNG passed to `AtBatSimulator`, `BallInPlayResolver`, `BaseRunnerAdvancement`.
- [ ] No `new Random()`, `Guid.NewGuid()`, or `DateTime.Now` in outcome logic.
- [ ] Timestamp is the **only** non-deterministic field (and ignored in tests).
- [ ] `LogHash` normalized (trim trailing spaces; `\n` newlines only).
- [ ] Same seed ⇒ identical `LogHash` and report body (excl. timestamp).

---

## Red Flags to Reject
❌ Multiple RNGs or any non‑seeded randomness.
❌ Pitch‑by‑pitch emission.
❌ Any helper that mutates game state besides scorekeeper.
❌ JSON/CSV writers or file I/O in the simulator.
❌ Stats incremented anywhere but `InningScorekeeper`.

---

## Minimal Loop Order (Reference)
```csharp
for each PA:
  batter, pitcher = current
  terminal = AtBatSimulator.TerminalOutcome(...)
  bip = (terminal == InPlay) ? BallInPlayResolver.Resolve(...) : null
  res = BaseRunnerAdvancement.Resolve(terminal, bip, preBases, preOuts, rng)  // pure
  apply = InningScorekeeper.ApplyPlateAppearance(state, res)                  // mutates, clamp, LOB=0
  logLine = Formatter.PlayLine(apply.IsWalkoff, apply.OutsAfter, res, batter, GetTeamName(state.Defense), inning/half)
  state = apply.StateAfter
```

---

## Document Control
**Approvals:** Technical Lead
**Next Steps:** Begin implementation with `PlayByPlayPhrases.cs` then `BaseRunnerAdvancement.cs`; wire loop; add formatter; wire CLI; ship tests.
