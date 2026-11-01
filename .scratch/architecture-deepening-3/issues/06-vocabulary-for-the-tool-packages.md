# 06 - The tool packages read the vocabulary from the library

Status: needs-triage
Category: enhancement
Severity: low
Blocked by: 05
Effort: [architecture-deepening-3](../spec.md)

**What to build:** `RuleTreeEditor` (Tool.Core) and `InspectController`, `InsertItems` and `SyntaxMarks` (Tool.Tui) stop keeping their own word lists, arity sets and spellings. The owner's note: where making a currently internal type public improves the seam and the implementation, consider that, not only a read-only view of the table (ADR-0004 keeps the tool packages from seeing internals). Facts they need: the rule-text word and aliases, the tree-format name, minimum and maximum operands, whether it takes a count or a range, the symbols, quantifier words, constant words, `LET` and `IN`. Facts the table lacks today: family, dual and "external".

**Needs:** a design decision on what becomes public, and an Agent Brief, once ticket 05 lands.
