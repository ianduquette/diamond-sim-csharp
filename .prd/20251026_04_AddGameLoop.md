# Product Requirements Document: Single-Game Simulator & Text Report — v1 Simple

**Document Version:** 1.1 (Review-Locked)
**Created:** 2025-10-28
**Status:** Ready for Implementation

---

## 1. Executive Summary

This PRD defines the requirements for a single-game baseball simulator that runs one complete 9-inning game and produces a deterministic text report. The simulator takes minimal inputs (seed, team names), auto-generates lineups, simulates plate appearances using existing probability models, and outputs a comprehensive text-based game report including line scores, play-by-play log, and box scores.

**Key Characteristics:**
- **Deterministic:** same seed produces identical game outcomes and text (timestamp excluded)
- **Text-only output:** no JSON, UI, or alternative formats
- **Simplified v1 scope:** no bullpen management, extra innings, or advanced features
- **Self-contained:** runs from command line with minimal configuration

**Primary Use Cases:**
- Regression testing of game simulation logic
- Reproducible game scenarios for debugging
- Baseline implementation for future enhancements

---

## 2. Goals / Non-Goals

### Goals
1. **Simulate a complete 9-inning baseball game** with realistic outcomes.
2. **Produce deterministic results** from a given seed for testing and reproducibility.
3. **Generate comprehensive text reports** including line scores, play logs, and box scores.
4. **Handle walk-off scenarios** correctly with proper run clamping and game termination.
5. **Support skipped bottom 9th** when home team leads after the top of the 9th.
6. **Provide reproducibility** by displaying the seed used (whether provided or generated).

### Non-Goals (v1)
1. **No bullpen management:** one starting pitcher per team for the entire game.
2. **No extra innings:** games can end in ties.
3. **No advanced features:** no park effects, weather, fatigue, injuries.
4. **No pitch-by-pitch detail:** only plate appearance outcomes.
5. **No alternative output formats:** text only, no JSON/XML/CSV.
6. **No interactive mode:** batch execution only.
7. **No lineup customization:** auto-generated lineups only.
8. **No in-game substitutions:** starting nine play the entire game.

---

## 3. User Inputs & CLI Contract

### Command-Line Interface (behavioral; not prescribing project layout)

**Minimal Invocation:**
```
DiamondSim --home "Sharks" --away "Comets"
```

**With Seed:**
```
DiamondSim --home "Sharks" --away "Comets" --seed 12345
```

### Input Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `--home` | string | Yes | – | Home team display name |
| `--away` | string | Yes | – | Away team display name |
| `--seed` | integer | No | Random | RNG seed for deterministic simulation |

### Input Validation

**Team Names:**
- Must be non-empty strings (display-only).

**Seed:**
- If provided: must be valid 32-bit integer.
- If omitted: system generates random seed and displays it in output.
- Seed value is printed in both header and footer for reproducibility.

### Fixed Settings (v1)

- **Designated Hitter:** Always ON (pitchers never bat).
- **Extra Innings:** OFF (ties allowed).
- **Lineup Generation:** Always auto-generated from seed.
- **Game Length:** 9 innings (or fewer if walk-off).

---

## 4. Determinism & RNG Scope

**Core Principle:** Given the same seed, the simulator must produce byte-for-byte identical output (excluding timestamp).

**RNG Usage:**
1. **Lineup order:** randomized once at game start.
2. **Plate appearance outcomes:** all PA resolutions draw from the same RNG stream.
3. **No other randomness:** no random wording or formatting.

**Excluded from determinism:**
- Timestamp in header (local time of execution).
- Any diagnostic output not part of the report (none expected in v1).

**RNG Initialization:**
- Single RNG instance seeded at game start from `--seed` or generated seed.
- RNG state must not be affected by external factors (time, environment).

**Testing Implications:**
- Same seed must produce identical: lineup order, PA outcomes, base-running results, final scores, box scores, play log contents, and `LogHash` value.

---

## 5. Game Flow (State Machine)

### Game Initialization — `PreGame`
1. Parse inputs.
2. Initialize or generate seed; create RNG.
3. Generate lineups for both teams (9 batters each).
4. Randomize batting order using RNG.
5. Initialize game state: Top 1st, 0–0, bases empty, 0 outs; current batter = Away #1.
**Transition:** `InningInProgress`

### Half-Inning Loop — `InningInProgress`
1. Identify current batter (cycle through lineup).
2. Resolve plate appearance to terminal outcome.
3. Update state (bases, outs, runs, stats).
4. Log play line.
5. Check end conditions.
**End conditions:**
- 3 outs recorded → `InningTransition`.
- Walk-off (bottom 9 only in v1) → `GameComplete`.

### Inning Transition — `InningTransition`
1. Record half-inning runs in line score.
2. Snapshot **LOB at instant of third out** (per separate spec).
3. Advance to next half; reset bases/outs.
4. Check game end conditions:
   - After **Top 9**: if Home leads → skip Bottom 9; mark `X` in line score → `GameComplete`.
   - After **Bottom 9**: game ends (win/loss/tie) → `GameComplete`.
**Else:** continue `InningInProgress`.

### Game Completion — `GameComplete`
1. Finalize statistics.
2. Compute team totals (R/H/E, team LOB).
3. Generate box scores (batting and pitching).
4. Compute `LogHash`.
5. Emit final report.

### Walk-off Handling (v1 scope)
- **Non-HR:** clamp to **minimum runs needed to win**, `LOB=0`, end immediately.
- **HR:** all runners + batter score, `LOB=0`, end immediately.

---

## 6. Output Shape (Report Specification)

### Report Sections
```
[HEADER]
[LINE SCORE]
[PLAY LOG]
[TEAM TOTALS]
[BOX SCORES - BATTING]
[BOX SCORES - PITCHING]
[FOOTER]
```

### Header
```
AWAY @ HOME — Seed: 12345
2025-10-28 19:30 MDT
DH: ON | Extras: OFF (tie allowed)
```
- Team names as provided.
- Seed value (provided or generated).
- Timestamp format: `YYYY-MM-DD HH:MM TZ` (no seconds).

### Running Line Score
```
       | 1  2  3  4  5  6  7  8  9 |  R  H  E
-------|---------------------------|---------
Away   | 0  1  0  2  0  0  0  0  0 |  3  8  1
Home   | 0  0  1  0  0  0  0  2  X |  3  7  0
```
- Always 9 inning columns; Home shows `X` if B9 skipped.
- Right-align numeric columns; totals at end.

### Play Log (one line per PA; fixed phrasing)
Pattern:
```
[Top 1] #1 Comet 1 vs Sharks P — Single to RF. R1 to 1B.
```
Fixed phrase set:
- **K:** `K swinging` or `K looking` (exact strings only).
- **BB:** `Walk`
- **HBP:** `HBP`
- **1B/2B/3B/HR:** `Single|Double|Triple|Home run to [LF/CF/RF]`
- **Groundout:** `Groundout [6-3/4-3/5-3/etc]`
- **Flyout/Lineout:** `Flyout to [LF/CF/RF]`, `Lineout to [position]`
- **ROE:** `Reaches on E[position]`
- **SF:** `Sacrifice fly to [OF]. R3 scores.` (and more scoring notes as needed)
- **DP/TP:** `Grounds into DP [6-4-3/etc]`, `Lines into TP [positions]`
- **Outs:** Append `1 out.`, `2 outs.`, `3 outs.` as applicable.
- **Walk-off marker:** Prefix `Walk-off:` before outcome on the decisive PA.

Runner/state notation examples: `R1 to 3B, R2 scores`, `R3 scores`.

### Team Totals
```
Final: AWAY 3 — HOME 4
Away: 3 R, 8 H, 1 E, 6 LOB
Home: 4 R, 7 H, 0 E, 5 LOB
```
- For ties, print `Final: AWAY x — HOME x (TIE)`.

### Box Scores

**Batting**
```
AWAY BATTING            AB   R   H  RBI   BB   K  HR  LOB
---------------------------------------------------------
Comet 1                  4    1   2    0    0   1   0    1
...
Comet 9 (DH)             3    0   0    0    0   0   0    0
---------------------------------------------------------
TOTALS                  31    3   8    3    1   8   0    6
```
- List all 9 batters in **lineup order**; mark 9th as `(DH)`.
- Right-align numeric columns; include totals row.

**Pitching**
```
AWAY PITCHING            IP   BF   H   R  ER   BB   K  HR
---------------------------------------------------------
Comets P                8.0   32   7   4   3    2   6   1
```
- One pitcher per team (v1).
- **Name format:** `"[Team] P"` (e.g., `Sharks P`, `Comets P`).
- IP uses thirds: `.0/.1/.2`.

### Footer
```
Seed: 12345
LogHash: a3f5c8d9e2b1f4a7c6d8e9f0a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0
```
- Repeat seed; `LogHash` is 64 hex chars.

---

## 7. Validation & Acceptance Criteria

### Functional
- **AC1 Determinism:** Same seed → identical output (timestamp excluded); matching `LogHash`.
- **AC2 Walk-off (non-HR):** Clamp to minimum needed; `LOB=0`; end immediately.
- **AC3 Walk-off (HR):** All runners+batter score; `LOB=0`; end immediately.
- **AC4 Skipped B9:** Home leads after Top 9 → skip Bottom 9; line score shows `X`.
- **AC5 Tie:** Tied after B9 → end game; print `(TIE)`.
- **AC6 Box Score Consistency:** `AB = PA - BB - HBP - SF`; IP thirds; sums reconcile.
- **AC7 RBI Rules:** ROE=0; BL BB/HBP=1; SF=1; HR=all; walk-off clamp.
- **AC8 Errors:** `E = ROE + explicit fielding errors`; if none explicit, `E = ROE`.
- **AC9 Lineup Gen:** Exactly 9 batters per team; names `"[Team] 1"... "[Team] 9"`; seed-random order; 9th `(DH)`.
- **AC10 Seed Display:** Generated when omitted; appears in header and footer.

### Output Format
- **OV1 Line Score:** 9 inning columns; right-aligned; `X` when B9 skipped; R/H/E totals accurate.
- **OV2 Play Log:** Exactly one line per PA; fixed phrases; chronological.
- **OV3 Box Scores:** All 9 batters in order; numeric right-aligned; totals present; IP thirds.
- **OV4 LogHash:** SHA-256 of **play-log lines only** (exclude header/timestamp, line-score, totals, box scores, inning headers); 64 hex; stable per seed.

### Edge Cases
- **E1 0–0 tie:** Allowed; LOB may be non-zero.
- **E2 High scoring:** Handle double digits; formatting holds.
- **E3 Perfect game:** Offensive zeros; pitching `9.0 IP, 27 BF`.
- **E4 Forced runs:** Consecutive BB/HBP with bases loaded handled; team LOB correct.

---

## 8. Resolved Decisions (formerly Open Questions)

- **Pitcher naming:** `"[Team] P"` (e.g., `Sharks P`). **Locked.**
- **Tie messaging:** Append `(TIE)` to final line. **Locked.**
- **LogHash scope:** **Only** play-log lines; exclude inning headers. **Locked.**
- **Error notation:** Include fielder position, e.g., `Reaches on E6`. **Locked.**
- **Timestamp format:** `YYYY-MM-DD HH:MM TZ` (no seconds). **Locked.**
- **Lineup display:** No separate section; implicit via box-score order. **Locked.**
- **Sacrifice fly:** Phrase `Sacrifice fly to [OF]. R3 scores.`; AB excluded; RBI=1. **Locked.**
- **DP/TP notation:** Standard numbers, e.g., `Grounds into DP 6-4-3`. **Locked.**

---

## 9. Risks & Future Work

### Risks
- **RNG quality:** Use a well-vetted RNG; affects realism. (Medium)
- **Rare edge cases:** Covered by tests; monitor. (Medium)
- **Output parsing rigidity:** Intentional for v1; mitigated by strict spec. (Low)

### Future Work (Post‑v1)
- Extra innings; bullpen and substitutions; advanced stats (W/L/S/BS, BA/OBP/SLG); pitch counts.
- Lineup customization; in-game events (SB/CS, balks, WP/PB, pickoffs).
- Alternative outputs (JSON/CSV/HTML) and park effects; pitch-by-pitch; historical replay; interactive mode.

---

## Appendix A: Box Score Calculation Rules

**Batting**
- `AB = PA - BB - HBP - SF`.
- RBI: ROE=0; BL BB/HBP=1; SF=1; HR all; walk-off clamp.
- LOB: team LOB is per half-inning snapshot; individual LOB per batter is displayed.

**Pitching**
- IP in thirds `.0/.1/.2`.
- ER excludes runs scoring solely due to errors.
- H excludes ROE.

---

## Appendix B: State Machine Summary

| State | Description | Entry | Exit |
|---|---|---|---|
| PreGame | Initial setup | Program start | Initialization complete |
| InningInProgress | Active play | Half-inning start | 3 outs or walk-off |
| InningTransition | Between halves | Half-inning end | Next half or game end |
| GameComplete | Final state | End condition met | Report emitted |

---

## Appendix C: Outcome Set

**Modeled (v1):** K, BB, HBP, InPlayOut, 1B, 2B, 3B, HR, ROE, SF, DP, TP.
**Not Modeled (v1):** IBB, pickoffs, balks, WP, PB, SB/CS, catcher interference.

---

## Document Control

**Approvals:** Product Owner; Technical Lead; QA Lead
**Change Log:**
- v1.1 (2025-10-28): Resolved all open questions; locked formatting and phrasing.
- v1.0 (2025-10-28): Initial draft based on PRP requirements.


---

# ADDENDUM: Implementation and Test Deliverables (v1.2)

# Product Requirements Document: Single-Game Simulator & Text Report — v1 Simple

**Document Version:** 1.2 (Implementation + Tests)
**Created:** 2025-10-28
**Status:** Ready for Implementation

---

> This version adds explicit **implementation deliverables** and a **test plan** so Roo (or a dev) produces code **and** tests, not just prose.

## 0. Implementation Deliverables (Code + Tests)

### D0. Language & Runtime
- **C# / .NET 8** (align with existing DiamondSim stack).

### D1. Source Deliverables
- Game loop implementation that satisfies Sections 4–6.
- Text report formatter that produces the exact shapes specified (stable phrasing).
- CLI entry that accepts `--home`, `--away`, `--seed` and prints the report to STDOUT (and exits with codes per spec).

### D2. Test Deliverables
Provide an automated test suite that covers **unit**, **integration (snapshot)**, and **contract** tests:

1. **Unit tests (logic-level)**
   - **Walk-off clamp (non‑HR):** minimum runs needed; `LOB=0`; game ends.
   - **Walk-off HR:** all runners + batter score; `LOB=0`; game ends.
   - **Skip Bottom 9th:** when Home leads after Top 9; line score shows `X`.
   - **RBI attribution:** ROE=0; BL BB/HBP=1; SF=1; HR=all; non‑HR walk‑off clamped.
   - **LOB instant-of-third-out:** snapshot taken before post-play state reset.
   - **Error tally:** `E = ROE + explicit fielding errors`; if none emitted, equals ROE.
   - **IP thirds:** `.0/.1/.2` mapping for 0/1/2 outs in an inning.

2. **Integration tests (snapshot-style)**
   - Run complete games with fixed seeds and team names and assert:
     - **Byte-for-byte** equality of the entire **report body** (excluding timestamp line), or
     - Equality of a canonical **`LogHash`** plus key totals (`R/H/E`, LOB).
   - Provide at least **5 seeds**:
     - `42` (low scoring), `8675309` (balanced), `12345` (HR present), `20251028` (errors present), `314159` (walk‑off case).
   - For each seed, assert:
     - Final score, line score, totals, and `LogHash` match the stored baseline.
   - Baselines live under a `__snapshots__` folder (text files).

3. **Contract tests (CLI behavior)**
   - Missing `--home` or `--away` → usage to **STDERR**; exit code **2**.
   - Bad lineup attempts (none in v1) are ignored; lineup always auto‑generated.
   - Unknown flag → usage; exit code **2**.
   - Unexpected exceptions → exit code **1**.

### D3. CI Hook
- Ensure `dotnet test` runs the full suite and fails on any snapshot drift.

### D4. Non‑Goals (implementation)
- No JSON/CSV export; no park effects; no bullpen/subs; no pitch-by-pitch.

---

## 1–9. (Unchanged Core Spec)

All requirements from **v1.1 (Review‑Locked)** remain authoritative:
- Determinism & RNG scope (Sec. 4)
- State machine & game flow (Sec. 5)
- Output shapes & phrasing (Sec. 6)
- Acceptance criteria & edge cases (Sec. 7)
- Resolved decisions (Sec. 8)
- Risks/Future work (Sec. 9)

> If any conflict arises, **v1.1** semantics win; this section only dictates deliverables and tests to prove compliance.

---

## 10. Test Plan Details (How to Verify)

### T1. Snapshot Normalization
- Timestamp line is **excluded** from comparisons.
- Normalize newlines to `\n` before hashing or comparing.
- `LogHash` = SHA‑256 over **play‑log lines only** (no header, line score, totals, box scores, inning headers).

### T2. Example Snapshot File Layout
```
__snapshots__/
  Report_seed_42.txt
  Report_seed_8675309.txt
  Report_seed_12345.txt
  Report_seed_20251028.txt
  Report_seed_314159.txt
```

### T3. Example Contract Assertions
- Usage text contains the flags table and the line: `DH: ON | Extras: OFF (tie allowed)`
- On success, process exits **0**.

### T4. Metric Guardrails (non‑blocking in v1)
- Optional: warn (don’t fail) if K%, BB%, HR% across 100 games deviate by >2× league SDs.
- This is advisory only; not a gate in v1.

---

## 11. Developer Hints (Non‑binding)
- Use a **single RNG** instance, passed through PA resolver and lineup shuffle.
- Keep phrasing centralized in one formatter to avoid drift.
- Build a tiny helper to compute `LogHash` from the emitted play‑log buffer.

---

## 12. Change Log
- **v1.2 (2025‑10‑28):** Added concrete code + test deliverables, snapshot strategy, and CI note.
- **v1.1 (2025‑10‑28):** Review‑locked spec; resolved phrasing and format decisions.
- **v1.0 (2025‑10‑28):** Initial draft.


---

## Related Architecture Documentation

**Implementation Architecture:** [Architecture Document](Architecture/ARCHITECTURE_GAME_LOOP_V1.md)

**Status:** ✅ Implementation Complete
- All core components implemented
- 150/150 tests passing
- Game simulator functional
- Test coverage: 75% (see [TEST_COVERAGE_REPORT.md](../TEST_COVERAGE_REPORT.md))

**Date Completed:** 2025-10-29
