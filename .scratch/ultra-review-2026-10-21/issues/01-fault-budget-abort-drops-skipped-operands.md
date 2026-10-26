# 01 - A fault-budget abort drops skipped AND/OR/COALESCE/quantifier operands, so an aborted evaluation can return a definite result

Status: done
Category: bug
Severity: high
Lens: F4 Evaluation; F9 Policies
Verdict: CONFIRMED
Blocked by: none

## Finding F4.1: Fault-budget abort drops skipped AND/OR/COALESCE and quantifier operands from the fold, so an aborted evaluation can return a definite (even True) result

Location: `src/TruthWeaver/Evaluation/Evaluator.cs:488` (CONFIRMED, high)

Once state.Aborted is set, EvalChainAsync marks the remaining operands NotEvaluated and continues without combining Unknown into the accumulator. CollectionBinding.EvaluateQuantifierAsync (src/TruthWeaver/Registry/CollectionBinding.cs:162) does the same for the remaining elements. Whatever the fold reached before the abort is returned as if the skipped operands could not change it. docs/diagnostics.md:170 says the terms not yet run count as Unknown. The non-chain operators get this right: they go through EvalAsync, which returns Unknown at Evaluator.cs:140.

### Failure scenario

FaultBudget = 1, rule `IsUnknown(boom) AND isOff` (boom throws). IsUnknown(boom) is True and the abort fires. isOff is skipped and never folded, so Result = True and IsSatisfied = true: the check fails open. Without a budget the result is False, and per the docs it should be AND(True, Unknown) = Unknown. Same with `AtMost(5, boom) AND isOff`, and with `EXISTS(x IN items: boomOrTrue)` where element 0 faults: the result is False although element 1 was never run.

### Verification

Traced. Evaluator.cs:657-659 sets state.Aborted = true when Faults.Count >= FaultBudget. EvalChainAsync (Evaluator.cs:486-494) then takes the `stop || this.state.Aborted` branch for each remaining operand. It only adds a NotEvaluated trace node and `continue`s, so it never runs `accumulator = combine(accumulator, Unknown)` (line 498). The accumulator therefore stays at whatever the fold reached.

Scenario `IsUnknown(boom) AND isOff` with FaultBudget=1: boom throws, so a fault is recorded and the budget aborts. IsUnknown(Unknown) returns True (a normal evaluation, since EvalAsync checked Aborted before the term ran), and the AND accumulator becomes True. isOff is skipped without being combined. The chain returns True. Decision.IsSatisfied (Decision.cs:33) is just `Result == True`, so it returns true. It does not check Faults or an aborted flag. By the docs, the skipped operand is Unknown, so AND(True, Unknown) = Unknown.

CollectionBinding.cs:162-167 does the same for quantifier elements on abort. Its comment at lines 145-148 shows the behavior is deliberate: the fold keeps what it reached ("exactly as today"). This contradicts docs/diagnostics.md:170 and CompilerOptions.cs:16 (unrun terms are Unknown). The MaxElementEvaluations path forces Unknown at lines 215-218, but the fault-budget path has no such fix. Non-chain operators go through EvalAsync, which returns Unknown at lines 140-148, so they are correct. A skipped AND/OR/COALESCE operand and a skipped element are the inconsistent cases. For OR, `IsUnknown(boom) OR x` aborting early gives False where Unknown is correct. For AND, a True partial fold gives a fail-open True.

Severity: the defect is real and fail-open, but it only triggers when the caller opts into FaultBudget and the rule has an external operator (IsUnknown, IsFalse and so on) over a faulting term. The result carries Faults, and the false-negative direction (False instead of Unknown) is fail-closed. I rate it high, not critical.

## Finding F9.3: A fault-budget abort in an AND/OR chain drops the skipped operands, so a policy can be definitely False or True

Location: `src/TruthWeaver/Evaluation/Evaluator.cs:488` (CONFIRMED, high)

In EvalChainAsync, an operand skipped because state.Aborted is set adds nothing to the accumulator. The chain keeps the fold of the operands before the abort. When the operand that faulted is wrapped in an inspection (IsTrue/IsFalse/IsKnown/IsUnknown), it gives a definite value. The chain can then return False for OR (or True for AND) while a later operand was never evaluated. In K3, an unevaluated operand must count as Unknown. A policy set makes this easier to trigger, because faults from earlier rules count against the same budget. This is core evaluator code outside the Policies slice. It is reported here because it causes a silent permit through a set.

### Failure scenario

DenyOverrides with FaultBudget = 1. Permit policy `staff` is True. Deny policy has condition `IsTrue(isSanctioned) OR isBlocked`. isSanctioned throws: IsTrue gives False, the budget aborts, and isBlocked is skipped. The OR returns a definite False, so the deny does not apply and the set answers Permit. isBlocked, which would have been True, never ran.

### Expected behavior

An operand skipped because of an abort folds as Unknown, so the chain is Unknown. The deny policy is then Unknown and DenyOverrides answers Indeterminate.

### Verification

Traced in src/TruthWeaver/Evaluation/Evaluator.cs. For OR(IsTrue(isSanctioned), isBlocked) with FaultBudget=1:
1. EvalChainAsync (474-509) starts with accumulator=False (the OR identity). Operand 1 is an InspectionExpression, so EvalAsync (215-218) runs it.
2. isSanctioned throws. InvokeCoreAsync (638-646) calls RecordFault and returns Unknown. RecordFault (650-661) sets state.Aborted = true because Faults.Count >= budget.
3. K3Logic.Inspect(IsTrue, Unknown) returns False, a definite value. Line 498 folds it, so accumulator = False. False is not the OR stop value (True), so stop stays false.
4. For operand 2, line 488 (`stop || this.state.Aborted`) is true. The chain only records a NotEvaluated trace node and `continue`s. It never calls combine with Unknown. The accumulator stays False.
5. The chain returns a definite False at line 505, with no Unknown. Nothing else reads Aborted: it is used only at Evaluator.cs:140/488/659 and CollectionBinding, and it never reaches the result. A caller sees False, and the only sign is the recorded Fault.
EvalAsync (140-148) correctly returns Unknown for a skipped single node, but EvalChainAsync bypasses it. The docs say the same: docs/diagnostics.md:170 says terms not yet run are Unknown in the trace, so the result is usually Unknown. The xmldoc at 467-472 claims stopping never changes the result, which holds only for short-circuit stops, not abort skips.
The scenario works for AND with IsFalse/IsUnknown as well. Without an inspection wrapper, the faulting term is Unknown and the fold usually stays Unknown, so the bug needs an external operator (IsTrue/IsFalse/IsKnown/IsUnknown) to turn Unknown into a definite value.
I did not trace the policy-set layer. The core evaluator defect is confirmed, and the DenyOverrides outcome (Permit instead of Indeterminate) follows if the policy condition is evaluated through this chain and its result is used as-is.
Severity: high. Fail-open is only possible with FaultBudget set and a faulting term wrapped in an inspection. The abort is a deliberate opt-in. The result is a wrong definite value for an authorization decision.

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.

## Comments

> *This was generated by AI during triage.*

## Agent Brief

**Category:** bug
**Summary:** A fault-budget abort inside an AND/OR/COALESCE chain, or a quantifier's element loop, drops the remaining skipped operands from the fold instead of counting them as Unknown, so the chain can return a definite True/False when the correct Strong K3 result is Unknown.

**Current behavior:** Once a fault-budget abort fires mid-chain, the remaining operands are marked not-evaluated and `continue`d past without being folded into the accumulator at all. Whatever partial fold was reached before the abort is returned as the final result. The equivalent single-node evaluation path already treats a skipped term as Unknown; only the chain-fold path (AND/OR/COALESCE and quantifier elements) has this gap.

**Desired behavior:** An operand skipped because of a fault-budget abort folds into the chain as Unknown, matching the single-node path and the documented contract (unrun terms count as Unknown). For AND, OR and COALESCE, and for a quantifier's remaining elements, the chain's result after an abort reflects the Unknown contribution of every skipped operand rather than silently keeping only the pre-abort partial fold.

**Key interfaces:**
- The chain-evaluation fold (the AND/OR/COALESCE path and the quantifier element-evaluation path) must combine Unknown into its accumulator for each operand skipped by an abort, not just continue past it.
- `Decision.Result`/`Decision.IsSatisfied` for a chain containing a faulting term wrapped in an inspection (IsTrue/IsFalse/IsKnown/IsUnknown), under a `FaultBudget` that aborts mid-chain.

**Acceptance criteria:**
- [x] `IsUnknown(boom) AND isOff` with `FaultBudget = 1` and `boom` throwing: the result is Unknown, not True, and `IsSatisfied` is correspondingly not satisfied.
- [x] The equivalent OR and COALESCE cases, and a quantifier whose remaining elements are skipped by the same abort, fold the skipped operands as Unknown.
- [x] A policy-set scenario (DenyOverrides, a faulting term wrapped in an inspection) that previously resolved to a definite Permit now resolves to Indeterminate/Unknown as appropriate.
- [x] The validation list in `CLAUDE.md` passes.

**Out of scope:**
- Changing `FaultBudget`'s semantics for non-chain (single-node) evaluation, which already folds skipped terms as Unknown correctly.
- Any change to how faults themselves are recorded or counted toward the budget.
