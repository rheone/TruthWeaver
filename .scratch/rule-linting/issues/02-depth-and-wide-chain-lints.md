# 02: Depth-near-limit and wide-chain lints

**What to build:** Two new opt-in lint rules that warn before a rule hits a compile limit or becomes hard to read.

**Blocked by:** None (can start immediately)

**Status:** ready

- [ ] `LintRules` gains `DeepNesting` and `WideChain` flags, and `All` includes both
- [ ] `DeepNesting` reports a rule whose depth reaches a fraction of `CompilerOptions.MaxDepth` (default 0.75)
- [ ] `WideChain` reports an `AND`/`OR` chain with more operands than the `CompilerOptions` width (default 16)
- [ ] Each rule has its own `DiagnosticCodes` entry (after `TRE0027`) and a diagnostics reference entry
- [ ] Both thresholds are `CompilerOptions` values with documented defaults
- [ ] A rule under both thresholds produces no finding
- [ ] Carries XML docs, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes

See also [spec](../spec.md).
