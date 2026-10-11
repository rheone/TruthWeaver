# 01: Deprecation marker and TRE0027

**What to build:** A predicate author marks a predicate deprecated, and rule authors see a warning with the replacement on every use.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `PredicateSchema.Deprecation` is an optional `PredicateDeprecation(ReplacedBy, Message)` set with `init`; null means not deprecated
- [x] `TRE0027` is a warning, one per use, at the call, and the rule still compiles
- [x] The message names the replacement and the replacement is also a `DiagnosticSuggestion`
- [x] `TRE0027` is documented in `DiagnosticCodes` and the diagnostics reference
- [x] A registered predicate without `Deprecation` produces no new diagnostic
- [ ] Carries XML docs on all public API, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes. Note 2026-10-10: the feature commit exists and CI is green on the branch head, but a pre-commit hook pass cannot be confirmed from history.

See also [spec](../spec.md).
