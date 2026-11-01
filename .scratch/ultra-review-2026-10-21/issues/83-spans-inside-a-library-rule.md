# 83 - Every node and diagnostic from a library rule points at the reference, not into the library text

Status: done
Category: enhancement
Severity: low
Lens: Code review of ticket 11b-2
Blocked by: none

## Finding

Ticket 11b-2 places every node and diagnostic that comes from a library rule at the span of the `rule("x")` reference in the caller's text. A rule with two predicates where one fails at analysis or evaluation then points both at the same span, so a caller cannot tell which part of the library rule is involved. The expanded body is also cached and shared across references, and the span rewrite copies only the top level of each shared node.

## Open question for the owner

Where should a trace entry or an analyzer diagnostic point for a node inside a library rule: the reference in the caller's text (today), the node's span in the library rule's own text (needs a way to name which rule), or both?

## Next step

Decide, then write an Agent Brief. Tickets 82 and 83 may merge, since both depend on how a library rule's diagnostics map back to text.

## Decision (owner, 2026-11-01)

Keep the reference span in the caller's text, and add a library locator: the library rule's name and the node's offset inside the library text, in the diagnostic message and the trace entry. No new span type.

## Agent Brief

**Category:** enhancement
**Summary:** Make a diagnostic or trace entry for a node inside a library rule say which library rule and where, while its span stays at the `rule("x")` reference.

**Current behavior:** Every node and diagnostic from a library rule has the span of the reference. Two predicates in one library rule cannot be told apart.

**Desired behavior:** A diagnostic that originates in a library rule's node (an analyzer finding, a lint, a compile error from a nested node) carries the library rule's name and the node's offset (or line and column) inside the library text in its message. A trace entry for such a node carries the same locator in its text. The span stays the reference's span. No new public span type.

**Acceptance criteria:**
- [x] A test fails before and passes after: a library rule with two predicates, one of which raises a diagnostic, gives a message that names the library rule and a position inside it.
- [x] A trace entry for a node from a library rule names the library rule.
- [x] A diagnostic from caller text is unchanged.
- [x] Docs (`docs/rule-text.md` Library rules, `docs/strong-k3/specification/diagnostics.md`) state the locator.
- [x] The validation list in CLAUDE.md passes, with a CHANGELOG entry.

**Out of scope:** Warnings from library rules (ticket 82, which needs this locator).

## Resolution (2026-11-01)

New internal `LibraryLocator` (`src/TruthWeaver/Parsing/LibraryLocator.cs`): the library rule name and the 1-based line and column in its text, written `library rule 'adult' at line 1, column 7`. `RuleNode.Library` holds it.

- `DefinitionExpander.Expand` takes the `LibraryRule` it expands. Inside a library rule each node keeps the reference span (unchanged, so the per-site cache key `(name, pin, site span)` is unchanged) and gets the locator of where it is written; a node from a nested library rule, a definition body or an argument keeps its own. The expansion's own diagnostics keep library-text spans (the node-budget count in `ExpandReference` now uses the reference span instead of the site), so `LibraryRuleExpander.AsCallerDiagnostic` can locate parse and expansion errors in the text.
- `RuleNodeCompiler.Build` wraps the node build: a diagnostic raised while building a located node, and not already located by an inner node, gets the reference span, the locator as a closing sentence (`In library rule 'adult' at line 1, column 7.`), and loses a replacement suggestion (a hint stays). A quantifier body's diagnostics are marked as located.
- `TRE0050` wraps the located message: `Library rule 'adult' does not compile here: <code> <message> In library rule 'adult' at line 2, column 3.`
- Trace: `LibraryLocatorCollector` maps each compiled node from a library rule (by reference) to its locator; `CompiledRule` carries it like the notes (rewrites drop it); `EvaluationState.AddLibraryLocators` merges it (works for a `PolicySet` shared state); `Evaluator.AddTraceEntry` appends ` (in <locator>)` through `TraceText.WithLibraryLocator`. Only flat `Decision.Trace` entries change; the trace tree, coverage and printers are unchanged. The entry of a quantifier itself (written by `CollectionBinding`) gets no locator; its body nodes do.

Tests (`RuleLibraryTests`): `Compile_OneOfTwoPredicatesInALibraryRuleFails_MessageNamesTheRuleAndThePosition_Test`, `Compile_SyntaxErrorInALibraryRule_MessageNamesTheRuleAndThePosition_Test`, `Compile_ErrorInTheCallerText_MessageNamesNoLibraryRule_Test` (guard, passed before and after), `EvaluateAsync_NodeFromALibraryRule_TraceEntryNamesTheLibraryRule_Test`. Red first: the three new behavior tests failed (`Sub-string not found` for the two messages, `Item not found in collection` for the trace entry). Docs: `docs/rule-text.md` (Library rules), `docs/strong-k3/specification/diagnostics.md` (`TRE0050`). CHANGELOG entry under Changed.
