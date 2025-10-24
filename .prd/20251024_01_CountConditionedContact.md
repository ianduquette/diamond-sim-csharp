# Product Requirements Document: Count-Conditioned Contact

**Document ID:** PRD-20251024-01
**Feature Name:** Count-Conditioned Contact Probability
**Created:** 2025-10-24
**Status:** Draft
**Priority:** High

---

## 1. Executive Summary

This PRD defines the requirements for adding count awareness to the DiamondSim baseball simulator. The feature will enable the simulation to adjust contact probability based on the current count (balls and strikes), making the simulation more realistic by reflecting how hitters' approach and success rates vary with different counts.

### Key Benefits
- **Increased Realism:** Simulations will better reflect real baseball dynamics where hitters are more aggressive in favorable counts and more defensive in unfavorable counts
- **Enhanced Analytics:** Enables analysis of count-specific strategies and outcomes
- **Foundation for Future Features:** Provides infrastructure for additional count-based mechanics (e.g., pitch selection, swing decisions)

---

## 2. Background & Context

### Current State
The DiamondSim project is a .NET 8 C# baseball simulator that currently:
- Simulates pitch-by-pitch outcomes between average batters and pitchers
- Maintains contact rates between 0.70 and 0.85 for neutral counts (validated by `AtBatTests`)
- Does not differentiate contact probability based on the current count

### Problem Statement
Real baseball exhibits clear patterns in contact rates based on count:
- **Pitcher's counts (e.g., 0-2):** Hitters are defensive, protecting the plate, resulting in lower contact quality and rates
- **Neutral counts (e.g., 0-0, 1-1):** Baseline contact rates
- **Hitter's counts (e.g., 2-0, 3-1):** Hitters can be selective and aggressive on good pitches, resulting in higher contact rates

The current simulator does not model this fundamental aspect of baseball strategy and outcomes.

### Existing Infrastructure
The codebase already includes:
- [`Probabilities.CountContactAdjust()`](src/DiamondSim/Probabilities.cs) - A method designed to apply count-based adjustments
- [`GameEngine.SimulatePitchContact()`](src/DiamondSim/GameEngine.cs) - Core contact simulation logic
- [`AtBatTests`](tests/DiamondSim.Tests/AtBatTests.cs) - Existing test suite validating contact rates

---

## 3. Goals & Objectives

### Primary Goals
1. Introduce count tracking capability to the simulation engine
2. Implement count-based contact probability adjustments
3. Validate that contact rates follow expected monotonic trends across counts

### Success Metrics
- All existing tests remain green (no regression)
- New tests demonstrate statistically significant differences in contact rates across counts
- Contact rates follow the expected pattern: 0-2 < 0-0 < 2-0

### Non-Goals (Out of Scope)
- Pitch selection logic based on count
- Ball/strike outcome probability adjustments based on count
- Individual player count-specific tendencies
- Historical count distribution analysis

---

## 4. Functional Requirements

### FR-1: Game State Tracking
**Priority:** P0 (Critical)

**Description:** Implement a `GameState` class to track the current count during an at-bat.

**Requirements:**
- FR-1.1: Create a new [`GameState`](src/DiamondSim/GameState.cs) class in `src/DiamondSim/`
- FR-1.2: The class must track:
  - `Balls` (int, 0-3)
  - `Strikes` (int, 0-2)
- FR-1.3: Provide validation to ensure counts remain within legal ranges
- FR-1.4: Include a method to determine if the count is complete (walk or strikeout)
- FR-1.5: Provide a clear string representation for debugging (e.g., "2-1")

**Acceptance Criteria:**
- `GameState` can be instantiated with valid ball and strike counts
- Invalid counts (e.g., 4 balls, 3 strikes) are rejected or handled appropriately
- State can be queried for current balls and strikes

---

### FR-2: Count-Aware Contact Simulation
**Priority:** P0 (Critical)

**Description:** Extend [`GameEngine`](src/DiamondSim/GameEngine.cs) to accept count information and adjust contact probability accordingly.

**Requirements:**
- FR-2.1: Add an overload to [`GameEngine.SimulatePitchContact()`](src/DiamondSim/GameEngine.cs) that accepts `(int balls, int strikes)` parameters
- FR-2.2: The new overload must call [`Probabilities.CountContactAdjust(balls, strikes)`](src/DiamondSim/Probabilities.cs) to obtain the count-based adjustment factor
- FR-2.3: Apply the adjustment factor to the base contact probability
- FR-2.4: Maintain the existing parameterless overload for backward compatibility
- FR-2.5: Ensure the adjustment preserves deterministic behavior when using seeded RNG

**Acceptance Criteria:**
- The new overload correctly applies count-based adjustments
- Existing code using the parameterless overload continues to work unchanged
- Contact probability increases in hitter's counts and decreases in pitcher's counts

---

### FR-3: Count-Based Contact Testing
**Priority:** P0 (Critical)

**Description:** Create comprehensive tests to validate count-conditioned contact behavior.

**Requirements:**
- FR-3.1: Create a new test file [`CountContactTests.cs`](tests/DiamondSim.Tests/CountContactTests.cs) in `tests/DiamondSim.Tests/`
- FR-3.2: Implement test: `ContactRate_VariesMonotonically_AcrossKeyCounts()`
  - Test counts: 0-2 (pitcher's count), 0-0 (neutral), 2-0 (hitter's count)
  - Run sufficient iterations (e.g., 10,000) for statistical significance
  - Assert: ContactRate(0-2) < ContactRate(0-0) < ContactRate(2-0)
- FR-3.3: Use deterministic RNG seeding for reproducibility
- FR-3.4: Include tolerance ranges appropriate for stochastic simulation
- FR-3.5: Add tests for edge cases (3-0, 0-2, 3-2 counts)

**Acceptance Criteria:**
- Tests pass consistently with seeded RNG
- Statistical differences are significant (not due to random variance)
- Tests clearly document expected behavior and tolerances

---

## 5. Technical Design

### 5.1 Architecture Overview

```
┌─────────────────┐
│   GameEngine    │
│                 │
│ SimulatePitch   │──┐
│ Contact()       │  │
│                 │  │  Uses count
│ SimulatePitch   │  │  to adjust
│ Contact(b,s)    │◄─┘  probability
└────────┬────────┘
         │
         │ Calls
         ▼
┌─────────────────┐      ┌──────────────┐
│  Probabilities  │      │  GameState   │
│                 │      │              │
│ CountContact    │      │ Balls: int   │
│ Adjust(b,s)     │      │ Strikes: int │
└─────────────────┘      └──────────────┘
```

### 5.2 File Changes

#### New Files
1. **[`src/DiamondSim/GameState.cs`](src/DiamondSim/GameState.cs)**
   - Purpose: Track current count state
   - Key members: `Balls`, `Strikes`, validation logic

2. **[`tests/DiamondSim.Tests/CountContactTests.cs`](tests/DiamondSim.Tests/CountContactTests.cs)**
   - Purpose: Validate count-conditioned contact behavior
   - Key tests: Monotonic trend validation, edge cases

#### Modified Files
1. **[`src/DiamondSim/GameEngine.cs`](src/DiamondSim/GameEngine.cs)**
   - Add: `SimulatePitchContact(int balls, int strikes)` overload
   - Modify: Internal logic to apply count adjustments

2. **[`src/DiamondSim/Probabilities.cs`](src/DiamondSim/Probabilities.cs)** (if needed)
   - Verify: `CountContactAdjust()` implementation is correct
   - Document: Expected adjustment ranges

### 5.3 Implementation Approach

**Phase 1: Foundation**
1. Create [`GameState`](src/DiamondSim/GameState.cs) class with validation
2. Add unit tests for `GameState` (optional but recommended)

**Phase 2: Integration**
3. Add count-aware overload to [`GameEngine.SimulatePitchContact()`](src/DiamondSim/GameEngine.cs)
4. Integrate with [`Probabilities.CountContactAdjust()`](src/DiamondSim/Probabilities.cs)

**Phase 3: Validation**
5. Create [`CountContactTests.cs`](tests/DiamondSim.Tests/CountContactTests.cs)
6. Run full test suite to ensure no regressions
7. Validate monotonic contact trends

---

## 6. Acceptance Criteria

### AC-1: Backward Compatibility
- ✅ All existing tests in [`AtBatTests.cs`](tests/DiamondSim.Tests/AtBatTests.cs) pass without modification
- ✅ Existing code using parameterless `SimulatePitchContact()` continues to work

### AC-2: Count-Based Behavior
- ✅ Contact rate at 0-2 count is statistically lower than at 0-0 count
- ✅ Contact rate at 2-0 count is statistically higher than at 0-0 count
- ✅ Contact rate at 0-0 count falls within existing validated range (0.70-0.85)

### AC-3: Deterministic Testing
- ✅ Tests use seeded RNG for reproducibility
- ✅ Tests pass consistently across multiple runs
- ✅ Statistical significance is achieved with appropriate sample sizes

### AC-4: Code Quality
- ✅ New code follows existing project conventions
- ✅ XML documentation comments are provided for public APIs
- ✅ No compiler warnings or errors
- ✅ Code is readable and maintainable

---

## 7. Testing Strategy

### 7.1 Unit Tests
- **[`GameState`](src/DiamondSim/GameState.cs) validation:** Test valid/invalid count combinations
- **Count adjustment logic:** Verify [`Probabilities.CountContactAdjust()`](src/DiamondSim/Probabilities.cs) returns expected ranges

### 7.2 Integration Tests
- **Monotonic trend test:** Primary validation in [`CountContactTests.cs`](tests/DiamondSim.Tests/CountContactTests.cs)
  - Sample size: 10,000+ iterations per count
  - Seeded RNG: Use fixed seed for reproducibility
  - Assertions: Verify ContactRate(0-2) < ContactRate(0-0) < ContactRate(2-0)

### 7.3 Regression Tests
- **Existing test suite:** All tests in [`AtBatTests.cs`](tests/DiamondSim.Tests/AtBatTests.cs) must pass
- **Baseline behavior:** Parameterless overload produces same results as before

### 7.4 Edge Case Tests
- **Extreme counts:** 3-0, 0-2, 3-2
- **Boundary validation:** Ensure counts don't exceed legal limits
- **Zero iterations:** Handle edge case of zero sample size gracefully

---

## 8. Risks & Mitigation

### Risk 1: Test Flakiness
**Description:** Stochastic simulations may produce inconsistent results due to random variance.

**Likelihood:** Medium
**Impact:** Medium

**Mitigation:**
- Use deterministic RNG with fixed seeds
- Run sufficient iterations (10,000+) for statistical significance
- Define appropriate tolerance ranges in assertions
- Document expected variance in test comments

---

### Risk 2: Adjustment Factor Tuning
**Description:** The adjustment factors in [`Probabilities.CountContactAdjust()`](src/DiamondSim/Probabilities.cs) may not reflect realistic baseball behavior.

**Likelihood:** Medium
**Impact:** Low

**Mitigation:**
- Start with conservative adjustment factors
- Document the source of adjustment values (research, data, assumptions)
- Make adjustment factors easily configurable for future tuning
- Consider adding configuration file for probability adjustments

---

### Risk 3: Performance Impact
**Description:** Additional count tracking and calculations may impact simulation performance.

**Likelihood:** Low
**Impact:** Low

**Mitigation:**
- Keep `GameState` lightweight (simple value type or struct)
- Avoid unnecessary allocations in hot paths
- Profile performance if concerns arise
- Consider caching adjustment factors if calculations are expensive

---

### Risk 4: Breaking Changes
**Description:** Modifications to [`GameEngine`](src/DiamondSim/GameEngine.cs) could break existing functionality.

**Likelihood:** Low
**Impact:** High

**Mitigation:**
- Maintain existing parameterless overload unchanged
- Run full regression test suite before merging
- Use method overloading rather than modifying existing signatures
- Comprehensive code review

---

## 9. Implementation Notes

### 9.1 RNG Seeding
- All tests must use seeded RNG for reproducibility
- Document seed values in test code
- Consider exposing seed as a test parameter for debugging

### 9.2 Statistical Significance
- Use appropriate sample sizes (recommend 10,000+ iterations)
- Consider implementing statistical tests (e.g., chi-squared) for validation
- Document expected effect sizes and confidence intervals

### 9.3 Realism Tuning
- Initial implementation should use conservative adjustments
- Document assumptions about adjustment factors
- Plan for future iteration based on validation against real baseball data
- Consider making adjustments configurable via external data files

### 9.4 Code Organization
- Keep [`GameState`](src/DiamondSim/GameState.cs) simple and focused on state tracking
- Separate concerns: state tracking vs. probability calculations
- Follow existing project patterns and conventions

---

## 10. Future Enhancements (Out of Scope)

The following features are explicitly out of scope for this PRD but may be considered in future iterations:

1. **Player-Specific Count Tendencies:** Different contact adjustments per player
2. **Pitch Selection Logic:** Count-based pitch type selection
3. **Ball/Strike Probability Adjustments:** Count-aware ball/strike outcomes
4. **Historical Count Analysis:** Tools to analyze count distributions
5. **Advanced Count States:** Tracking full at-bat history beyond current count
6. **Count-Based Swing Decisions:** Whether to swing based on count
7. **Umpire Tendencies:** Count-based strike zone variations

---

## 11. Dependencies

### Internal Dependencies
- Existing [`Probabilities.CountContactAdjust()`](src/DiamondSim/Probabilities.cs) implementation
- Current [`GameEngine`](src/DiamondSim/GameEngine.cs) architecture
- NUnit test framework

### External Dependencies
- .NET 8 SDK
- NUnit 3.x test framework
- No new external dependencies required

---

## 12. Documentation Requirements

### Code Documentation
- XML documentation comments for all public APIs
- Inline comments explaining adjustment logic
- Example usage in method documentation

### Test Documentation
- Clear test names describing what is being validated
- Comments explaining statistical approach and tolerances
- Documentation of RNG seeding strategy

### User Documentation
- Update README if applicable
- Document new API surface in code comments
- Provide examples of count-aware simulation usage

---

## 13. Success Criteria Summary

This feature will be considered successfully implemented when:

1. ✅ [`GameState`](src/DiamondSim/GameState.cs) class is created and tracks count correctly
2. ✅ [`GameEngine.SimulatePitchContact(int, int)`](src/DiamondSim/GameEngine.cs) overload is implemented
3. ✅ [`CountContactTests.cs`](tests/DiamondSim.Tests/CountContactTests.cs) validates monotonic contact trends
4. ✅ All existing tests pass without modification
5. ✅ New tests pass consistently with seeded RNG
6. ✅ Code follows project conventions and is well-documented
7. ✅ No performance regressions are observed

---

## 14. Appendix

### A. Count Definitions
- **Pitcher's Count:** More strikes than balls (e.g., 0-2, 1-2)
- **Neutral Count:** Equal balls and strikes (e.g., 0-0, 1-1, 2-2)
- **Hitter's Count:** More balls than strikes (e.g., 2-0, 3-1)

### B. Expected Contact Rate Ranges (Approximate)
- **0-2 Count:** 0.60-0.75 (lower than baseline)
- **0-0 Count:** 0.70-0.85 (baseline, per existing tests)
- **2-0 Count:** 0.75-0.90 (higher than baseline)

*Note: Exact ranges will be determined by [`Probabilities.CountContactAdjust()`](src/DiamondSim/Probabilities.cs) implementation and validated through testing.*

### C. References
- Existing test suite: [`tests/DiamondSim.Tests/AtBatTests.cs`](tests/DiamondSim.Tests/AtBatTests.cs)
- Probability utilities: [`src/DiamondSim/Probabilities.cs`](src/DiamondSim/Probabilities.cs)
- Game engine: [`src/DiamondSim/GameEngine.cs`](src/DiamondSim/GameEngine.cs)

---

**Document End**
