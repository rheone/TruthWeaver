# 17: Pin the evaluator to the analyzer rails and fold EvaluateExactlyOne

**What to build:** A property test asserts, for generated rules and every True/False/Unknown assignment, that the evaluator's value equals the value read from the analyzer's dual rails (today the analyzer is pinned to the oracle only for tautology and contradiction verdicts). The evaluator's separate `ExactlyOne` evaluation is folded into the threshold path with no change to the conformance tests. The repeated "unhandled" throw arms across the switches are left alone, because each switch is exhaustive over a closed hierarchy. This resolves k3-hardening 11.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] A property test compares evaluator values with analyzer-rail values across generated rules and all assignments, and a deliberately wrong analyzer rail makes it fail
- [ ] The separate ExactlyOne evaluation is removed in favour of the threshold path with no change to the K3 conformance tests
- [ ] k3-hardening 11 is marked resolved by this ticket
- [ ] The full validation from CLAUDE.md passes

Source: owner grilling session, 2026-10-03 (decisions Q1-Q24).
