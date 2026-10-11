# L2: PrintTruthTable

Scope decisions (owner, 2026-10-11) are in the spec. The worker adds no project, package or dependency beyond them without asking.

**What to build:** `CompiledRule.PrintTruthTable(TruthTableOptions?)` over the rule's terms. K3 table: all 3^n combinations of `True`/`False`/`Unknown` with the rule's value per row; option for the classical table (2^n over `True`/`False` only). Order of rows and columns is stable (term order of first occurrence, `False` before `Unknown` before `True`, matching the engine's ordering note that it is not numeric). Default term cap 8 (6,561 rows). A larger table needs an explicit option and returns a warning diagnostic; above an absolute hard limit (documented, for example 12 terms) it is refused. Output formats: Markdown, CSV, JSON, HTML, LaTeX. `TextWriter` overloads and a `Try…` form. Terms can be shown as calls or as letters (`SimpleVariables`) with the constant style option. Rules with variables use stub values like the rest of the tool.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Every row equals `EvaluateAsync` with the same stubs (property test against `K3Oracle`)
- [ ] The cap, the raised-cap warning and the hard limit are tested
- [ ] All five formats are documented with an example in `docs/`, tested through the doctest checker
- [ ] The page for this feature is written or updated in the same change, `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
