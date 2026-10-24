# 33 - A quantifier variable that shadows a declared source keeps the hidden source validator for element queries

Status: needs-triage
Severity: medium
Lens: F8 Predicate+DataSource concepts
Verdict: PLAUSIBLE
Verified 2026-10-24: reproduced - declaring outer source "user" with `JsonQueryValidator.Instance`, then compiling `FORALL(user IN orders, readsTotal(val: from("user", "total")))` where the `orders` element source accepts the flat key `"total"` (not JSONPath), fails to compile with `TRE0025` (`MalformedDataQuery`) because `DeclareElementSource` keeps the outer validator for the shadowing variable name.
Blocked by: none

## Finding F8.4: A quantifier variable that shadows a declared source keeps the hidden source's validator for element queries

Location: `src/TruthWeaver/Registry/CollectionBinding.cs:251` (PLAUSIBLE, medium)

DeclareElementSource copies the outer declarations and then calls declared.Add(variable), which keeps an existing validator. At evaluation, WithElementSource (line 268-276) makes the element source win over the outer source of the same name. So compile-time syntax checks for from(variable, ...) run in the dialect of a source the body can never reach. The ADR-0010 amendment states both rules (keep the validator, the element wins), and together they contradict ADR-0006 decision 4: a validator checks a query in that source's dialect.

### Failure scenario

The outer "user" is declared with JsonQueryValidator.Instance. A collection "orders" has an elementSource that projects to a custom key-value source whose dialect is "total". The rule FORALL(user IN orders, over(limit: from("user", "total"))) fails to compile with TRE0025 (not valid JSONPath), although the query is valid for the source it reaches at evaluation. The reverse case also happens: a query that is malformed for the element source is not flagged because JSONPath accepts it.

### Expected behavior

Inside the body, declare the variable without a validator (replace the outer entry), or report a shadowing diagnostic when the variable name equals a declared source.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
