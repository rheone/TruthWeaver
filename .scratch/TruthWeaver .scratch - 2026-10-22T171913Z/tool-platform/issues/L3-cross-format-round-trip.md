# L3: Cross-format round-trip guarantee

Scope decisions (owner, 2026-10-11) are in the spec. The worker adds no project, package or dependency beyond them without asking.

**What to build:** A property test and a public helper that prove: for generated rules (including notes and every operator, literal kind and data-source variable), converting between rule text, JSON and YAML in all six directions gives a document that compiles to an equal tree (and equal notes). Find and fix any printer or reader gap the test exposes (the text round trip is property-tested today; the other directions are not confirmed). Add public conversion helpers on `CompiledRule` and the compiler if they are missing, with `Try…` forms.

**Blocked by:** L1

**Status:** done

- [x] All six conversions are covered by one generated-rule property test over many seeds
- [x] Gaps found are fixed and listed in the ticket
- [x] `docs/rule-formats.md` states the guarantee and its exceptions (rewrites drop notes)
- [x] The page for this feature is written or updated in the same change, `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Gaps found

None. `CrossFormatRoundTripTests.Convert_GeneratedRulesInAllSixDirections_CompileToEqualTreesAndNotes_Test` runs 300 seeds through all six directions and asserts that the run used every operator, every literal kind, a variable reference and a note. Probes that did not expose a gap: strings with `"`, `\`, LF, CR, tab, U+0085, U+2028, U+0001, `#`, `:`, brackets and YAML indicator characters; `long.MinValue`/`long.MaxValue` and 2^53 + 1; `decimal.MaxValue`/`MinValue`, 1e-28, trailing-zero scale and 29-digit decimals; empty arrays; DateTimeOffset with offsets of up to +/-14 h; queries with quotes and backslashes; notes with `*/`, `//`, quotes and line feeds. The L1 printers and readers were already exercised by this test before it was committed.

## Comments

- 2026-10-11, dispatch run: done. Decisions taken without the owner (review welcome):
  - `RuleFormat` (`Text`, `Json`, `Yaml`) is in core (`TruthWeaver.Compilation`) so C1 and later core APIs share it; the helpers that take it (`Convert`/`TryConvert`, `CompileAs`/`TryCompileAs`, `PrintAs`) are extension methods in `TruthWeaver.Yaml` (`RuleFormatExtensions`), the only package that reads and writes all three formats. `RuleConversionResult` (`Text`, `Diagnostics`, `Succeeded`) is in `TruthWeaver.Yaml`.
  - `PrintAs(Text)` uses `PrintRuleText()` (notes kept); with `includeNotes: false` it is `CanonicalText`. An undefined `RuleFormat` value throws `ArgumentOutOfRangeException` (a programming error, so no `Try` path for it).
