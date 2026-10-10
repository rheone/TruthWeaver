# 03: Not-canonical lint

**What to build:** An opt-in lint that tells an author when `Canonicalize()` would change the rule, so stored rules converge on one form.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `LintRules` gains `NotCanonical`
- [x] The lint fires when `Canonicalize()` produces a different tree, and does not fire when it produces the same tree
- [x] The finding carries the canonical rule text as a replacement suggestion
- [x] The lint does not fire when canonicalization is refused for size (`MaxRewriteNodeCount`)
- [x] A new `DiagnosticCodes` entry and diagnostics reference entry
- [x] `LintRules.All` includes `NotCanonical`
- [x] Documentation updated in the same change: `docs/diagnostics.md` lists the new code and flag, and `docs/rewriting-rules.md` links to it from the `Canonicalize()` description
- [x] Carries XML docs, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes

See also [spec](../spec.md).
