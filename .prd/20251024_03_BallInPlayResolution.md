# Product Requirements Document: Ball-In-Play Resolution

**Document ID:** PRD-20251024-03
**Feature Name:** Ball-In-Play Resolution (BIP → Hit Type)
**Created:** 2025-10-24
**Status:** Draft
**Priority:** High

---

## 1. Executive Summary

This PRD defines the requirements for resolving **BallInPlay** outcomes from the at-bat simulator into specific hit types: **Out, Single, Double, Triple, and HomeRun**. The resolution will be influenced by batter **Power** and pitcher **Stuff** attributes, producing realistic aggregate distributions that match expected baseball statistics.

### Key Benefits
- **Complete Hit Resolution:** Transforms abstract BallInPlay outcomes into concrete game results
- **Attribute-Driven Outcomes:** Power and Stuff ratings meaningfully affect hit distributions
- **Realistic Statistics:** Produces HR%, 2B%, 3B%, Singles%, and BABIP within expected ranges
- **Deterministic Testing:** Fully testable with seeded RNG for consistent validation
- **Foundation for Scoring:** Enables calculation of runs, batting average, slugging percentage, etc.

---

## 2. Background & Context

### Current State
The DiamondSim project currently has:
- Count-conditioned contact probability (Part 1, completed)
- Complete at-bat loop producing terminal outcomes: K, BB, BIP (Part 2, completed)
- [`AtBatTerminal.BallInPlay`](src/DiamondSim/Outcomes.cs:50) enum value representing contact made
- [`AtBatResult`](src/DiamondSim/Outcomes.cs:59-63) record containing terminal outcome information

### Problem Statement
The simulator can currently determine when a ball is put in play, but it cannot:
- Distinguish between different types of hits (singles, doubles, triples, home runs)
- Determine when a ball in play results in an out
- Model how batter Power affects extra-base hit probability
- Model how pitcher Stuff affects the quality of contact
- Produce realistic distributions of hit types that match baseball statistics

### Baseball Context
In real baseball, when a ball is put in play:
1. **Contact Quality:** Determined by batter skill (Power, Contact) and pitcher skill (Stuff)
2. **Hit Type Distribution:** Varies significantly based on player attributes
   - Power hitters: More home runs and extra-base hits
   - Contact hitters: More singles, fewer strikeouts
   - Strong pitchers (high Stuff): More weak contact, more outs on balls in play
3. **Expected Distributions (Average vs. Average):**
   - **Outs:** ~70-71% of balls in play
   - **Singles:** ~60-70% of hits (or ~18-21% of all BIP)
   - **Doubles:** ~16-22% of hits (or ~5-7% of all BIP)
   - **Triples:** ~0.2-0.6% of hits (or ~0.06-0.18% of all BIP)
   - **Home Runs:** ~3-5% of all BIP
   - **BABIP (Batting Average on Balls In Play):** ~0.290 ± 0.030

### BABIP Definition
For this PRD, **BABIP** is defined as:
```
BABIP = (Singles + Doubles + Triples) / (BIP - HomeRuns)
```
This is the traditional definition that excludes home runs from both numerator and denominator, measuring the batting average on balls that stay in the field of play.

---

## 3. Goals & Objectives

### Primary Goals
1. Define `BipOutcome` enum for all possible ball-in-play results
2. Design `BallInPlayResolver` component with clear API and probability model
3. Implement Power and Stuff influence on outcome distributions
4. Validate distributions against realistic baseball statistics
5. Ensure deterministic behavior with seeded RNG

### Success Metrics
- All existing tests remain green (no regression)
- Distribution tests pass for average vs. average matchup (10,000 trials)
- HR%, 2B%, 3B%, Singles%, and BABIP fall within target ranges
- Power and Stuff demonstrably affect distributions in expected directions
- Tests are deterministic using seeded RNG

### Non-Goals (Out of Scope)
- Fielding mechanics or defensive positioning
- Baserunning or advancement on hits
- Park factors or environmental conditions
- Batted ball physics (launch angle, exit velocity)
- Specific defensive player ratings
- Situational hitting adjustments
- Hit location or spray charts

---

## 4. Functional Requirements

### FR-1: BipOutcome Enumeration
**Priority:** P0 (Critical)

**Description:** Define an enumeration for all possible ball-in-play outcomes.

**Requirements:**
- FR-1.1: Add `BipOutcome` enum to [`Outcomes.cs`](src/DiamondSim/Outcomes.cs)
- FR-1.2: Define enum values:
  - `Out` - Ball in play results in an out (fly out, ground out, line out)
  - `Single` - Batter reaches first base
  - `Double` - Batter reaches second base
  - `Triple` - Batter reaches third base
  - `HomeRun` - Batter circles all bases
- FR-1.3: Add XML documentation for each value
- FR-1.4: Consider adding `BipResult` record containing:
  - `Outcome` (BipOutcome) - The specific hit type or out
  - Optional: Additional metadata (contact quality, etc.)

**Acceptance Criteria:**
- ✅ Enum is properly defined with all five outcome types
- ✅ XML documentation clearly describes each outcome
- ✅ Enum follows project style guidelines

---

### FR-2: BallInPlayResolver Component Design
**Priority:** P0 (Critical)

**Description:** Design the component responsible for resolving BallInPlay outcomes into specific hit types.

**Requirements:**
- FR-2.1: Create new file [`BallInPlayResolver.cs`](src/DiamondSim/BallInPlayResolver.cs) in `src/DiamondSim/`
- FR-2.2: Implement `ResolveBallInPlay()` method that:
  - Accepts batter Power rating (double, 0.0-1.0 scale)
  - Accepts pitcher Stuff rating (double, 0.0-1.0 scale)
  - Accepts `IRandomSource` for deterministic testing
  - Returns `BipOutcome` enum value
- FR-2.3: Use table-driven approach (no physics simulation):
  - Define base probability distributions for average vs. average
  - Apply Power adjustment to shift distribution toward extra-base hits
  - Apply Stuff adjustment to shift distribution toward outs
  - Use cumulative probability tables for efficient sampling
- FR-2.4: Ensure method is stateless and thread-safe
- FR-2.5: Make implementation internal with public interface if appropriate

**Acceptance Criteria:**
- ✅ Resolver component is well-structured and documented
- ✅ API is clear and easy to use
- ✅ Implementation is deterministic with seeded RNG
- ✅ No external dependencies beyond existing project code

---

### FR-3: Probability Model & Attribute Influence
**Priority:** P0 (Critical)

**Description:** Define how Power and Stuff ratings influence outcome probabilities.

**Requirements:**
- FR-3.1: **Base Distribution (Average vs. Average, Power=0.5, Stuff=0.5):**
  - Out: ~70-71%
  - Single: ~18-21%
  - Double: ~5-7%
  - Triple: ~0.06-0.18%
  - HomeRun: ~3-5%
  - Total: 100%

- FR-3.2: **Power Influence (Batter):**
  - Higher Power (>0.5): Increase HR%, 2B%, 3B%; decrease Singles%, Outs%
  - Lower Power (<0.5): Decrease HR%, 2B%, 3B%; increase Singles%, Outs%
  - Suggested adjustment range: ±10-15% for extreme Power values
  - Power primarily affects extra-base hit probability

- FR-3.3: **Stuff Influence (Pitcher):**
  - Higher Stuff (>0.5): Increase Outs%; decrease all hit types proportionally
  - Lower Stuff (<0.5): Decrease Outs%; increase all hit types proportionally
  - Suggested adjustment range: ±8-12% for extreme Stuff values
  - Stuff primarily affects overall hit probability (BABIP)

- FR-3.4: **Adjustment Method:**
  - Use multiplicative or additive adjustments to base probabilities
  - Ensure probabilities remain valid (0.0-1.0 range, sum to 1.0)
  - Apply Power adjustment first, then Stuff adjustment
  - Document adjustment formulas clearly in code

- FR-3.5: **Probability Knobs:**
  - Define constants for base rates and adjustment factors
  - Make knobs easily tunable (constants at top of file or configuration)
  - Document rationale for each knob value

**Acceptance Criteria:**
- ✅ Base distribution matches target ranges for average vs. average
- ✅ Power demonstrably affects extra-base hit rates
- ✅ Stuff demonstrably affects overall hit rates (BABIP)
- ✅ All probabilities are valid and sum to 1.0
- ✅ Adjustment logic is well-documented

---

### FR-4: Distribution Validation Tests
**Priority:** P0 (Critical)

**Description:** Create comprehensive tests to validate BIP outcome distributions.

**Requirements:**
- FR-4.1: Create new test file [`BallInPlayTests.cs`](tests/DiamondSim.Tests/BallInPlayTests.cs) in `tests/DiamondSim.Tests/`

- FR-4.2: Implement test: `BipOutcomes_AverageVsAverage_ProducesRealisticDistributions()`
  - Simulate 10,000 balls in play with average Power (0.5) and average Stuff (0.5)
  - Count outcomes: outs, singles, doubles, triples, home runs
  - Calculate percentages and BABIP
  - Assert ranges:
    - **HR%:** 0.03 to 0.05 (3-5%)
    - **2B%:** 0.16 to 0.22 (16-22% of hits, or ~5-7% of BIP)
    - **3B%:** 0.002 to 0.006 (0.2-0.6% of hits, or ~0.06-0.18% of BIP)
    - **Singles%:** 0.60 to 0.70 (60-70% of hits, or ~18-21% of BIP)
    - **BABIP:** 0.26 to 0.32 (0.29 ± 0.03)
  - Assert sum: All percentages account for 100% of BIP

- FR-4.3: Implement test: `BipOutcomes_HighPower_IncreasesExtraBaseHits()`
  - Compare high Power (0.8) vs. average Power (0.5)
  - Assert: High Power produces significantly more HR% and 2B%
  - Assert: High Power produces fewer Outs%

- FR-4.4: Implement test: `BipOutcomes_HighStuff_IncreasesOuts()`
  - Compare high Stuff (0.8) vs. average Stuff (0.5)
  - Assert: High Stuff produces significantly more Outs%
  - Assert: High Stuff produces lower BABIP

- FR-4.5: Implement test: `BipOutcomes_LowPower_FavorsContactHits()`
  - Compare low Power (0.2) vs. average Power (0.5)
  - Assert: Low Power produces fewer HR% and more Singles%

- FR-4.6: Use `SeededRandom` with fixed seed for all tests
- FR-4.7: Document expected ranges and rationale in test comments

**Acceptance Criteria:**
- ✅ Tests pass consistently with seeded RNG
- ✅ Distributions fall within expected ranges
- ✅ Power and Stuff effects are statistically significant
- ✅ Tests clearly document assumptions and tolerances
- ✅ All tests follow `.rules/testing.md` (NUnit, deterministic)

---

### FR-5: Metrics Calculation & Reporting
**Priority:** P1 (High)

**Description:** Define how to calculate and report key metrics from BIP outcomes.

**Requirements:**
- FR-5.1: **BABIP Calculation:**
  ```
  BABIP = (Singles + Doubles + Triples) / (TotalBIP - HomeRuns)
  ```
  - Excludes home runs from both numerator and denominator
  - Traditional definition used in baseball statistics

- FR-5.2: **Hit Percentage Calculations:**
  - HR% = HomeRuns / TotalBIP
  - 2B% = Doubles / TotalBIP (or Doubles / TotalHits for hit distribution)
  - 3B% = Triples / TotalBIP (or Triples / TotalHits for hit distribution)
  - Singles% = Singles / TotalBIP (or Singles / TotalHits for hit distribution)
  - Outs% = Outs / TotalBIP

- FR-5.3: **Validation:**
  - HR% + 2B% + 3B% + Singles% + Outs% = 1.00 (100%)
  - BABIP = (Singles% + 2B% + 3B%) / (1.00 - HR%)

- FR-5.4: Document metric definitions in test comments and code documentation

**Acceptance Criteria:**
- ✅ Metrics are calculated correctly
- ✅ Definitions are clearly documented
- ✅ Validation formulas are verified in tests

---

## 5. Technical Design

### 5.1 Architecture Overview

```
┌─────────────────────────────────────┐
│      AtBatSimulator                 │
│                                     │
│  SimulateAtBat()                    │
│    ↓                                │
│  Returns: AtBatResult               │
│    Terminal = BallInPlay            │
└─────────────────────────────────────┘
                │
                │ If Terminal == BallInPlay
                ▼
┌─────────────────────────────────────┐
│   BallInPlayResolver                │
│                                     │
│  ResolveBallInPlay(                 │
│    power: double,                   │
│    stuff: double,                   │
│    random: IRandomSource            │
│  )                                  │
│    ↓                                │
│  1. Get base probabilities          │
│  2. Apply Power adjustment          │
│  3. Apply Stuff adjustment          │
│  4. Normalize probabilities         │
│  5. Sample from distribution        │
│    ↓                                │
│  Returns: BipOutcome                │
└─────────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────┐
│        BipOutcome                   │
│                                     │
│  Out | Single | Double |            │
│  Triple | HomeRun                   │
└─────────────────────────────────────┘
```

### 5.2 File Changes

#### New Files
1. **[`src/DiamondSim/BallInPlayResolver.cs`](src/DiamondSim/BallInPlayResolver.cs)**
   - Purpose: Resolve BallInPlay outcomes into specific hit types
   - Key method: `ResolveBallInPlay()`
   - Dependencies: [`IRandomSource`](src/DiamondSim/Random.cs), probability tables

2. **[`tests/DiamondSim.Tests/BallInPlayTests.cs`](tests/DiamondSim.Tests/BallInPlayTests.cs)**
   - Purpose: Validate BIP outcome distributions
   - Key tests: Distribution validation, Power effects, Stuff effects

#### Modified Files
1. **[`src/DiamondSim/Outcomes.cs`](src/DiamondSim/Outcomes.cs)**
   - Add `BipOutcome` enum
   - Optionally add `BipResult` record

### 5.3 Probability Table Design

**Approach:** Use cumulative probability distribution for efficient sampling.

```csharp
// Pseudo-code example
public class BallInPlayResolver {
    // Base probabilities for average vs. average (Power=0.5, Stuff=0.5)
    private const double BaseOutRate = 0.705;      // 70.5%
    private const double BaseSingleRate = 0.195;   // 19.5%
    private const double BaseDoubleRate = 0.060;   // 6.0%
    private const double BaseTripleRate = 0.001;   // 0.1%
    private const double BaseHomeRunRate = 0.039;  // 3.9%
    // Sum = 1.000

    // Adjustment factors
    private const double PowerAdjustmentFactor = 0.30;  // ±30% for extreme Power
    private const double StuffAdjustmentFactor = 0.20;  // ±20% for extreme Stuff

    public BipOutcome ResolveBallInPlay(
        double power,
        double stuff,
        IRandomSource random
    ) {
        // 1. Start with base probabilities
        var probs = GetBaseProbabilities();

        // 2. Apply Power adjustment (affects hit type distribution)
        probs = ApplyPowerAdjustment(probs, power);

        // 3. Apply Stuff adjustment (affects overall hit rate)
        probs = ApplyStuffAdjustment(probs, stuff);

        // 4. Normalize to ensure sum = 1.0
        probs = Normalize(probs);

        // 5. Sample from cumulative distribution
        return SampleOutcome(probs, random);
    }
}
```

### 5.4 Power Adjustment Logic

**Goal:** Higher Power increases extra-base hits, especially home runs.

```csharp
// Pseudo-code
private Probabilities ApplyPowerAdjustment(Probabilities probs, double power) {
    // power: 0.0 (weak) to 1.0 (elite)
    // Neutral power = 0.5
    double powerDelta = power - 0.5; // Range: -0.5 to +0.5

    // Shift probability mass from singles/outs to extra-base hits
    double hrBoost = powerDelta * PowerAdjustmentFactor * 2.0;  // HR most affected
    double doubleBoost = powerDelta * PowerAdjustmentFactor * 1.0;
    double tripleBoost = powerDelta * PowerAdjustmentFactor * 0.3;

    probs.HomeRun += hrBoost;
    probs.Double += doubleBoost;
    probs.Triple += tripleBoost;

    // Compensate by reducing singles and outs
    double totalBoost = hrBoost + doubleBoost + tripleBoost;
    probs.Single -= totalBoost * 0.4;  // Singles reduced
    probs.Out -= totalBoost * 0.6;     // Outs reduced more

    return probs;
}
```

### 5.5 Stuff Adjustment Logic

**Goal:** Higher Stuff increases outs, decreases all hit types proportionally.

```csharp
// Pseudo-code
private Probabilities ApplyStuffAdjustment(Probabilities probs, double stuff) {
    // stuff: 0.0 (weak) to 1.0 (elite)
    // Neutral stuff = 0.5
    double stuffDelta = stuff - 0.5; // Range: -0.5 to +0.5

    // Shift probability mass between outs and hits
    double outBoost = stuffDelta * StuffAdjustmentFactor;

    probs.Out += outBoost;

    // Reduce all hit types proportionally
    double hitReduction = outBoost / (1.0 - probs.Out);
    probs.Single -= probs.Single * hitReduction;
    probs.Double -= probs.Double * hitReduction;
    probs.Triple -= probs.Triple * hitReduction;
    probs.HomeRun -= probs.HomeRun * hitReduction;

    return probs;
}
```

### 5.6 Sampling Algorithm

```csharp
// Pseudo-code
private BipOutcome SampleOutcome(Probabilities probs, IRandomSource random) {
    double roll = random.NextDouble(); // 0.0 to 1.0
    double cumulative = 0.0;

    cumulative += probs.Out;
    if (roll < cumulative) return BipOutcome.Out;

    cumulative += probs.Single;
    if (roll < cumulative) return BipOutcome.Single;

    cumulative += probs.Double;
    if (roll < cumulative) return BipOutcome.Double;

    cumulative += probs.Triple;
    if (roll < cumulative) return BipOutcome.Triple;

    return BipOutcome.HomeRun; // Remaining probability
}
```

---

## 6. Acceptance Criteria

### AC-1: Backward Compatibility
- ✅ All existing tests pass without modification
- ✅ No changes to existing public APIs
- ✅ [`AtBatSimulator`](src/DiamondSim/AtBatSimulator.cs) continues to work as before

### AC-2: BipOutcome Enumeration
- ✅ `BipOutcome` enum is defined in [`Outcomes.cs`](src/DiamondSim/Outcomes.cs)
- ✅ All five outcome types are present: Out, Single, Double, Triple, HomeRun
- ✅ XML documentation is complete and clear

### AC-3: Distribution Targets (Average vs. Average, 10,000 trials, Power=0.5, Stuff=0.5)
- ✅ **HR%:** 0.03 to 0.05 (3-5% of all BIP)
- ✅ **2B%:** 0.05 to 0.07 (5-7% of all BIP, or 16-22% of hits)
- ✅ **3B%:** 0.0006 to 0.0018 (0.06-0.18% of all BIP, or 0.2-0.6% of hits)
- ✅ **Singles%:** 0.18 to 0.21 (18-21% of all BIP, or 60-70% of hits)
- ✅ **Outs%:** Remainder (approximately 70-71%)
- ✅ **BABIP:** 0.26 to 0.32 (0.29 ± 0.03)
- ✅ Sum of all percentages: 100%

### AC-4: Power Effects (10,000 trials)
- ✅ High Power (0.8) produces significantly more HR% than average Power (0.5)
- ✅ High Power (0.8) produces significantly more 2B% than average Power (0.5)
- ✅ High Power (0.8) produces fewer Outs% than average Power (0.5)
- ✅ Low Power (0.2) produces fewer HR% and more Singles% than average Power (0.5)

### AC-5: Stuff Effects (10,000 trials)
- ✅ High Stuff (0.8) produces significantly more Outs% than average Stuff (0.5)
- ✅ High Stuff (0.8) produces lower BABIP than average Stuff (0.5)
- ✅ Low Stuff (0.2) produces fewer Outs% and higher BABIP than average Stuff (0.5)

### AC-6: Deterministic Testing
- ✅ Tests use `SeededRandom` with fixed seed
- ✅ Tests pass consistently across multiple runs
- ✅ No flaky test failures due to randomness

### AC-7: Code Quality
- ✅ New code follows `.rules/style.md` (K&R braces, file-scoped namespaces)
- ✅ Tests follow `.rules/testing.md` (NUnit, deterministic)
- ✅ XML documentation comments for public APIs
- ✅ No compiler warnings or errors

---

## 7. Testing Strategy

### 7.1 Unit Tests
- **BipOutcome enum:** Verify all values are defined correctly
- **Probability normalization:** Verify probabilities sum to 1.0 after adjustments
- **Edge cases:** Verify extreme Power/Stuff values (0.0, 1.0) produce valid distributions

### 7.2 Integration Tests (Primary Validation)
- **Distribution test:** [`BallInPlayTests.cs`](tests/DiamondSim.Tests/BallInPlayTests.cs)
  - Sample size: 10,000 balls in play
  - Seeded RNG: Fixed seed for reproducibility
  - Assertions: HR%, 2B%, 3B%, Singles%, Outs%, BABIP within target ranges
  - Test name: `BipOutcomes_AverageVsAverage_ProducesRealisticDistributions()`

### 7.3 Attribute Effect Tests
- **High Power test:** Verify increased extra-base hits
- **Low Power test:** Verify decreased extra-base hits
- **High Stuff test:** Verify increased outs and lower BABIP
- **Low Stuff test:** Verify decreased outs and higher BABIP

### 7.4 Regression Tests
- **Existing test suites:** All tests in [`AtBatTests.cs`](tests/DiamondSim.Tests/AtBatTests.cs), [`CountContactTests.cs`](tests/DiamondSim.Tests/CountContactTests.cs), and [`AtBatLoopTests.cs`](tests/DiamondSim.Tests/AtBatLoopTests.cs) must pass
- **No API changes:** Existing code continues to work

### 7.5 Statistical Validation
- **Chi-square test (optional):** Verify distributions are statistically consistent
- **Variance analysis:** Document expected variance for 10,000 trials
- **Confidence intervals:** Calculate 95% confidence intervals for each metric

---

## 8. Risks & Mitigation

### Risk 1: Distribution Tuning Difficulty
**Description:** Achieving all target ranges simultaneously may require extensive tuning.

**Likelihood:** High
**Impact:** Medium

**Mitigation:**
- Start with research-based initial values from baseball statistics
- Document all probability knobs and adjustment factors clearly
- Make knobs easily adjustable (constants at top of file)
- Use iterative approach: run tests, adjust knobs, repeat
- Accept wider tolerance ranges initially if needed
- Consider using optimization algorithms to find best knob values
- Document the tuning process and final values

---

### Risk 2: Power and Stuff Interactions
**Description:** Power and Stuff adjustments may interact in unexpected ways, making it difficult to achieve independent effects.

**Likelihood:** Medium
**Impact:** Medium

**Mitigation:**
- Apply adjustments in a specific order (Power first, then Stuff)
- Test each adjustment independently before combining
- Document interaction effects clearly
- Consider using multiplicative vs. additive adjustments
- Validate that extreme combinations (high Power + high Stuff) produce reasonable results
- Add specific tests for interaction scenarios

---

### Risk 3: Triple Rarity
**Description:** Triples are very rare (~0.1% of BIP), making them difficult to validate statistically with 10,000 trials.

**Likelihood:** High
**Impact:** Low

**Mitigation:**
- Use wider tolerance range for triple percentage (0.06-0.18% of BIP)
- Consider increasing sample size for triple-specific tests (e.g., 50,000 trials)
- Document expected variance for rare events
- Focus validation on more common outcomes (HR, 2B, Singles, Outs)
- Consider making triple rate a tunable parameter
- Accept that triple validation may be less precise

---

### Risk 4: BABIP Calculation Complexity
**Description:** BABIP calculation excludes home runs, which may cause confusion or implementation errors.

**Likelihood:** Low
**Impact:** Low

**Mitigation:**
- Clearly document BABIP definition in code and tests
- Use explicit formula: `BABIP = (1B + 2B + 3B) / (BIP - HR)`
- Add validation test to verify BABIP calculation is correct
- Include comments explaining why HR is excluded
- Provide example calculations in test documentation

---

### Risk 5: Unrealistic Extreme Distributions
**Description:** Extreme Power or Stuff values may produce unrealistic distributions (e.g., 50% home runs).

**Likelihood:** Medium
**Impact:** Medium

**Mitigation:**
- Define reasonable bounds for Power and Stuff effects
- Cap adjustment factors to prevent extreme outcomes
- Add validation tests for extreme attribute values
- Document expected ranges for extreme cases
- Consider using logarithmic or sigmoid curves for adjustments
- Validate against real baseball data for elite players

---

## 9. Implementation Notes

### 9.1 Probability Knob Starting Values

Suggested initial values for tuning:

```csharp
// Base probabilities (Average vs. Average: Power=0.5, Stuff=0.5)
private const double BaseOutRate = 0.705;      // 70.5%
private const double BaseSingleRate = 0.195;   // 19.5%
private const double BaseDoubleRate = 0.060;   // 6.0%
private const double BaseTripleRate = 0.001;   // 0.1%
private const double BaseHomeRunRate = 0.039;  // 3.9%
// Sum = 1.000

// Adjustment factors
private const double PowerAdjustmentFactor = 0.30;  // ±30% for extreme Power
private const double StuffAdjustmentFactor = 0.20;  // ±20% for extreme Stuff

// Power distribution (how Power affects different hit types)
private const double PowerToHomeRunWeight = 2.0;    // HR most affected
private const double PowerToDoubleWeight = 1.0;     // 2B moderately affected
private const double PowerToTripleWeight = 0.3;     // 3B slightly affected

// Stuff distribution (how Stuff affects outs vs. hits)
private const double StuffToOutWeight = 1.0;        // Direct effect on out rate
```

These values should be documented and easily adjustable.

### 9.2 Implementation Approach

**Phase 1: Foundation**
1. Add `BipOutcome` enum to [`Outcomes.cs`](src/DiamondSim/Outcomes.cs)
2. Optionally add `BipResult` record

**Phase 2: Core Resolver**
3. Create [`BallInPlayResolver.cs`](src/DiamondSim/BallInPlayResolver.cs)
4. Implement base probability table
5. Implement Power adjustment logic
6. Implement Stuff adjustment logic
7. Implement probability normalization
8. Implement sampling algorithm

**Phase 3: Validation**
9. Create [`BallInPlayTests.cs`](tests/DiamondSim.Tests/BallInPlayTests.cs)
10. Implement average vs. average distribution test (10,000 trials)
11. Tune probability knobs to achieve target distributions
12. Implement Power effect tests
13. Implement Stuff effect tests
14. Verify all existing tests still pass

### 9.3 Testing Approach

1. **Start with basic test:** Verify resolver produces valid outcomes
2. **Add distribution test:** Run 10,000 trials, observe actual distributions
3. **Tune base probabilities:** Adjust to match target ranges
4. **Add Power adjustment:** Verify high Power increases HR% and 2B%
5. **Add Stuff adjustment:** Verify high Stuff increases Outs% and lowers BABIP
6. **Validate interactions:** Test extreme combinations
7. **Add edge case tests:** Verify extreme attribute values

### 9.4 Code Organization

- Keep [`BallInPlayResolver`](src/DiamondSim/BallInPlayResolver.cs) focused on probability calculations
- Extract helper methods for adjustments and normalization
- Use clear, descriptive method names (e.g., `ApplyPowerAdjustment()`, `Normalize()`)
- Document all probability knobs and adjustment formulas
- Follow existing project patterns from [`AtBatSimulator`](src/DiamondSim/AtBatSimulator.cs)

 CI Runtime Considerations

- **Test Duration:** 10,000 trials per test should complete in < 1 second
- **Total Test Suite:** Multiple tests may take 2-5 seconds total
- **Acceptable:** This is reasonable for CI/CD pipelines
- **Optimization:** If needed, reduce trial count for faster feedback (e.g., 5,000 trials)
- **Parallel Execution:** Tests are independent and can run in parallel

---

## 10. Future Enhancements (Out of Scope)

The following features are explicitly out of scope for this PRD but may be considered in future iterations:

1. **Fielding Mechanics:** Defensive positioning, fielder ratings, throwing arms
2. **Baserunning:** Runner advancement on hits, taking extra bases
3. **Park Factors:** Ballpark dimensions affecting HR rates
4. **Weather Conditions:** Wind, temperature affecting ball flight
5. **Batted Ball Data:** Launch angle, exit velocity, spray charts
6. **Hit Location:** Left field, center field, right field distribution
7. **Defensive Shifts:** Positioning affecting out probability
8. **Situational Hitting:** Adjustments based on game situation
9. **Advanced Metrics:** wOBA, ISO, expected batting average (xBA)
10. **Historical Validation:** Comparing distributions to real MLB data

---

## 11. Dependencies

### Internal Dependencies
- [`Outcomes.cs`](src/DiamondSim/Outcomes.cs) - Existing enums and records (modified)
- [`Random.cs`](src/DiamondSim/Random.cs) - `IRandomSource` interface for deterministic RNG
- [`Model.cs`](src/DiamondSim/Model.cs) - Player attribute definitions (Power, Stuff)
- [`AtBatSimulator.cs`](src/DiamondSim/AtBatSimulator.cs) - Produces BallInPlay outcomes

### External Dependencies
- .NET 8 SDK
- NUnit 3.x test framework (per `.rules/testing.md`)
- No new external dependencies required

---

## 12. Documentation Requirements

### Code Documentation
- XML documentation comments for all public APIs
- Inline comments explaining probability knobs and adjustment formulas
- Comments documenting BABIP calculation and definition
- Example usage in method documentation
- Clear explanation of Power and Stuff effects

### Test Documentation
- Clear test names describing what is being validated
- Comments explaining distribution targets and tolerances
- Documentation of RNG seeding strategy
- Comments explaining probability knob values used in tests
- Example calculations for BABIP and other metrics

### Tuning Documentation
- Document all probability knobs and their initial values
- Document the tuning process and iterations
- Document final values and rationale
- Provide guidance for future tuning if needed
- Document expected variance for each metric

---

## 13. Success Criteria Summary

This feature will be considered successfully implemented when:

1. ✅ `BipOutcome` enum is defined in [`Outcomes.cs`](src/DiamondSim/Outcomes.cs)
2. ✅ [`BallInPlayResolver.cs`](src/DiamondSim/BallInPlayResolver.cs) implements resolution logic
3. ✅ [`BallInPlayTests.cs`](tests/DiamondSim.Tests/BallInPlayTests.cs) validates distributions (10,000 trials)
4. ✅ HR% falls within 3-5% range
5. ✅ 2B% falls within 5-7% of BIP range (16-22% of hits)
6. ✅ 3B% falls within 0.06-0.18% of BIP range (0.2-0.6% of hits)
7. ✅ Singles% falls within 18-21% of BIP range (60-70% of hits)
8. ✅ BABIP falls within 0.26-0.32 range (0.29 ± 0.03)
9. ✅ All percentages sum to 100%
10. ✅ Power demonstrably affects extra-base hit rates
11. ✅ Stuff demonstrably affects out rates and BABIP
12. ✅ All existing tests pass without modification
13. ✅ Tests are deterministic using `SeededRandom`
14. ✅ Code follows `.rules/style.md` and `.rules/testing.md`
15. ✅ No compiler warnings or errors

---

## 14. Appendix

### A. BIP Outcome Definitions
- **Out:** Ball in play results in an out (fly out, ground out, line out)
- **Single:** Batter reaches first base safely
- **Double:** Batter reaches second base safely
- **Triple:** Batter reaches third base safely
- **Home Run:** Batter circles all bases and scores

### B. Expected Distribution Ranges (Average vs. Average)

**As Percentage of All Balls In Play:**
- **Outs:** ~70-71%
- **Singles:** ~18-21%
- **Doubles:** ~5-7%
- **Triples:** ~0.06-0.18%
- **Home Runs:** ~3-5%
- **Total:** 100%

**As Percentage of Hits Only:**
- **Singles:** ~60-70% of hits
- **Doubles:** ~16-22% of hits
- **Triples:** ~0.2-0.6% of hits
- **Home Runs:** ~10-15% of hits

**Key Metrics:**
- **BABIP:** 0.26 to 0.32 (0.29 ± 0.03)
- **Hit Rate:** ~29-30% of BIP (excluding HR from BABIP calculation)

*Note: These ranges are based on typical MLB statistics for average matchups. Exact ranges will be validated through testing and may be adjusted based on simulation results.*

### C. Power and Stuff Effect Examples

**High Power Batter (0.8) vs. Average (0.5):**
- HR%: +2-3 percentage points (e.g., 4% → 6-7%)
- 2B%: +1-2 percentage points (e.g., 6% → 7-8%)
- Singles%: -1-2 percentage points (e.g., 20% → 18-19%)
- Outs%: -2-3 percentage points (e.g., 70% → 67-68%)

**High Stuff Pitcher (0.8) vs. Average (0.5):**
- Outs%: +3-5 percentage points (e.g., 70% → 73-75%)
- BABIP: -0.03 to -0.05 (e.g., 0.29 → 0.24-0.26)
- All hit types reduced proportionally

*These are approximate examples and will be refined during implementation.*

### D. BABIP Calculation Examples

**Example 1: Average vs. Average**
- Total BIP: 10,000
- Outs: 7,050 (70.5%)
- Singles: 1,950 (19.5%)
- Doubles: 600 (6.0%)
- Triples: 10 (0.1%)
- Home Runs: 390 (3.9%)
- **BABIP = (1,950 + 600 + 10) / (10,000 - 390) = 2,560 / 9,610 = 0.266**

**Example 2: High Power Batter**
- Total BIP: 10,000
- Outs: 6,800 (68.0%)
- Singles: 1,800 (18.0%)
- Doubles: 700 (7.0%)
- Triples: 10 (0.1%)
- Home Runs: 690 (6.9%)
- **BABIP = (1,800 + 700 + 10) / (10,000 - 690) = 2,510 / 9,310 = 0.270**

### E. References
- Part 1 PRD: [`.prd/20251024_01_CountConditionedContact.md`](.prd/20251024_01_CountConditionedContact.md)
- Part 2 PRD: [`.prd/20251024_02_AtBatLoop.md`](.prd/20251024_02_AtBatLoop.md)
- Existing outcomes: [`src/DiamondSim/Outcomes.cs`](src/DiamondSim/Outcomes.cs)
- At-bat simulator: [`src/DiamondSim/AtBatSimulator.cs`](src/DiamondSim/AtBatSimulator.cs)
- Test suite: [`tests/DiamondSim.Tests/AtBatLoopTests.cs`](tests/DiamondSim.Tests/AtBatLoopTests.cs)
- Random source: [`src/DiamondSim/Random.cs`](src/DiamondSim/Random.cs)

---

**Document End**
### 9.5
