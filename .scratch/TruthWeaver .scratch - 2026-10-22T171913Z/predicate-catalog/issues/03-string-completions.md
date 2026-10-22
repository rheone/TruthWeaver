# 03: String predicate completions

**What to build:** The string members the Final Semantic Inventory names that the catalog lacks become registerable predicates: `IsEmpty` (a non-null empty string), `IsNotNullOrEmpty`, `IsNullOrWhiteSpace`, `IsNotNullOrWhiteSpace`, `NotContains`, `NotEqual` and `NotMatches`. Follow the catalog rules in `CONTEXT.md`: ordinal comparison only, the null tests (`IsNullOrWhiteSpace` and its complement) are definite, and the new comparison-style members take the `NullBehavior` option. Every positive predicate ships with its registered `NotX` twin, defined as the K3 complement (`Unknown` stays `Unknown`, so a null selection must not turn into `True` by accident); the members here that are already negated (`IsNotNullOrEmpty`, `IsNotNullOrWhiteSpace`, `NotContains`, `NotEqual`, `NotMatches`) are the twins of the existing or listed positive form. Any string predicate added here without a listed twin gets one. An invalid `NotMatches` pattern still faults to `Unknown` plus a `Fault`. Each member has XML docs, a schema description and README coverage.

**Blocked by:** 10

**Status:** done

- [ ] The failing test run is shown before the implementation Note 2026-10-10: the failing-first run is not recorded in the repo, so it cannot be confirmed.
- [x] Each of the seven members is registerable and returns the documented value for a null, empty, whitespace and ordinary string
- [x] Negated members agree with the K3 complement of their positive form, including for `Unknown`
- [x] `NotMatches` with an invalid pattern yields `Unknown` plus a `Fault`
- [x] README and the gap list show the members as present
- [x] The full validation from CLAUDE.md passes

Source: [gap list, String section](../k3-gap-list.md). Rules: [CONTEXT.md](../../../CONTEXT.md).

## Comments

- 2026-10-04: Done. `StringPredicates` gains `IsEmpty`, `IsNotNullOrEmpty`, `IsNullOrWhiteSpace`, `IsNotNullOrWhiteSpace`, `NotContains` and `NotEqual`; `RegexPredicates` gains `NotMatches`. Also added `IsNotEmpty` as the twin of `IsEmpty` (catalog rule: every positive predicate has a `NotX` twin). The comparison-style members (`IsEmpty`, `IsNotEmpty`, `NotEqual`, `NotContains`, `NotMatches`) take `NullBehavior` and default to `Unknown`; `NullBehavior.False` answers a definite `False` for null, never `True`. The four null tests are definite and have no option. Documented in `docs/predicates.md`; gap list updated. Tests in `StringCompletionPredicatesTests`.
- 2026-10-04 bookkeeping: the boxes were ticked from what this comment records. No comment shows a red run before the implementation, so that box stays open. The full validation passed on the integrated branch (restore --locked-mode, build, test, csharpier, format, roslynator).
