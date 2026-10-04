# 37: Document the RuleBuilder array-versus-list difference

**What to build:** Every `RuleBuilder` overload pair where a `params` array and an `IEnumerable` list behave differently (the array form rejects zero or one operands, the list form folds to a constant or the single operand) shows that case in its XML documentation, with an array example and a list example side by side, and the README has a short note. The fold behaviour is a deliberate decision and does not change, and there is no analyzer or API change. This resolves k3-followups 33.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Each affected overload pair's XML docs show the array-versus-list case
- [ ] The README has the note
- [ ] k3-followups 33 is marked resolved by this ticket
- [ ] The full validation from CLAUDE.md passes

Source: owner grilling session, 2026-10-03 (decisions Q1-Q24).
