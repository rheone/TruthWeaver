# 79 - A library rule used inside a FORALL or EXISTS body is checked at the top-level scope

Status: done
Category: bug
Severity: low
Lens: Follow-up to ticket 11b-2
Blocked by: none

## Finding

Ticket 11b-2 checks each library rule on its own in the top-level scope. A library rule that is referenced inside a `FORALL` or `EXISTS` body and relies on that body's element predicates (the collection's element registry) gets `TRE0050` (`InvalidLibraryRule`), because those predicates are not in the top-level registry. This case was not tested. ADR-0019 decision 4 says a library rule resolves its names against the caller's compile, which can be read as including the scope of the quantifier body.

## Open question for the owner

Should a library rule referenced inside a quantifier body resolve against that body's element registry (so a library rule can be written for a collection element), or against the top-level registry only (so such a rule is an error by design)?

## Next step

Decide, then write an Agent Brief. `let` definitions already resolve inside a quantifier body against that collection's element registry (see the Quantifiers section of `docs/rule-text.md`), which suggests the first reading.

## Evidence from the 11b-2 code review (2026-11-01)

The review confirmed the behavior in the code: each library rule is built once against the top-level registry, so `FORALL(line IN lines, rule("line-ok"))` with a library rule that uses the collection's element predicates reports `TRE0050` even though the quantifier body's own compile holds those names. It has not been run. The decision above still stands as the blocker.

## Decision (owner, 2026-11-01)

A library rule referenced inside a `FORALL` or `EXISTS` body resolves against that body's element registry, the same as a `let` definition. Outside a matching body, a library rule that needs element predicates still fails with `TRE0050`.

## Agent Brief

**Category:** bug
**Summary:** Let a library rule that uses a collection's element predicates compile and evaluate when it is referenced inside that collection's quantifier body.

**Current behavior:** Each library rule is built against the top-level registry, so `FORALL(line IN lines, rule("line-ok"))` reports `TRE0050` when `line-ok` uses element predicates.

**Desired behavior:** The reference expands in the quantifier body's scope. Its predicates resolve against the collection's element registry, and its data-source names against the caller. A nested quantifier body does the same for its own collection. Outside a body, the top-level registry applies, and a missing name gives `TRE0050` as now.

**Acceptance criteria:**
- [x] A test fails before the fix and passes after: a library rule with an element predicate, referenced in a `FORALL` body, compiles and evaluates; the same rule referenced at the top level still gives `TRE0050`.
- [x] A library rule referenced in a nested quantifier body resolves against the inner collection.
- [x] Existing `RuleLibraryTests` pass unchanged.
- [x] `docs/rule-text.md` (Library rules) states the scope rule.
- [x] The validation list in CLAUDE.md passes, with a CHANGELOG entry.

**Out of scope:** `call("x")` (ticket 11b-3); spans (ticket 83).

## Resolution (2026-11-01)

- New internal `ILibraryRuleScope` (`Expand`, `EnterQuantifier`), implemented by `LibraryRuleExpander<TContext>`. The
  `DefinitionExpander.Expander` holds the current scope and, for a `QuantifierNode` body, asks the scope for the scope
  of its collection (`ExpandBody`). A definition body expands in the scope of its use.
- `ICollectionBinding.CreateLibraryScope` makes a `LibraryRuleExpander<Element<TContext, TItem>>` over the element
  registry, with the body options (the variable declared as a data source when the collection projects elements, the
  same `BodyOptions` the body compile uses). An unknown collection keeps the enclosing scope: the build reports
  `UnknownCollection` and does not build the body.
- The parse cache moved to `LibraryRuleParses`, shared by every scope of one compile. The per-site expansion cache and
  the per-rule check are per scope, because they depend on the registry.
- The check build of a library rule in a body runs at quantifier depth 0 with no inherited variables; a shadowed
  variable or quantifier depth error inside such a library rule is reported by the caller's build, without the
  library rule name.

Tests (`tests/TruthWeaver.Tests/RuleLibraryTests.cs`):

- `Compile_LibraryRuleInAQuantifierBody_ResolvesAgainstTheElementRegistry_Test` (theory: reference in the `FORALL`
  body; a library rule that holds the `FORALL`). Red first: both cases gave `TRE0050 ... No predicate named 'shipped'
  is registered`.
- `Compile_LibraryRuleInANestedQuantifierBody_ResolvesAgainstTheInnerCollection_Test`. Red first: `TRE0050 ... No
  predicate named 'partOk' is registered`.
- `Compile_LibraryRuleWithAnElementPredicateAtTheTopLevel_ReportsInvalidLibraryRule_Test`: passes before and after
  (the top-level rule stays).

Docs: `docs/rule-text.md` Library rules states the scope rule. CHANGELOG: a Fixed entry.
