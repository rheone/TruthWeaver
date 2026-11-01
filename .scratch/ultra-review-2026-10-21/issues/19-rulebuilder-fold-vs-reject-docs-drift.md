# 19 - K3 reference pages disagree with RuleBuilder on folding versus rejecting too few operands (And, Or, ANY/ALL/NONE, Parity, Coalesce, GreaterThan/LessThan overloads)

Status: done
Category: bug
Severity: medium
Lens: L1 Connectives; L2 Cardinality+quantifiers; L3 Externals+If+constants
Verdict: CONFIRMED
Blocked by: none

## Finding L1.1: and.md/or.md say RuleBuilder.And/Or(params) reject fewer than two operands; the code folds them to a constant or the operand

Location: `docs/strong-k3/gates/and.md:264` (CONFIRMED, medium)

The 'Empty and single-operand conventions' bullet in and.md (line 264) and or.md (line 269) states that `RuleBuilder.And(params RuleBuilder[])` / `RuleBuilder.Or(params RuleBuilder[])` with fewer than two operands compile to `InfixArityViolation` and that only the `IEnumerable<RuleBuilder>` overload folds. `src/TruthWeaver/Building/RuleBuilder.cs` lines 195-205 and 229-239 fold the params overload too: zero operands become `Constant(True)` for AND (`Constant(False)` for OR) and one operand is returned unchanged. `docs/rulebuilder.md` lines 44-56, the CHANGELOG (line 463) and `RuleBuilderOverloadConsistencyTests` / `RuleBuilderEnumerableTests` all pin the code's behavior, so the K3 reference pages are the stale side.

### Failure scenario

An application builds a permission rule from a dynamic list of checks with `RuleBuilder.And(checks.ToArray())`. The docs promise a `TRE0006` compile error when the list is empty, so the author does not guard the count. The code instead compiles the rule to the constant `True`, and every evaluation grants access.

### Expected behavior

and.md and or.md should state what the code and docs/rulebuilder.md state: both the `params` and the `IEnumerable` overload of `And`/`Or` fold an empty list to the identity constant and a single operand to itself at build time; only the DSL, JSON and YAML reject fewer than two operands with `TRE0006`.

### Verification

src/TruthWeaver/Building/RuleBuilder.cs:195-205 (And) and 229-239 (Or): the params overload switches on operands.Length. 0 gives Constant(True) for And and Constant(False) for Or. 1 returns operands[0]. The IEnumerable overload just calls the params overload via Materialize. The XML docs at lines 187-193 say the same. docs/strong-k3/gates/and.md:264 says the opposite: "RuleBuilder.And(params RuleBuilder[]) with fewer than two operands compile to InfixArityViolation. Only the IEnumerable overload applies the convention." The or.md claim was not opened, but it is the same pattern. The stale-docs claim is confirmed for and.md. The failure scenario is also confirmed: an empty array gives Constant(True), not TRE0006. Severity is lowered to medium. It is a documentation error and the code is correct. The code's own XML docs and rulebuilder.md state the real behavior. The fail-open risk only comes from a reader trusting the stale page.

## Finding L1.2: parity.md says RuleBuilder.Parity(IEnumerable) folds an empty or single-operand sequence; the code builds the node and the compiler rejects it

Location: `docs/strong-k3/derived/parity.md:285` (PLAUSIBLE, medium)

The 'Empty and single-operand conventions' bullet in parity.md states that `RuleBuilder.Parity(IEnumerable<RuleBuilder>)` turns an empty sequence into the constant `False` and returns a single operand unchanged. `src/TruthWeaver/Building/RuleBuilder.cs` lines 327-343 do not fold either overload (the XML remarks say so explicitly), so the node reaches `RuleNodeCompiler.BuildVariadic` and is reported as `InfixArityViolation`. `docs/rulebuilder.md` line 54, the CHANGELOG (line 465) and `RuleBuilderEnumerableTests.Enumerable_overload_of_an_operator_with_no_identity_does_not_fold_Test` pin the non-folding behavior, so the reference page is the stale side.

### Failure scenario

A caller follows parity.md and passes a list that may be empty to `RuleBuilder.Parity(list)` expecting a `False` constant for an empty list. `Compile` instead fails with `TRE0006` and `CompiledRule` is null; code that dereferences it without checking `Succeeded` throws.

### Expected behavior

parity.md should say that neither `Parity` overload folds: a sequence of fewer than two operands builds the node and the compiler reports `InfixArityViolation`, exactly as the `params` form does.

### Verification

not verified (medium or low)

## Finding L2.2: ANY/ALL/NONE pages say the params RuleBuilder overload rejects fewer than two operands; the code folds them

Location: `docs/strong-k3/cardinality/any.md:496` (PLAUSIBLE, medium)

any.md:496, all.md:287 and none.md:2232 (Edge cases) state 'Only `RuleBuilder.Any(IEnumerable<RuleBuilder>)` applies a convention, at build time ... `RuleBuilder.Any(params RuleBuilder[])` rejects fewer than two operands like the rule languages.' `RuleBuilder.Any(params RuleBuilder[])` (RuleBuilder.cs:353-363), `All` (387-397) and `None` (421-431) fold exactly as the sequence overload does: empty becomes a constant, one operand becomes itself (or `NOT` for `NONE`), and the XML remarks say this is deliberate so array and list give the same rule. The documentation is the side that is wrong (the Syntax table on the same pages, 'for two or more operands', is also misleading).

### Failure scenario

A library consumer reads any.md, calls `RuleBuilder.Any(single)` expecting a compile diagnostic (`InfixArityViolation`) that would catch an accidental one-element array, and instead gets a rule that silently compiles to `single`; `RuleBuilder.Any()` with no operands compiles to the constant `False` instead of failing.

### Expected behavior

The reference pages describe the package as it behaves: both `Any`/`All`/`None` overloads fold an empty or single-operand list (identity constant, the operand, or `NOT` operand), and only the rule languages reject fewer than two operands.

### Verification

not verified (medium or low)

## Finding L2.3: GreaterThan and LessThan pages claim there is no IEnumerable RuleBuilder overload; both overloads exist

Location: `docs/strong-k3/cardinality/greaterthan.md:1643` (PLAUSIBLE, low)

greaterthan.md (Syntax table and the sentence after it, 'RuleBuilder has only the params overload for GreaterThan; there is no IEnumerable overload') and lessthan.md make the same claim for `LessThan`. `RuleBuilder.GreaterThan(int k, IEnumerable<RuleBuilder>)` (RuleBuilder.cs:642) and `RuleBuilder.LessThan(int k, IEnumerable<RuleBuilder>)` (RuleBuilder.cs:663) exist and are tested (`RuleBuilderOverloadConsistencyTests.GreaterThanAndLessThan_WithAList_BuildTheSameRuleAsTheArray_Test`). ADR-0005 log row 43 only lists `Between`, `AtLeast`, `AtMost`, `Exactly`, so the doc text was written from the ADR rather than the code. The documentation is the wrong side.

### Failure scenario

A consumer with a runtime-length list writes `RuleBuilder.GreaterThan(1, list.ToArray())` or avoids the operator because the page says the overload does not exist; the public API surface is misdescribed.

### Expected behavior

The pages list `RuleBuilder.GreaterThan(int k, IEnumerable<RuleBuilder>)` and `RuleBuilder.LessThan(int k, IEnumerable<RuleBuilder>)` with the same 'not folded' note the AtLeast/AtMost/Exactly pages carry.

### Verification

not verified (medium or low)

## Finding L3.4: coalesce.md says RuleBuilder.Coalesce(IEnumerable) folds an empty or single list; the code, rulebuilder.md and the pinned test say it does not

Location: `docs/strong-k3/functions/coalesce.md:29` (PLAUSIBLE, medium)

The reference page states 'an empty list builds the constant Unknown and a single operand builds that operand unchanged', but RuleBuilder.Coalesce(IEnumerable) (RuleBuilder.cs:487-498) builds the unfolded node and its remark says 'A short or empty sequence is not folded, because COALESCE has no identity constant'; docs/rulebuilder.md:54 and RuleBuilderEnumerableTests pin the no-fold behavior. The two reference pages contradict each other, and the code's stated reason is false: coalesce.md:65 and :145-155 correctly say Unknown is COALESCE's identity on both sides, so folding would be K3-sound.

### Failure scenario

A caller who follows coalesce.md writes `RuleBuilder.Coalesce(checks)` with an empty `checks` list expecting the constant Unknown; the builder instead renders `{"op":"coalesce","operands":[]}` and Compile returns InfixArityViolation (TRE0006), so the rule fails to compile. The strong-k3 page is the side that disagrees with the pinned behavior; whichever side the owner keeps, the other page and the code remark must change.

### Expected behavior

coalesce.md, rulebuilder.md and RuleBuilder.Coalesce(IEnumerable) describe one behavior; if no-fold is kept, the reason given should not claim COALESCE lacks an identity.

### Verification

not verified (medium or low)

## Acceptance criteria

- [x] The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- [x] The page that describes the behavior states the current truth (required when behavior or public API changes).
- [x] The validation list in CLAUDE.md passes.

## Comments

> *This was generated by AI during triage.*

Findings L1.1, L1.2, L2.2 and L2.3: confirmed docs-only fixes — the code is correct and already pinned by tests (`RuleBuilderOverloadConsistencyTests`, `RuleBuilderEnumerableTests`); `and.md`, `or.md`, `parity.md`, `any.md`, `all.md`, `none.md`, `greaterthan.md` and `lessthan.md` are the stale side and get corrected to match.

Finding L3.4 (coalesce.md): reversed from the ticket's framing. `coalesce.md`'s own **Formal semantics** section proves `Unknown` is COALESCE's identity element on both sides — the same identity-element reasoning the codebase already uses to fold AND→`True`, OR→`False`, ANY→`False`, ALL→`True` and NONE→`True` on empty/single-operand input. `coalesce.md`'s "Arity" section already states the IEnumerable overload should fold accordingly. The code's own remark ("COALESCE has no identity constant"), `docs/rulebuilder.md`'s grouping of `Coalesce` with `Parity`/`ExactlyOne` as having no identity, and the pinned test asserting no-fold are the side that's wrong. Fix the **code**, not `coalesce.md`.

## Agent Brief

**Category:** bug
**Summary:** Two kinds of drift between `RuleBuilder`'s fold/reject behavior for short operand lists and the K3 reference docs. (1) `and.md`, `or.md`, `parity.md`, `any.md`, `all.md`, `none.md`, `greaterthan.md` and `lessthan.md` misdescribe already-correct, already-tested code — fix the docs. (2) `RuleBuilder.Coalesce(IEnumerable<RuleBuilder>)` does not fold an empty or single-operand sequence, even though `coalesce.md`'s own formal semantics prove `Unknown` is COALESCE's two-sided identity — fix the code.

**Current behavior:**
- `and.md`/`or.md` claim the `params` overload of `And`/`Or` rejects fewer than two operands while only the `IEnumerable` overload folds; both overloads actually fold identically.
- `parity.md` claims `Parity(IEnumerable)` folds an empty/single sequence; neither `Parity` overload folds (correctly — Parity has no identity).
- `any.md`/`all.md`/`none.md` claim only the `IEnumerable` overload of `Any`/`All`/`None` folds; both overloads fold identically.
- `greaterthan.md`/`lessthan.md` claim `RuleBuilder` has no `IEnumerable` overload for `GreaterThan`/`LessThan`; both exist and are tested.
- `coalesce.md` claims `RuleBuilder.Coalesce(IEnumerable)` folds an empty list to `Unknown` and a single operand to itself; the code builds the unfolded node instead, which the compiler then rejects as `InfixArityViolation` for fewer than two operands.

**Desired behavior:**
- The eight listed K3 reference pages are corrected to describe the already-correct, already-tested `RuleBuilder` fold/no-fold behavior (code and tests are not changed for these).
- `RuleBuilder.Coalesce(IEnumerable<RuleBuilder>)` folds an empty sequence to `Constant(Unknown)` and a single-operand sequence to that operand unchanged, matching `coalesce.md`'s Arity and Formal-semantics sections and matching how `And`/`Or`/`Any`/`All`/`None` already fold their identity. `docs/rulebuilder.md`'s "operators with no identity" grouping and the code's own remark are corrected to no longer list `Coalesce` there. The existing pinned test asserting Coalesce does not fold is updated to assert that it does.

**Key interfaces:**
- `RuleBuilder.Coalesce(IEnumerable<RuleBuilder>)`: add the fold, mirroring the existing `And`/`Or`/`Any`/`All`/`None` `IEnumerable` overloads' empty/single-operand handling, using `Unknown` as the identity.
- `RuleBuilder.Coalesce(params RuleBuilder[])`: leave unfolded (unaffected — the `params` overload for `Coalesce` was never claimed to fold by any doc, and this ticket doesn't ask for that to change).

**Acceptance criteria:**
- [x] `and.md`, `or.md`, `parity.md`, `any.md`, `all.md`, `none.md`, `greaterthan.md`, `lessthan.md` are corrected to match current, already-tested `RuleBuilder` behavior.
- [x] `RuleBuilder.Coalesce(IEnumerable<RuleBuilder>)` with an empty sequence builds `Constant(Unknown)`; with one operand, builds that operand unchanged.
- [x] `docs/rulebuilder.md`'s "no identity" grouping and the code's own remark no longer list `Coalesce`.
- [x] The existing pinned enumerable-overload test for `Coalesce` is updated to assert the new fold behavior instead of no-fold.
- [x] The `Coalesce(params RuleBuilder[])` overload's behavior is unchanged.
- [x] The validation list in `CLAUDE.md` passes.

**Out of scope:**
- `Parity` and `ExactlyOne`, which genuinely have no identity and correctly remain unfolded.
- The counted/threshold operators (`Between`, `AtLeast`, `AtMost`, `GreaterThan`, `LessThan`, `Exactly`), which also correctly remain unfolded.

## Resolution (2026-11-01)

Docs side: `and.md`, `or.md`, `any.md`, `all.md`, `none.md` now say both the `params` and the `IEnumerable` overload fold; `parity.md` says neither overload folds; `greaterthan.md` and `lessthan.md` list the `IEnumerable<RuleBuilder>` overload. Code side (per the ticket's Decision): `RuleBuilder.Coalesce(IEnumerable<RuleBuilder>)` folds an empty sequence to `Constant(Unknown)` and one operand to itself; `Coalesce(params RuleBuilder[])` is unchanged (`coalesce.md` already described this). Tests: `RuleBuilderEnumerableTests` (Coalesce moved from the no-identity case to the fold cases; red first), `RuleBuilderOverloadConsistencyTests` (new `Compile_ShortListForCoalesce_FoldsFromAListButNotFromAnArray_Test`; Coalesce 0/1 removed from the array-equals-list and short-list theories). Pages updated: the K3 pages above, `docs/rulebuilder.md`, `CHANGELOG.md`.

Note: Coalesce is now the one operator where an array and a list of the same operands can give different rules; `docs/rulebuilder.md` states the exception.
