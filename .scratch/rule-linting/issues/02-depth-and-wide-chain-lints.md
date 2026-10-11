# 02: Depth-near-limit and wide-chain lints

**What to build:** Two new opt-in lint rules that warn before a rule hits a compile limit or becomes hard to read.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `LintRules` gains `DeepNesting` and `WideChain` flags, and `All` includes both
- [x] `DeepNesting` reports a rule whose depth reaches a fraction of `CompilerOptions.MaxDepth` (default 0.75)
- [x] `WideChain` reports an `AND`/`OR` chain with more operands than the `CompilerOptions` width (default 16)
- [x] Each rule has its own `DiagnosticCodes` entry (after `TRE0027`) and a diagnostics reference entry
- [x] Both thresholds are `CompilerOptions` values with documented defaults
- [x] A rule under both thresholds produces no finding
- [x] Documentation updated in the same change: `docs/diagnostics.md` lists the two new codes and flags, and the `CompilerOptions` thresholds with their defaults
- [x] Carries XML docs, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes

See also [spec](../spec.md).
