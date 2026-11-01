# 02 - A node-limit report after a quantifier body is reported once

Status: ready-for-agent
Category: bug
Severity: low
Blocked by: none
Effort: [architecture-deepening-3](../spec.md)

## Finding

When a quantifier body goes over `MaxNodeCount`, the compiler reports it from inside the body's compile. The outer compile's "already reported" flag is not set by that report, so a node after the quantifier may report the limit again. The existing tests use `Assert.Contains`, which does not catch a duplicate. Not run.

## Agent Brief

**Category:** bug
**Summary:** Make the node-limit diagnostic appear exactly once, wherever the limit is crossed.

**Current behavior:** Possibly two `MaxNodeCountExceeded` diagnostics when the limit is crossed inside a quantifier body and more nodes follow it.

**Desired behavior:** Exactly one, for a limit crossed in the rule, in a quantifier body, in a nested body, in a `let` use and in a library rule.

**Acceptance criteria:**
- [ ] A test asserts `Assert.Single` of that code for each of those five cases. If a case fails today, fix it (the compiler may use the shared node-count guard instead of its own counter).
- [ ] Existing limit tests (`CompilerLimitsAndLenientMode`) pass unchanged.
- [ ] The validation list in CLAUDE.md passes.

**Out of scope:** The compile-scope value (ticket 07).
