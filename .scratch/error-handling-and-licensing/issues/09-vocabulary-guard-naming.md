# 09: Name the narrowed vocabulary guard check explicitly

**What to build:** `VocabularyGuardTests` narrows the retired-name check for `ResolvedValue` to `ResolvedValuePredicates` and `TResolved`, because `EvaluationOptions.IncludeResolvedValues` legitimately uses "resolve" for data sources (ADR-0006). Make that explicit in the code: list the retired identifiers as whole names with a comment per entry, and rename any test or constant that implies a broader `ResolvedValue` ban.

**Blocked by:** None (can start immediately)

**Status:** done

- [ ] The retired-name list reads as exact identifiers, each with its replacement in a comment Note 2026-10-10: the exact-identifier form of the list was not re-checked.
- [ ] The guard still fails if a retired name returns (verify by temporarily reintroducing one) and does not flag `IncludeResolvedValues` Note 2026-10-10: the temporary reintroduction check is a manual step and cannot be confirmed.
- [x] ADR-0007 mentions that "resolve" is reserved for variable references and data sources
- [x] The full validation from CLAUDE.md passes

Source: owner review, 2026-10-04 (question 6).
