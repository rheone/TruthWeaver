# 22: Enumerable overloads for the counted operators

**What to build:** `RuleBuilder.Between`, `AtLeast`, `AtMost` and `Exactly` gain `IEnumerable<RuleBuilder>` overloads, extending ticket 17 (which covered `And`, `Or`, `Parity`, `Any`, `All`, `None`, `ExactlyOne` and `Coalesce`). The overloads build the same node as the `params` overloads and go through the same count validation, with no folding of empty or short lists, because counted operators have no identity constant. A count that cannot be met (for example `AtLeast(2, [])`) is the same diagnostic as with `params`. A null sequence throws `ArgumentNullException`, and the sequence is enumerated once. The README `RuleBuilder` section documents the overloads and warns that an empty list is probably a bug. Owner decision 2026-10-03: these shortcuts improve readability of builder code.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] The failing test run is shown before the implementation
- [ ] Each of the four operators has an `IEnumerable<RuleBuilder>` overload, documented
- [ ] Many-item lists build the same tree as the `params` overload
- [ ] Empty and too-short lists get the same validation outcome as `params`, with no folding
- [ ] A null sequence throws and the sequence is enumerated once
- [ ] README documents the overloads and the empty-list warning
- [ ] The full validation from CLAUDE.md passes

Source: owner decisions 2026-10-03 (Q9, Q10); builds on [ticket 17](17-optional-builder-enumerable-overloads.md).
