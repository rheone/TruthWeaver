# 17: Move rule text, rule formats and RuleBuilder; delete the Operators section

**What to build:** A reader finds how to write rule text on `docs/rule-text.md` (the EBNF grammar, `GroupingStyle`, `RuleText.NormalizeWhitespace` and the case rules), how to choose and convert formats on `docs/rule-formats.md`, and the `RuleBuilder` reference, outline and diagram rendering on `docs/rulebuilder.md`. The README Operators section is deleted. The `docs/strong-k3/` pages replace the symbol table, the precedence rules, the delimiter rules, the arity table, the operator table, the connective explanation and the Collapse text, and the README links to them.

**Blocked by:** 16

**Status:** ready-for-agent

- [ ] The three new pages follow the standard and are not on the baseline
- [ ] `docs/rule-text.md` is added to the doctest list, and its EBNF block keeps its `doctest:skip` marker
- [ ] Every statement from the deleted Operators section is either on a K3 page, on one of the new pages, or dropped as a duplicate; none is lost
- [ ] The README table of contents and every anchor that pointed into the Operators section are updated
- [ ] No link is broken, and the README doctests still pass
- [ ] `dotnet test` passes

See the [plan](../readme-breakdown-plan.md).
