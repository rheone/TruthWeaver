# 03: Not-canonical lint

**What to build:** An opt-in lint that tells an author when `Canonicalize()` would change the rule, so stored rules converge on one form.

**Blocked by:** None (can start immediately)

**Status:** ready

- [ ] `LintRules` gains `NotCanonical`
- [ ] The lint fires when `Canonicalize()` produces a different tree, and does not fire when it produces the same tree
- [ ] The finding carries the canonical rule text as a replacement suggestion
- [ ] The lint does not fire when canonicalization is refused for size (`MaxRewriteNodeCount`)
- [ ] A new `DiagnosticCodes` entry and diagnostics reference entry
- [ ] `LintRules.All` includes `NotCanonical`
- [ ] Carries XML docs, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes

See also [spec](../spec.md).
