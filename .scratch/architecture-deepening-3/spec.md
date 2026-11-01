# Architecture deepening 3: scope, vocabulary and node identity

**Status:** needs-triage (owner decisions recorded 2026-11-01; wave 1 tickets are released, later tickets are released in order)

## Goal

Turn five shallow modules into deep ones, so that a change to scope, spelling or provenance happens in one place. The review that found them looked at the recent hot spots (commits since 2026-10-10) and checked each claim against the code. The vocabulary is that of the `codebase-design` skill (module, interface, depth, seam, leverage, locality).

## Decisions (owner, 2026-11-01)

| Candidate | Decision |
| --- | --- |
| Evaluation run | The seam sits at the quantifier crossing. A frame (run, scope, memo scope, registry) goes to the collection binding. The state, memo keys and fault recorder sit behind it. The public `EvaluateAsync` and `EvaluateManyAsync` do not change. |
| Compile scope | One compile-scope value carries depth, quantifier depth, bound variables, the node budget with its reported flag, and the library scope. It crosses `Compile`, `CompileBody` and the library rule check. |
| Vocabulary | Step 1 stays inside the library: a completeness test, then the parser keywords, aliases and reader quantifier words derive from the operator table. Step 2 follows only after step 1: the tool packages stop keeping their own lists. Step 2 may make internal types public, not only a read-only view, where that improves the seam and the implementation. |
| Node identity | The tree path is the stable identity, kept internal. Provenance (notes, library locators) is recorded by path while the node is built. Coverage and counterfactual share one internal pairing module. The public `TraceNode` and the decision-record schema do not change. |
| Diagnostic catalogue | Stays a note: drift is caught at commit, and there is one real adapter (SARIF). The only action, if wanted later, is to delete the two redundant `DiagnosticCodeConsistencyTests` doc tests. |

## Order

Wave 1 (small, parallel, each leaves the build green): tickets 01 to 04. Then the vocabulary (05, 06), the compile scope (07), the evaluation run (08), provenance (09) and the trace pairing module (10). The `Blocked by` lines hold that order.

## Facts the review corrected

- Memo keys are built at 6 sites in 2 files, not 3 files. Three bug tickets (34, 45, 71) were a key part one caller left out.
- The library rule check compile passes none of the four scope parameters (ticket 86). A body's over-budget report may not set the outer flag, so a node after the quantifier may report the limit twice. Neither is tested.
- Notes are keyed by tree path (public in `CompiledRule.Notes`); locators are keyed by node reference. The silent give-up in the two collectors is close to unreachable today.
- Coverage pairs trace nodes by position and uses the text as a guard. A null collection writes `"... [no collection]"` as tree text, which the guard would reject. Not run.
- Only tickets 59, 76 and 77 were spelling drift; 02 and 73 were reserved-word precedence bugs and 72 was `IN` recovery in the parser.

## ADR touchpoints

ADR-0002 and ADR-0007 (trace shape), ADR-0004 (the tool packages cannot see internals), ADR-0008, ADR-0010 and ADR-0019 (expand before build, `call("x")` folds into the caller's run), ADR-0014 (coverage works on recorded decisions; a mismatch is rejected with `ArgumentException`).
