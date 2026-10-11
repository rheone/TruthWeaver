# 03: Not-canonical lint

**What to build:** An opt-in lint that tells an author when `Canonicalize()` would change the rule, so stored rules converge on one form.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `LintRules` gains `NotCanonical`
- [x] The lint fires when `Canonicalize()` produces a different tree, and does not fire when it produces the same tree
- [x] The finding carries the canonical rule text as a replacement suggestion
- [x] The lint does not run when the rule exceeds `MaxRewriteNodeCount`. `Canonicalize()` itself has no cap; the guard keeps the opt-in lint from adding a full rewrite pass to compiling a very large rule (decided 2026-10-10)
- [x] A new `DiagnosticCodes` entry and diagnostics reference entry
- [x] `LintRules.All` includes `NotCanonical`
- [x] Documentation updated in the same change: `docs/diagnostics.md` lists the new code and flag, and `docs/rewriting-rules.md` links to it from the `Canonicalize()` description
- [x] Carries XML docs, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes

See also [spec](../spec.md).
