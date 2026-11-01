# 01 - Coverage and Explain with a quantifier over a null collection

Status: ready-for-agent
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
- [ ] A test through the public interface (compile a rule with a quantifier, evaluate it with a null collection, then call coverage and `Explain`). If it passes today, keep it as a pin and say so in the Resolution; if it fails, fix it.
- [ ] The existing mismatch tests (`RuleCoverageMismatchedDecisionTests`) pass unchanged.
- [ ] The validation list in CLAUDE.md passes.

**Out of scope:** Changing the trace text or the public `TraceNode` (ticket 10).
