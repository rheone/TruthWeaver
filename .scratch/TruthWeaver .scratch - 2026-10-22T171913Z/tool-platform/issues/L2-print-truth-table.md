# L2: PrintTruthTable

Scope decisions (owner, 2026-10-11) are in the spec. The worker adds no project, package or dependency beyond them without asking.

**What to build:** `CompiledRule.PrintTruthTable(TruthTableOptions?)` over the rule's terms. K3 table: all 3^n combinations of `True`/`False`/`Unknown` with the rule's value per row; option for the classical table (2^n over `True`/`False` only). Order of rows and columns is stable (term order of first occurrence, `False` before `Unknown` before `True`, matching the engine's ordering note that it is not numeric). Default term cap 8 (6,561 rows). A larger table needs an explicit option and returns a warning diagnostic; above an absolute hard limit (documented, for example 12 terms) it is refused. Output formats: Markdown, CSV, JSON, HTML, LaTeX. `TextWriter` overloads and a `Try…` form. Terms can be shown as calls or as letters (`SimpleVariables`) with the constant style option. Rules with variables use stub values like the rest of the tool.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Every row equals `EvaluateAsync` with the same stubs (property test against `K3Oracle`)
- [x] The cap, the raised-cap warning and the hard limit are tested
- [x] All five formats are documented with an example in `docs/`, tested through the doctest checker
- [x] The page for this feature is written or updated in the same change, `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

2026-10-11, dispatch run: choices the ticket left open, all the conservative option.

- Hard limit is 12 terms (`TruthTableOptions.HardTermLimit`), as the ticket's example said; the default cap is 8 (`DefaultTermCap`). Both count distinct terms and apply to the classical table as well. Neither is a settable number.
- API: `PrintTruthTable(options)` (string, throws `InvalidOperationException` when refused), `PrintTruthTable(writer, options)`, and `TryPrintTruthTable` for both. A raised cap returns the new `TRE0034` warning (`TruthTableCapRaised`) and a refusal the new `TRE0035` error (`TruthTableTooLarge`); the throwing forms cannot return the warning, so only the `Try` forms expose it.
- "Rules with variables use stub values": the table treats each term, including one with a `from(...)` argument, as a free variable; no predicate runs and no data source is read. The values come from the analyzer's dual-rail BDD (`Analyzer.CreateValuation`), built once, so the table shares the compiler's definition of every operator. The property test compares every row to `EvaluateAsync` with the same stub values, and a second test compares the connectives to `K3Oracle`.
- JSON and CSV use the same constant style as the other formats (`Letters` by default). A program that parses the JSON should set `ConstantFormat.Style = Word`.
- Lettered headers use the letters of `PrintEquationWithLegend`; no legend is added to the table (the JSON has `terms` with the letter and the full call).
- CSV rows end with a line feed, not CRLF, to match the deterministic-output rule. No format has a trailing line feed.
