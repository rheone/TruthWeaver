# 09: Functions and external operators: COALESCE, If, inspections

**What to build:** Documents for COALESCE (alias ?? and n-ary form; replaces only Unknown; associative), If (alias ? :; the consensus term so that If(Unknown, A, A) = A, the strongest extension of if-then-else, contrasted with the bare multiplexer, McCarthy's conditional and SQL CASE), and IsTrue, IsFalse, IsUnknown and IsKnown (definite results, SQL IS TRUE / IS UNKNOWN precedents). Each is marked as an external operator rather than a Strong K3 connective (not information-monotone), with the classification ambiguity recorded as an open question where the owner has not decided the category. A decision-flow diagram for If if it helps.

**Blocked by:** 05, k3-followups 21

**Status:** done

- [x] Documents conform to the template; the external-operator caveat is stated in each
- [x] If has a 27-triple evaluation table verified by the harness; the SQL CASE equivalence is documented
- [x] COALESCE associativity is verified, not asserted
- [x] Classification ambiguities appear in the unresolved-questions list
- [x] Every table, formula and canonical form is verified against the independent Strong K3 oracle by the verification harness (ticket 02); relative links resolve
- [x] Written with the github-markdown skill conventions; Mermaid diagrams (only where they materially help) use the mermaid-diagram-generator skill and are verified to be semantically identical to the documented formula

Source: [spec audit](../../k3-conformance/spec-audit.md) section C. See also [spec](../spec.md).

## Comments

- 2026-10-03: Added `functions/coalesce.md`, `if.md`, `istrue.md`, `isfalse.md`, `isunknown.md` and `isknown.md` and linked them from `functions/README.md` (pending -> linked). Every document opens with the external-operator caveat (COALESCE and the inspections) or states why `If` is a connective, and notes the classification question (proposal open questions 3, 4, 5, owner-accepted recommendation A) for the ticket 11 unresolved-questions list. Harness-checked: `k3:truth` tables (COALESCE for 2 and 3 operands, the 27-triple `If` table, 3-row inspection tables) and `k3:canonical` forms, including COALESCE associativity (both groupings against the three-operand oracle), `Unknown` as identity, `COALESCE(a, b) = If(IsKnown(a), a, b)`, the `If` consensus form, `If(c, True, f)` = `OR`, `If(c, t, False)` = `AND`, `If(a, b, True)` = `IMPLIES`, and the inspection definitions and mutual relations. The SQL `CASE` equivalence `If(IsTrue(c), t, f)` and the differing triples against the bare multiplexer (1), McCarthy (2) and SQL `CASE` (4) are prose and a table (hand-derived, re-checked with a script, not harness-checked, because the harness compares a marked form only with the operation's own oracle). One Mermaid decision-flow diagram for `If`. `K3Reference` tests pass (16). No source or test files changed.
- Verified against the engine with a throwaway probe (not committed): `COALESCE` and `If` and the inspections need 2 or more, exactly 3 and exactly 1 operands (`BRE0014`); `??` chains flatten to one node, `??` and the ternary mixed with `AND`/`OR`/each other without parentheses are `BRE0007`; `If`, `IsTrue` and the other call names are case-insensitive and have no call-less form (`BRE0001`); JSON/YAML ops `coalesce`, `if`, `isTrue`, `isFalse`, `isUnknown`, `isKnown` (case-insensitive); COALESCE short-circuits at the first definite value (rest `NotEvaluated`, no fault), `If` evaluates only the needed branch for a definite condition and both for `Unknown`, exhaustive mode evaluates all; a faulting operand is `Unknown` plus one `Fault` (`IsTrue(boom)` is `False`, `IsUnknown(boom)` is `True`, `If(boom, t, t)` is `True`); `ExpandToPrimitives` expands `If` and the inspections, `ExpandToNand`/`ExpandToNor` keep `COALESCE`; `Simplify`/`Canonicalize` flatten nested `COALESCE`; `CompressToDerived` restores `If`, `IsFalse`, `IsUnknown`, `IsKnown` but not `IsTrue` from `COALESCE(x, False)`.
