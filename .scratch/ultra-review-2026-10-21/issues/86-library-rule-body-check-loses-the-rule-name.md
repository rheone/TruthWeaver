# 86 - A shadowed variable or depth error inside a library rule in a quantifier body is reported without the library rule's name

Status: ready-for-agent
Category: bug
Severity: low
Lens: Follow-up to ticket 79
Blocked by: none

## Finding

Ticket 79 makes a library rule used inside a `FORALL` or `EXISTS` body resolve against that body's element registry. The agent noted that the check build of such a library rule runs at quantifier depth 0 with no inherited variables. A shadowed-variable or quantifier-depth error inside that library rule is reported by the caller's build without the library rule's name (the locator from ticket 83 may not apply to it).

## Open question for the owner

Should the check build inherit the enclosing quantifier depth and bound variables, so `ShadowedVariable` and `MaxQuantifierDepth` apply to the library rule as if its text were written in place, and the message names the library rule?

## Next step

Decide, then write an Agent Brief.

## Decision (owner, 2026-11-01)

The check build of a library rule used inside a quantifier body inherits the enclosing quantifier depth and bound variables, as a `let` definition does. `ShadowedVariable` and `MaxQuantifierDepth` apply to the library rule's text as if it were written in place, and the message carries the library locator.

## Agent Brief

**Category:** bug
**Summary:** Make a library rule referenced inside a `FORALL` or `EXISTS` body see the enclosing depth and bound variables when it is checked.

**Current behavior:** The library rule's check build runs at quantifier depth 0 with no inherited variables. A shadowed variable or a quantifier-depth error inside it is reported by the caller's build without the library rule's name, and a library rule can nest past `MaxQuantifierDepth` unnoticed.

**Desired behavior:** The check runs with the enclosing quantifier depth and the bound variable names in scope. A library rule that reuses an enclosing variable name gives `ShadowedVariable`; one that nests past `MaxQuantifierDepth` gives the existing depth diagnostic. Each message carries the library locator (the library rule's name and the position in its text).

**Acceptance criteria:**
- [ ] A test fails before the fix and passes after: a library rule `FORALL(line IN lines, ...)` referenced inside a `FORALL(line IN lines, ...)` body gives `ShadowedVariable` naming the library rule.
- [ ] A test: a library rule with one quantifier, referenced at the maximum nesting depth, gives the depth diagnostic naming the library rule.
- [ ] A library rule with no quantifier, referenced in a body, is unchanged.
- [ ] Existing `RuleLibraryTests` pass unchanged.
- [ ] `docs/rule-text.md` (Library rules) states that a library rule is checked as if written in place. CHANGELOG entry. The validation list in CLAUDE.md passes.

**Out of scope:** The locator gaps listed in ticket 87.
