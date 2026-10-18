# 16: Document the three kinds of "no value" and the analyzer's independence assumption

**Parent:** [05](05-unknown-consistency-audit.md) D4 and D6 (owner decisions 2026-10-17: keep behavior, document)

**Blocked by:** None

**Status:** done

**What to build:** (D4) Add one table to `docs/data-sources.md` that lists missing, null and empty values and the answer each data-source predicate gives. (D6) State in the analysis docs that the analyzer treats distinct terms as independent, what that means for tautology/implication/overlap results, and what a host can do about correlated terms. No behavior change.

- [x] The table and the independence note are written to current truth
- [x] `DocumentationLint` and `DocExample` pass
- [x] `CHANGELOG.md` has an entry if the docs commit needs one
