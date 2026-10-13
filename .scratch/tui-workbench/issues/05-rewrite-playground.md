# 05: Rewrites with steps and compare

Scope decisions (owner, 2026-10-11) are in the spec; the worker adds no project, package or dependency beyond them without asking.

**What to build:** Apply `Simplify`, `Canonicalize`, NNF, CNF, DNF, compress, expand to primitives and `OptimizeOrder` to the current rule; show the step list (`SimplifyWithSteps`), a before and after (text and tree), size metrics and any diagnostics (for example `TRE0033`); accept the result into the editor or discard it. A compare view against the last saved or accepted version showing equivalence (and, when ticket 13 exists, looser or tighter).

**Blocked by:** 04

**Status:** on-hold until tickets 16 and 20 (feature-backlog-2026-10-11) are done; then ready-for-agent

- [ ] Each rewrite shows before and after and the diagnostics; accept replaces the rule and is undoable
- [ ] Compare reports equivalence using `RuleEquivalence` and respects `MaxAnalysisTerms`
- [ ] The page for this feature is written or updated in the same change (`docs/tui.md`, linked from the README and `docs/cli.md`), and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
