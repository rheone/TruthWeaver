# 03: String predicate completions

**What to build:** The string members the Final Semantic Inventory names that the catalog lacks become registerable predicates: `IsEmpty` (a non-null empty string), `IsNotNullOrEmpty`, `IsNullOrWhiteSpace`, `IsNotNullOrWhiteSpace`, `NotContains`, `NotEqual` and `NotMatches`. Follow the catalog rules in `CONTEXT.md`: ordinal comparison only, the null tests (`IsNullOrWhiteSpace` and its complement) are definite, and the new comparison-style members take the `NullBehavior` option. Negated members follow the owner's answer to the `NotX` question from ticket 02 (a K3 complement where `Unknown` stays `Unknown`, so a null selection must not turn into `True` by accident). An invalid `NotMatches` pattern still faults to `Unknown` plus a `Fault`. Each member has XML docs, a schema description and README coverage.

**Blocked by:** 10

**Status:** ready-for-agent

- [ ] The failing test run is shown before the implementation
- [ ] Each of the seven members is registerable and returns the documented value for a null, empty, whitespace and ordinary string
- [ ] Negated members agree with the K3 complement of their positive form, including for `Unknown`
- [ ] `NotMatches` with an invalid pattern yields `Unknown` plus a `Fault`
- [ ] README and the gap list show the members as present
- [ ] The full validation from CLAUDE.md passes

Source: [gap list, String section](../k3-gap-list.md). Rules: [CONTEXT.md](../../../CONTEXT.md).
