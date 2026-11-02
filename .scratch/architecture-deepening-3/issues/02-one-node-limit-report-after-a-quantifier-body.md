# 02 - A node-limit report after a quantifier body is reported once

Status: done
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
- [x] A test asserts `Assert.Single` of that code for each of those five cases. If a case fails today, fix it (the compiler may use the shared node-count guard instead of its own counter).
- [x] Existing limit tests (`CompilerLimitsAndLenientMode`) pass unchanged.
- [x] The validation list in CLAUDE.md passes.

**Out of scope:** The compile-scope value (ticket 07).

## Resolution

Status: done

Confirmed by running: a limit crossed in a quantifier body, and in a nested body, reported `TRE0010` two and three times. The body compile reports the limit itself, but the outer compile's `nodeLimitReported` flag stayed unset, so a later node reported it again. `RuleNodeCompiler` now sets the flag when a body's diagnostics carry the code. `NodeLimitReportedOnceTests` asserts `Assert.Single` for the rule, a quantifier body, a nested body, a `let` use and a library rule; the `let` and rule cases passed before the fix and stay as pins. A library rule that goes over the limit surfaces as one `TRE0050` that carries the `TRE0010` text, so that test asserts that single diagnostic. `CompilerLimitsAndLenientModeTests` pass unchanged.
