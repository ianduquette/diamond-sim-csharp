# Implementation Prompts for DiamondSim PRDs

Quick prompts to give AI agents for implementing the PRDs.

---

## Prompt 1: GameResult Refactoring

```
Please implement .prd/20251123_02_Refactor-GameSimulator-Return-Object.md using strict TDD.

Follow the phases in order, write one test at a time, make it pass with minimal code, then refactor.
Run `dotnet test` after each change to ensure all 266+ tests stay green.
```

---

## Prompt 2: Individual Player Runs Tracking

```
Please implement .prd/20251123_03_Individual-Player-Runs-Tracking.md using strict TDD.

Add Batter references to BaseState, update all callers, track individual R stats.
Run `dotnet test` after each change to ensure all 266+ tests stay green.
```

---

## Prompt 3: Code Maintainability Improvements

```
Please address the High Priority items in .prd/20251123_04_Code-Maintainability-Audit.md:

1. Make GameState immutable (convert to record)
2. Add null checks to all public constructors

Use TDD approach. Run `dotnet test` after each change.
```

---

**Note:** Each PRD contains detailed implementation steps, test examples, and success criteria. The prompts above are intentionally brief - the PRDs have all the details.
