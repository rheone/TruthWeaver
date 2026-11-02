# 01 - Coverage and Explain with a quantifier over a null collection

Status: done
Category: bug
Severity: low
Blocked by: none
Effort: [architecture-deepening-3](../spec.md)

## Finding

The trace tree writes a quantifier over a null collection as `"{description} [no collection]"`. Rule coverage pairs a trace node with the rule by position and checks the node text against the quantifier's plain description; a mismatch throws. The review expects coverage and `Explain` to throw for such a decision. It did not run it.

## Agent Brief

**Category:** bug
**Summary:** Make coverage, counterfactual reading and `Explain` accept a decision for a quantifier whose collection was null.

**Current behavior:** Not confirmed by running. By reading, pairing compares the text exactly and rejects the `[no collection]` suffix.

**Desired behavior:** A decision made over a null collection can be passed to rule coverage, rule counterfactual and `Explain` for the same rule. The quantifier node is recorded as the evaluation recorded it. A tampered decision is still rejected with `ArgumentException`.

**Acceptance criteria:**
- [x] A test through the public interface (compile a rule with a quantifier, evaluate it with a null collection, then call coverage and `Explain`). If it passes today, keep it as a pin and say so in the Resolution; if it fails, fix it.
- [x] The existing mismatch tests (`RuleCoverageMismatchedDecisionTests`) pass unchanged.
- [x] The validation list in CLAUDE.md passes.

**Out of scope:** Changing the trace text or the public `TraceNode` (ticket 10).

## Resolution

Status: done

Confirmed by running: coverage and `Explain` threw `ArgumentException` at `$` for a quantifier over a null collection. `TraceText.Matches` now accepts the quantifier description with the `[no collection]` suffix, and `CollectionBinding` builds the suffix from `TraceText.NoCollectionSuffix`. `NullCollectionQuantifierDecisionTests` covers coverage and `Explain`; both were red before the fix. The `RuleCoverageMismatchedDecisionTests` mismatch tests pass unchanged.

`RuleCounterfactual.FindChanges(rule, decision)` was not fixed. It rejects every decision of a rule with a quantifier, null collection or not (a `[True]` collection fails at `$.operands[0]`), because it pairs the quantifier's element children with the quantifier's one body operand. The owner chose to keep this ticket to the null-collection bug. Follow-up: [11](11-counterfactual-with-a-quantifier-decision.md).

Review follow-up: `TraceText.Matches` now takes the trace node and accepts the `[no collection]` text only on a quantifier node with an `Unknown` result and no children. Added tests: a nested quantifier over a null collection, an `Explain` assertion on the Unknown result, and three tampered-decision tests (the suffix on a node with elements, on a definite result, and on another quantifier) that stay rejected with `ArgumentException`.
