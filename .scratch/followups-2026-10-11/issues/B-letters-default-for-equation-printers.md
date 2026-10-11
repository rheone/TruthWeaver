# B: Letters is the default constant style for equations

**What to build:** Make `ConstantStyle.Letters` (T, F, U) the default for `PrintEquation` in every dialect (Unicode, LaTeX, AsciiMath, CStyle, MathML, Typst, Ascii) and for `PrintEquationWithLegend`. `PrintRuleText`, `PrintPlainText`, `PrintMermaid`, `PrintHtml` and `PrintDot` keep `Word`. An explicit `Word` setting still gives `True`, `False`, `Unknown`. Update every doc example and test whose output changes (docs/equations.md, docs/examples.md, the rewrite pages) and record the break in the CHANGELOG with a migration step (`Constants = new ConstantFormat { Style = ConstantStyle.Word }`).

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `EquationOptions.Default` prints constants as T, F, U in all dialects
- [x] Existing tests and doc examples are updated, not deleted
- [x] Docs explain the default and how to get words back
- [x] The page that describes the changed behavior is updated in the same change (no "changed from" text), `CHANGELOG.md` records each break with a migration step, and `K3ReferenceSyncChecker` and the doc example tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

- 2026-10-11 (dispatch run): The `Architecture.Tests` and `Generators.Tests` projects could not be built here: a running `CSharpLanguageServer` process locks `src/TruthWeaver.Generators/bin/.../TruthWeaver.Generators.dll` (MSB3027). The other six test projects pass, `dotnet build -p:CI=true` passes (built with a separate `--artifacts-path`), and csharpier and `dotnet format` checks are clean. The doc-example checker gained a `words` option word; an equation or legend marker with no constant word now documents the letters default.
