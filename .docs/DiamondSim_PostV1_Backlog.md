# DiamondSim Post-V1 Backlog

> Checklist of outstanding items (edge-case tests removed).
> Basic postgame summary ships with the game loop.

---

## Game Loop & CLI
- [x] Single-game loop + CLI runner (seeded; deterministic)
- [x] Basic postgame summary (with loop): final score, line score, LOB, batting box (core), team pitching line (R/ER/UER), scoring summary
- [x] Important: Implement .docs\TEST_COVERAGE_REPORT.md
- [x] Allow for more than 9 innings. No ties
- [ ] CLI flags: --seed, --out <file>, --playbyplay none|scoring|full, --format text|json

## Pitching Systems
- [ ] Pitcher substitutions (v1): active pitcher per side, 3-batter minimum, gates at start-of-half / between PAs (no mid-PA emergencies)
- [ ] Inherited runners ownership (per-runner ownerPitcherId) + per-pitcher R/ER allocation
- [ ] Decisions ledger: W/L/S/BS based on lead-change and finishing pitcher rules
- [ ] Fatigue model: pitch count/stamina decay + between-game recovery; hook into sub logic
- [ ] Bullpen AI (minimal): choose relievers by fatigue/matchup/leverage

## Scoring
- [ ] Properly attribute runs to players. (.docs\box_score_runs_limitation.md)
- [ ] Properly attribute LOBS (left on base to players)

## Lineups, DH & Substitutions
- [ ] Pinch-hit / pinch-run / defensive replacements with lineup integrity
- [ ] DH support & variants (toggle), including DH loss rules on defensive switches
- [ ] Random lineup generator (seeded) for demo; later: role-aware ordering
- [ ] Defensive positions & ratings (validation + impact on errors/range later)

## Rules & Scoring Fidelity (beyond v1)
- [ ] GIDP RBI rule: no RBI on run-scoring double plays
- [ ] Sacrifice fly guards: < 2 outs + “ordinary effort” assumption; RBI=1; ER/UER per error involvement
- [ ] Inside-the-park walk-offs policy: classify as HR (all runs) vs hit+errors (clamp); document & test
- [ ] Advanced earned-run reconstruction (v2): true OBR rebuild across prior errors/“should-have-been” outs
- [ ] Fielder’s choice, interference/obstruction, appeal plays, infield fly (outcomes + scoring rules)
- [ ] Explicit error events (E#) and fielding ratings influencing error probability

## Baserunning & Situational
- [ ] SB/CS, pickoffs, WP/PB (if desired)
- [ ] Tag-up logic for SF; simple advancement risk model (later)
- [ ] Close calls, should runners 'go for it'

## Probability / Physics Tuning
- [ ] League-fit benchmarks vs MLB 2023/2024 (K%, BB%, HR%, BABIP, XBH mix, R/G) with cited sources
- [ ] Park factors (HR/2B/3B/run env modifiers)
- [ ] Platoon splits (L/R adjustments)
- [ ] Optional: weather/altitude modifiers

## Engine Robustness
- [ ] Impossible-state guards (can happen now): validate/throw on outs > 3 in half, duplicate base occupancy, negative scores, etc.
- [ ] Determinism harness: seed logging, replay snapshot, 100x reproducibility check
- [ ] Config surface: expose key knobs (probabilities, rules toggles) via config, not compile-time

## Data, Output & Persistence
- [ ] JSON/CSV exports for box/line scores and play log
- [ ] Per-pitcher splits in reports once substitutions/ownership land
- [ ] Season/series mode (later): schedules, standings, cumulative stats

## Documentation
- [ ] Algorithm deck (updated with walk-off HR exception, clamp order)
- [ ] PRD/PRP library for: subs, ownership/decisions, fatigue, baserunning
- [ ] Config reference & rule presets (DH on/off, ghost runner toggle, era settings)
