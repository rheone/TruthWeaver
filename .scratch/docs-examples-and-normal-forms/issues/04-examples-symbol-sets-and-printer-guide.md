# 04: Symbol sets and a printer guide

**What to build:** Add to `docs/examples.md` a symbol-set section: the same rule printed with `OperatorStyle.Word`, `Symbolic` and `CStyle`, and with `SimpleVariables`, with a table of the symbols and the `CStyle` equivalence caveat. Add a table at the top of the page: which printer suits which job.

**Blocked by:** 02

**Status:** done

- [x] All three operator styles are shown on one rule, with a table of the symbols
- [x] The `CStyle` caveat is stated
- [x] The printer table links to `docs/equations.md` and `docs/rule-text.md`
- [x] Every runnable block has a `<!-- doctest:... -->` marker; `dotnet test tests/TruthWeaver.Tests --filter-class "*DocExample*"` and `"*DocumentationLint*"` pass
- [x] The page follows `docs/agents/documentation-standard.md` (plain language, ASD-STE100, no links to ADRs or `.scratch`)
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
