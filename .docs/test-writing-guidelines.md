# Test Writing Guidelines for Diamond Sim C#

## Critical Rules - NEVER VIOLATE THESE

### 1. **ONE ACT PER TEST** (Most Important!)
- Each test method should call the System Under Test (SUT) **exactly once**
- Exception: Tests validating **relative relationships** may measure twice (baseline + target) but this must be explicit in Arrange

**WRONG:**
```csharp
[Test]
public void EliteBatter_HigherContactThanAverage() {
    var averageRate = ExecuteSut(averageBatter, pitcher);  // ❌ First Act
    var eliteRate = ExecuteSut(eliteBatter, pitcher);      // ❌ Second Act
    Assert.That(eliteRate, Is.GreaterThan(averageRate));
}
```

**RIGHT (Absolute Value Test):**
```csharp
[Test]
public void EliteBatter_ExpectedContactRate() {
    var eliteRate = ExecuteSut(eliteBatter, pitcher);  // ✅ One Act
    Assert.That(eliteRate, Is.InRange(0.78, 0.86));    // ✅ Assert against known value
}
```

**RIGHT (Relative Relationship Test):**
```csharp
[Test]
public void Count_0_2_LowerThanBaseline() {
    // Arrange
    var baselineRate = ExecuteSut(batter, pitcher, 0, 0);  // ✅ Baseline in Arrange

    // Act
    var rate_0_2 = ExecuteSut(batter, pitcher, 0, 2);      // ✅ One Act

    // Assert
    Assert.That(rate_0_2, Is.LessThan(baselineRate));
}
```

### 2. **Use ExecuteSut Pattern**
- Create a private helper method called `ExecuteSut` that encapsulates SUT execution
- Place it at the **end of the test class**
- Make it `static` if possible (no instance state needed)

```csharp
/// <summary>
/// Executes the System Under Test (SUT) - measures contact rate.
/// </summary>
private static double ExecuteSut(Batter batter, Pitcher pitcher) {
    var rng = new SeededRandom(seed: Seed);
    var resolver = new ContactResolver(rng);
    var trials = TestConfig.SIM_DEFAULT_N;
    var contacts = ContactResolverTestHelper.CountContacts(resolver, batter, pitcher, trials);
    return (double)contacts / trials;
}
```

### 3. **Minimize Instance Fields**
- Avoid `[SetUp]` methods unless absolutely necessary
- Don't store `_resolver`, `_trials`, etc. as instance fields
- Use constants for fixed values (e.g., `Seed`, `Balls`, `Strikes`)
- Let `ExecuteSut` create what it needs

**WRONG:**
```csharp
private ContactResolver _resolver;
private int _trials;

[SetUp]
public void SetUp() {
    _resolver = new ContactResolver(new SeededRandom(Seed));
    _trials = TestConfig.SIM_DEFAULT_N;
}
```

**RIGHT:**
```csharp
private const int Seed = 1337;
// ExecuteSut creates resolver and trials internally
```

### 4. **Use `var` Appropriately**
- Use `var` for local variables when the type is obvious from the right side
- Always use `var` for return values from method calls

```csharp
var batter = TestFactory.CreateEliteBatter();        // ✅
var contactRate = ExecuteSut(batter, pitcher);       // ✅
var contacts = CountContacts(resolver, ...);         // ✅
```

### 5. **Test Purpose Determines Structure**

**Baseline Tests** (validate absolute values):
- One Act per test
- Assert against known expected values
- Calculate expected values from formulas in production code

**Relationship Tests** (validate relative comparisons):
- Baseline measurement in Arrange
- Target measurement in Act
- Assert relationship (LessThan, GreaterThan, etc.)

### 6. **Don't Add Unnecessary Complexity**
- If all tests use the same count (e.g., 0-0), make it a constant
- If all tests use the same players (e.g., average), create them in Arrange
- Don't pass parameters that never change

### 7. **Leverage Static Properties in Model**
- Add common rating presets to `BatterRatings` and `PitcherRatings` as static properties
- Use these throughout the codebase (not just tests)
- Examples: `BatterRatings.Average`, `BatterRatings.Elite`, `BatterRatings.Poor`

### 8. **Test Organization**
```csharp
[TestFixture]
public class MyTests {
    private const int Seed = 1337;
    // Other constants here

    [Test]
    public void Test1() {
        // Arrange
        // Act
        // Assert
    }

    [Test]
    public void Test2() {
        // Arrange
        // Act
        // Assert
    }

    /// <summary>
    /// Executes the System Under Test (SUT).
    /// </summary>
    private static ReturnType ExecuteSut(params) {
        // Implementation
    }
}
```

## Quick Checklist Before Submitting Tests

- [ ] Each test has exactly ONE Act (or baseline in Arrange + one Act for relationship tests)
- [ ] Using `ExecuteSut` pattern
- [ ] `ExecuteSut` is at the end of the class
- [ ] No unnecessary instance fields
- [ ] Using `var` for local variables
- [ ] Test names clearly describe what's being tested
- [ ] Asserting against known values (not comparing two measurements unless testing relationships)

## Remember

**The goal is clean, maintainable, easy-to-understand tests that follow OO principles. When in doubt, ask yourself: "Does this test have exactly one Act?"**
