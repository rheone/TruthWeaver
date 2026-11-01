# 08 - A frame crosses the quantifier seam and a run owns the state behind it

Status: needs-triage
Category: refactor
Severity: medium
Blocked by: 03, 07
Effort: [architecture-deepening-3](../spec.md)

**What to build:** `EvaluateQuantifierAsync` (13 parameters, nine of them run-wide) takes one frame: the run, the scope, the memo scope and the registry. The collection binding makes the element frame. Behind the seam, one module owns the state, the memo keys (built at six sites in two files today), the fault recorder, and the identity of a collection fault (`TermIdentity(name, [])`, a rule that lives in three files). `PolicySet` shares one state through internals and must keep doing so. The shape must let `call("x")` (ADR-0019) fold a callee into the caller's run, fault budget, trace and timeout.

**Constraints:** ADR-0001, 0002, 0006, 0010, 0011, 0012, 0015, 0019; each element allocates a new evaluator today, so no extra allocation per term; AOT clean. Replace, do not layer, the two tests coupled to internals (`EvaluatorDefensivePathTests`, one in `EvaluationOptionsTests`).

**Needs:** an Agent Brief once tickets 03 and 07 land.
