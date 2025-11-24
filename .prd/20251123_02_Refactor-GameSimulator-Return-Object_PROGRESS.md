# PRD Progress: Refactor GameSimulator to Return Rich Object (TDD)

**Original PRD:** `.prd/20251123_02_Refactor-GameSimulator-Return-Object.md`
**Date Started:** 2025-11-23
**Last Updated:** 2025-11-23 20:59 UTC
**Status:** ✅ Phases 1-5 Complete (Core Model + RunGameV2 Working!)

## ✅ COMPLETED PHASES (1-4)

### Phase 1: GameMetadata ✅
- ✅ Test: Constructor with valid data
- ✅ Implementation: Minimal code to pass
- ✅ Test: Null validation (homeTeam, awayTeam)
- ✅ Implementation: Null checks added
- **Design Decision:** Removed `logHash` parameter - will be calculated by GameResult from PlayLog
- **Files Created:**
  - `src/DiamondSim/GameMetadata.cs`
  - `tests/DiamondSim.Tests/Model/GameMetadataTests.cs`
- **Tests Passing:** 3/3

### Phase 2: TeamLineup ✅
- ✅ Test: Constructor with valid data
- ✅ Implementation: Minimal code to pass
- ✅ Test: Null validation + batter count validation (must be 9)
- ✅ Implementation: Validation added
- **Files Created:**
  - `src/DiamondSim/TeamLineup.cs`
  - `tests/DiamondSim.Tests/Model/TeamLineupTests.cs`
- **Tests Passing:** 4/4

### Phase 3: PlayLogEntry ✅
- ✅ Test: Constructor with valid data
- ✅ Implementation: Minimal code to pass
- ✅ Test: Null validation (batterName, pitchingTeamName, resolution)
- ✅ Implementation: Null checks added
- **Design Decision:** Pure data class - NO `ToDisplayString()` method (formatting stays in GameReportFormatter)
- **Files Created:**
  - `src/DiamondSim/PlayLogEntry.cs`
  - `tests/DiamondSim.Tests/Model/PlayLogEntryTests.cs`
- **Tests Passing:** 4/4

### Phase 4: GameResult ✅
- ✅ Test: Constructor with all 7 properties
- ✅ Implementation: Minimal code to pass
- ✅ Test: Null validation for ALL 7 parameters
- ✅ Implementation: Comprehensive null checks
- **Files Created:**
  - `src/DiamondSim/GameResult.cs`
  - `tests/DiamondSim.Tests/Model/GameResultTests.cs`
- **Tests Passing:** 8/8

## 📊 STATISTICS

- **Total New Tests:** 20 (all passing ✅)
- **TDD Discipline:** 100% - Every line driven by failing test first
- **Test Files:** 4 new test files
- **Production Files:** 4 new model classes
- **Architecture:** Pure data classes with immutable properties

## 🏗️ KEY ARCHITECTURAL DECISIONS

1. **Pure Data Classes:** All model classes are immutable data containers (init-only properties)
2. **No Formatting Logic:** Model classes contain NO display methods (SRP - Single Responsibility)
3. **LogHash Calculation:** Moved from GameMetadata to GameResult (calculated from PlayLog)
4. **Comprehensive Validation:** All reference-type constructor parameters validated for null
5. **Test Helper Usage:** Leveraged existing `GameStateTestHelper` for consistency

## ✅ COMPLETED PHASES (1-5)

### Phase 5: RunGameV2() Implementation ✅
- ✅ Test: RunGameV2() returns non-null GameResult
- ✅ Test: Metadata has correct team names and seed
- ✅ Test: Game completes successfully (IsFinal, no ties, inning>=9)
- ✅ Test: Lineups are injected correctly (TestLineupGenerator)
- ✅ Test: Determinism (same seed = same LogHash)
- ✅ Implementation: Full game simulation with PlayLogEntry objects
- **Design Decision:** Added ILineupGenerator injection for better testability
- **Files Created:**
  - `src/DiamondSim/ILineupGenerator.cs`
  - `src/DiamondSim/DefaultLineupGenerator.cs`
  - `tests/DiamondSim.Tests/TestHelpers/TestLineupGenerator.cs`
  - `tests/DiamondSim.Tests/GameLoop/GameSimulatorV2Tests.cs`
- **Tests Passing:** 6/6 (GameSimulatorV2Tests)

### Phase 5C: LogHash Implementation ✅
- ✅ Added LogHash calculated property to GameResult
- ✅ LogHash = SHA-256(PlayLog + FinalScore)
- ✅ Test: LogHash is valid 64-char hex
- ✅ Test: Same PlayLog + Score = same LogHash
- **Files Modified:**
  - `src/DiamondSim/GameResult.cs` - Added LogHash property
  - `tests/DiamondSim.Tests/Model/GameResultTests.cs` - Added 2 LogHash tests
- **Tests Passing:** 2/2 (LogHash tests)

## 📋 REMAINING WORK (Phases 6-8)

### Phase 6: Backward Compatibility
- [ ] Test: RunGame() still works
- [ ] Implementation: RunGame() as wrapper calling RunGameV2().ToConsoleReport()
- [ ] Run all 266+ existing tests to ensure no regressions

### Phase 7: ToConsoleReport Extension
- [ ] Test: ToConsoleReport() extension method
- [ ] Implementation: Extension method
- [ ] Refactor: Update GameReportFormatter to accept GameResult

### Final Cleanup
- [ ] Run full test suite
- [ ] Organize Model classes into `src/DiamondSim/Model/` subfolder with proper namespace

## 📁 FILES CREATED

**Production Code:**
- `src/DiamondSim/GameMetadata.cs` - Game setup metadata
- `src/DiamondSim/TeamLineup.cs` - Team roster (9 batters)
- `src/DiamondSim/PlayLogEntry.cs` - Single play-by-play entry
- `src/DiamondSim/GameResult.cs` - Composite result object

**Test Code:**
- `tests/DiamondSim.Tests/Model/GameMetadataTests.cs` (3 tests)
- `tests/DiamondSim.Tests/Model/TeamLineupTests.cs` (4 tests)
- `tests/DiamondSim.Tests/Model/PlayLogEntryTests.cs` (4 tests)
- `tests/DiamondSim.Tests/Model/GameResultTests.cs` (8 tests)

## 📊 STATISTICS (Updated)

- **Total Tests:** 293 (all passing ✅)
  - 287 existing tests (unchanged)
  - 6 new GameSimulatorV2 tests
- **TDD Discipline:** 100% - Every line driven by failing test first
- **Test Files:** 6 test files (4 model + 2 game loop)
- **Production Files:** 7 files (4 model + 3 generators/interfaces)

## 🎯 NEXT TASK PROMPT

```
Continue implementing .prd/20251123_02_Refactor-GameSimulator-Return-Object.md using strict TDD.

COMPLETED:
- Phases 1-4: Core Data Model (GameMetadata, TeamLineup, PlayLogEntry, GameResult)
- Phase 5A-C: RunGameV2() working with full simulation, lineup injection, LogHash
- 293 tests passing

NEXT: Port remaining tests from GameLoopTests.cs to test RunGameV2() GameResult properties

Tests to Port (from GameLoopTests.cs):
1. BoxScore_AtBatCalculation_ExcludesWalksHbpAndSacFlies → Test BoxScore.AB calculation
2. BoxScore_InningsPitched_UsesThirdsNotation → Test pitcher IP in BoxScore
3. LineScore_HasNineInningColumns → Test LineScore structure
4. LineScore_ShowsRunsHitsErrors → Test LineScore data
5. FinalScore_MatchesLineScoreTotals → Test FinalState matches LineScore
6. PlayLog_EachLineHasRequiredComponents → Test PlayLogEntry properties
7. PlayLog_OutsPhrase_ShowsCorrectCount → Test outsAfter in PlayLogEntry

Then:
- Phase 6: Make RunGame() wrapper calling RunGameV2().ToConsoleReport()
- Phase 7: Implement ToConsoleReport() extension method
- Cleanup: Remove old GenerateLineup/Shuffle methods, organize files

Follow strict TDD - write test, see it fail/pass, refactor.
All 293 tests must stay green.
```

## 📝 NOTES

- TDD discipline was strictly followed throughout Phases 1-4
- User provided valuable feedback on design decisions (LogHash removal, pure data classes)
- GameState validation caught a test bug (outs=3 invalid, changed to outs=0)
- Used existing test helpers (GameStateTestHelper) for consistency
