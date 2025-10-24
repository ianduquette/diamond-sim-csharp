# DiamondSim Code Style

- **Language/Target:** C# 12, .NET 8
- **Namespaces:** file-scoped (e.g., `namespace DiamondSim;`)
- **Braces:** K&R — opening `{` on the **same line**
- **Indentation:** 4 spaces, LF line endings, final newline
- **Usings:** outside the namespace
- **Locals:** prefer `var` when the type is obvious
- **Visibility:** keep APIs minimal; make helpers `internal` where possible
- **Layout:** public members first, then private
- **Project structure:**
  - Source in `src/DiamondSim/`
  - Tests in `tests/DiamondSim.Tests/`
- **Determinism:** tests must use `SeededRandom`

> If a rule here conflicts with editor defaults, **follow this file** and `.editorconfig`.
