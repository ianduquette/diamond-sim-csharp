# DiamondSim — Simulation & Randomness Design

**Version:** 1.0  
**Date:** 2025-10-24  
**Audience:** Engineers and reviewers  
**Scope:** Plate appearance (PA) simulation from first pitch to terminal outcome. Subsequent layers (ball-in-play resolution, base/out state, innings, games) reference this document.

---

## 1) Purpose

DiamondSim models a plate appearance as a sequence of pitches with probabilities influenced by the batter’s and pitcher’s ratings and the current count. The design targets realistic aggregate distributions (K%, BB%, BIP%) while keeping the model small, fast, and testable.

---

## 2) Model Boundary (this layer)

**Included**
- Pitch location (zone vs. out-of-zone) influenced by **Pitcher.Control**.
- Swing decision influenced by **Batter.Patience** and pitch location.
- Contact decision influenced by **Batter.Contact**, **Pitcher.Stuff**, and **count**.
- Foul vs. ball-in-play (BIP) split with higher foul rates at two strikes.
- Terminal states: **Strikeout (K)**, **Walk (BB)**, **BallInPlay (BIP)**.

**Excluded (handled by later layers)**
- BIP outcome resolution (Out/1B/2B/3B/HR).
- Base/out state, runner advancement, scoring, innings, and full games.

---

## 3) Ratings & Scales

All ratings are integer **0–100**, where **50 ≈ MLB average**.

- **Batters:** `Contact`, `Power`, `Patience`, `Speed`
- **Pitchers:** `Control`, `Stuff`, `Stamina`, `Speed`

This layer uses: `Contact`, `Patience`, `Control`, `Stuff`. (Power/Speed/Stamina are not used here.)

---

## 4) Randomness & Seeding Policy

- RNG abstraction: `IRandomSource` → `SeededRandom(int seed)` in tests; a non-seeded or per-run-seeded RNG in simulations.
- **Why seed tests:** deterministic, non-flaky distribution checks; reproducible debugging.
- **Risk:** overtuning to one RNG path.
- **Mitigations:** large trial counts (e.g., 10k), tolerance bands, occasional seed sweeps for stress testing.

**Operational guidance**
- Tests: fixed seeds.
- Simulations/demos: vary or record seed per run.

---

## 5) At-Bat Algorithm

The PA is a Markov process with three absorbing states (K, BB, BIP). Per pitch:

1. **Zone decision (Control)**
   ```
   inZoneRate = BaseInZoneRate + ((Control - 50) / 50) * ControlAdjustment
   inZone     = RNG() < clamp01(inZoneRate)
   ```

2. **Swing decision (Patience, location)**
   ```
   if inZone:
       swingRate = InZoneSwingRate
   else:
       swingRate = clamp01(OutOfZoneSwingRate
                           - ((Patience - 50) / 50) * PatienceAdjustment)
   swing = RNG() < swingRate
   ```

3. **If take**
   - In zone → **Called Strike**
   - Out of zone → **Ball**

4. **If swing**
   - Contact:
     ```
     baseContact = ContactFromRatings(batter.Contact, pitcher.Stuff)
     countAdj    = CountContactAdjust(balls, strikes)
     contactProb = clamp01(baseContact + countAdj)
     contact     = RNG() < contactProb
     ```
   - If miss → **Swinging Strike**
   - If contact → **Foul or InPlay**:
     ```
     foulRate = (strikes == 2) ? FoulRateWithTwoStrikes : FoulRateOtherCounts
     foul     = RNG() < foulRate
     outcome  = foul ? Foul : InPlay
     ```

5. **Update count**
   - Ball → `balls++`
   - Swinging/Callee strike → `strikes++`
   - Foul → `strikes++` **only if** `strikes < 2`
   - InPlay → **terminal**

6. **Terminal checks**
   - `balls >= 3` → **Walk**
   - `strikes >= 2` → **Strikeout**
   - `InPlay` → **BallInPlay**

Safety: a maximum pitch limit (e.g., 50) asserts termination in tests.

---

## 6) Probability Knobs (tunable constants)

| Constant | Meaning | Increasing this tends to… |
|---|---|---|
| `BaseInZoneRate` | Baseline strike-zone rate at Control=50 | Reduce BB, slightly raise K |
| `ControlAdjustment` | Sensitivity of zone rate to Control | Spread BB by pitcher quality |
| `InZoneSwingRate` | Swing rate on strikes | Reduce called strikes; shifts K/BIP balance |
| `OutOfZoneSwingRate` | Baseline chase rate | Reduce BB, raise swinging strikes |
| `PatienceAdjustment` | Sensitivity of chase to Patience | Spread BB/K by batter quality |
| `FoulRateWithTwoStrikes` | Foul share with 2 strikes | Reduce BIP, lengthen ABs |
| `FoulRateOtherCounts` | Foul share otherwise | Reduce BIP overall |

**Match-up inputs**
- `ContactFromRatings(Contact, Stuff)` increases with batter Contact, decreases with pitcher Stuff.
- `CountContactAdjust(balls, strikes)` reduces contact at 2-strike counts and can increase it in hitter’s counts (e.g., 2–0).

---

## 7) Default Values (current calibration)

```csharp
const double BaseInZoneRate     = 0.575;
const double ControlAdjustment  = 0.14;
const double InZoneSwingRate    = 0.72;
const double OutOfZoneSwingRate = 0.228;
const double PatienceAdjustment = 0.22;
const double FoulRateWithTwoStrikes = 0.58;
const double FoulRateOtherCounts    = 0.43;
```

`Probabilities.ContactFromRatings` uses small per-point effects around a base contact near 0.78 at 0–0.  
`Probabilities.CountContactAdjust` enforces monotonicity (e.g., 0–2 ↓, 2–0 ↑).

---

## 8) Validation Targets

- **K%:** 0.18–0.28  
- **BB%:** 0.07–0.12  
- **BIP%:** 0.55–0.70  
- Sum ≈ 1.00 (±1e-4).

---

## 9) Tuning Procedure

1. Run tests (`dotnet test`).
2. Identify which metric misses (BB, K, or BIP).
3. Adjust most orthogonal knob:
   - BB → `BaseInZoneRate` or `OutOfZoneSwingRate`
   - K → `InZoneSwingRate` or `OutOfZoneSwingRate`
   - BIP → foul rates
4. Change small (±0.002–0.005).
5. Re-run tests, commit when inside band.

---

## 10) Test Policy

- Framework: NUnit  
- Determinism: Seeded RNG  
- Trials: 10k minimum  
- Focus: realistic trend, not exact precision  

---

## 11) Performance

Allocation-free and CPU-light. 10k ABs in ms.  
Use 5k in CI if needed.

---

## 12) Extensibility

Future layers:
- BIP → Out/1B/2B/3B/HR via Power & Stuff
- Base/out → advancement & scoring
- Game → innings, stats, consistency
- Configurable tuning

---

## 13) Glossary

**BIP**: Ball in play.  
**K%/BB%/BIP%**: Share of ABs ending in each terminal event.  
**Seed**: RNG starting point for reproducibility.

---

## 14) References

- Internal: `docs/simulation_design_reference.md`, `docs/simulation_design_for_kids.md`  
- External: MLB stats, Strat-O-Matic, APBA, Diamond Mind.
