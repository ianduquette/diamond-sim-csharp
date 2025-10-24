# Reference: Simulation & Randomness Design

This document summarizes how the at-bat simulation in **DiamondSim** works and why it’s built this way.

## Location
Full technical explanation: [`docs/simulation_design.md`](simulation_design.md)

## Key Points
- **Pitch-by-pitch loop** that ends in Strikeout, Walk, or Ball-in-Play.
- **Probability knobs** control how often pitches land in the zone, how often hitters swing, and how fouls behave.
- **Ratings (0–100)** translate to small shifts in probability curves—50 = MLB average.
- **Seeded randomness** keeps results deterministic in tests and reproducible during tuning.
- **Target distributions** for average vs average:
  - K%: 18–28%
  - BB%: 7–12%
  - BIP%: 55–70%
- **Count-awareness** means hitters behave differently at 0–2 vs 2–0.
- **Constants** can be tuned slightly (±0.002–0.005) to stay inside these bands.

## Why This Exists
This summary is meant for inclusion in `.prd` files so new contributors or AI agents know what “realistic” means.

When referencing, use:
