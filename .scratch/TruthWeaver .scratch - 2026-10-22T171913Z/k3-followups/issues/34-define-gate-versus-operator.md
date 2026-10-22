# 34: Define Gate versus Operator in CONTEXT.md

**What to build:** The domain glossary settles the vocabulary clash found by the k3-reference validation: a Gate is the logical concept (NOT, AND and OR as Strong Kleene truth functions) and an Operator is its programmatic implementation (the DSL word, tree node, JSON op or builder member). The glossary's "never called a gate" lines are replaced by that distinction, the reference category label "Gates / Operators" stays, and the reference's unresolved question about the word (U3 in its validation report) is marked resolved.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] The glossary defines Gate and Operator and no longer says operators are never called gates
- [x] The reference terminology page and the glossary agree
- [x] The validation report marks U3 resolved
- [x] The reference verification harness and doctest harness still pass

Source: owner grilling session, 2026-10-03 (decisions Q1-Q24).
