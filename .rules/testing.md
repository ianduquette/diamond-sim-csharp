# DiamondSim Testing Rules

- **Framework:** **NUnit only** (not xUnit, not MSTest)
- **Packages:** use the standard NUnit + Microsoft.NET.Test.Sdk only
- **Namespaces:** tests live under `namespace Tests;`
- **Determinism:** always use `new SeededRandom(<constant>)`
- **Structure:**
  - `[TestFixture]` per suite, `[Test]` per case
  - Avoid flaky thresholds; use large trial counts when testing distributions
- **Assertions:** prefer `Assert.That(x, Is.InRange(a, b));`, `Assert.AreEqual`, etc.
- **Folders:** all test files go in `tests/DiamondSim.Tests/`
