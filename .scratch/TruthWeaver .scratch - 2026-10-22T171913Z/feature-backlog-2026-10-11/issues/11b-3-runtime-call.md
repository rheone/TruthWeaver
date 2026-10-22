# 11b-3: Runtime call("x")

**Parent:** [11b](11b-rule-library.md)

**What to build:** `call("x")` and `call("x", hash: "...")` in rule text. The node stays in the tree and evaluates another compiled rule of the library at evaluation time. Its value is the call node's value. Its faults join the caller's fault list and count against the caller's fault budget. Its trace nests under the call node. Timeout and cancellation come from the caller's options. The compiler checks existence, pin and cycles with the expand references. If a reload later removes or changes the rule, the call node returns Unknown plus a Fault. Decide in this ticket how analysis (BDD, normal forms, rewrites) treats a call node: as an opaque term, unless the owner prefers otherwise; report the choice. Touches the operator list in the architecture notes: parser, `RuleNodeCompiler`, `Evaluator`, `Analyzer`/`BddManager`, `NodeShape`, `OperatorDefinitions`, printers.

**Blocked by:** 11b-2

**Status:** on-hold (do not start or dispatch until the owner releases the sub-tickets)

- [ ] A call returns the callee's value; its faults and trace appear in the caller's Decision
- [ ] A removed or changed callee returns Unknown plus a Fault and `Decision.IsSatisfied` is false
- [ ] A call counts against the caller's fault budget and respects the caller's timeout and cancellation
- [ ] Analysis treats a call node as an opaque term, and the "no tautologies" note in the docs says calls are not information-monotone claims
- [ ] The pages are updated in the same change (rule text, diagnostics table, evaluation trace, glossary), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
