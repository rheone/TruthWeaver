# 17: Move rule text, rule formats and RuleBuilder; delete the Operators section

**What to build:** A reader finds how to write rule text on `docs/rule-text.md` (the EBNF grammar, `GroupingStyle`, `RuleText.NormalizeWhitespace` and the case rules), how to choose and convert formats on `docs/rule-formats.md`, and the `RuleBuilder` reference, outline and diagram rendering on `docs/rulebuilder.md`. The README Operators section is deleted. The `docs/strong-k3/` pages replace the symbol table, the precedence rules, the delimiter rules, the arity table, the operator table, the connective explanation and the Collapse text, and the README links to them.

**Blocked by:** 16

**Status:** done

- [x] The three new pages follow the standard and are not on the baseline
- [x] `docs/rule-text.md` is added to the doctest list, and its EBNF block keeps its `doctest:skip` marker
- [x] Every statement from the deleted Operators section is either on a K3 page, on one of the new pages, or dropped as a duplicate; none is lost
- [x] The README table of contents and every anchor that pointed into the Operators section are updated
- [x] No link is broken, and the README doctests still pass
- [x] `dotnet test` passes

See the [plan](../readme-breakdown-plan.md).

## Comments

2026-10-04: Created `docs/rule-text.md`, `docs/rule-formats.md` and `docs/rulebuilder.md`; deleted the README Operators section and replaced it with a short "Writing rules" section of links. No statement was added to a K3 page. The delimiter diagnostic messages, the grammar context rules, the case rules and the `CStyle` ternary label went to `docs/rule-text.md`. Dropped as obsolete: the `UnknownRequiresResolution` remark. `CONTEXT.md` now links to `docs/rule-text.md#grammar`. Doctests registered for the three new pages.
