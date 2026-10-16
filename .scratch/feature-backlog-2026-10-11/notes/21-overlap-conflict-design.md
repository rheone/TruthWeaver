# 21: Overlap and conflict analysis between rules: design note

**Ticket:** [21 spike](../issues/21-spike-overlap-and-conflict-analysis.md). **Builds on:** [13](../issues/13-compare-rules-looser-or-tighter.md) (done, commit `cb6926e`). **Touches later:** [07](../issues/07-policy-sets-and-combining-algorithms.md) (on hold).
**Status:** spike output, 2026-10-11. This note decides nothing. The owner schedules a grilling on it; the open questions at the end are written for that session.
**Read with:** [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md) (decisions 17 and 18), `CONTEXT.md` (Connective, External operator, Term identity), [Looser or tighter](../../../docs/rewriting-rules.md#looser-or-tighter).

## Recommendation

**Go, with a narrow first slice: a pairwise joint-outcome check.** Add `RuleOverlap.Compare(first, second)`. It builds both rules on one shared dual-rail BDD, as `RuleImplication.Compare` does, and reports which of the nine pairs of values `(first, second)` some assignment of the terms can produce, with one example assignment for each reachable pair. Every pairwise question in the ticket is a view of that table:

| Question | Pairs that answer it |
| --- | --- |
| Do A and B ever both hold (overlap)? | `(True, True)` |
| Do A and B ever give opposite definite answers (conflict)? | `(True, False)` or `(False, True)` |
| Is A ever satisfied when B is not (ticket 13's `SatisfiesOnlyBefore`)? | `(True, False)` or `(True, Unknown)` |
| Which contexts are decided differently? | Every pair off the diagonal |
| Is A shadowed by B (A never holds unless B holds)? | `(True, False)` and `(True, Unknown)` both unreachable |

Leave the set-level questions (redundancy in a rule set, shadowing in an ordered list) for a second slice. They depend on how a set combines its rules, which TruthWeaver does not define yet (ticket 07 is on hold). They also cost much more (section 5).

Why it is worth doing: ticket 13 answers "looser or tighter" for one rule against its new version. Authors of a group of rules (a permission per action, a feature flag per audience) also ask "can these two ever both apply" and "can these two ever disagree". The engine already has the exact machinery. The first slice is about 1 internal method, 2 public records and a docs section.

Why not more now: the engine has no "rule set" concept (`CONTEXT.md`: a name or version around a rule belongs to the application), no combining semantics, and no BDD size budget. The set-level analysis needs all three decided first.

## 1. Definitions

All definitions range over **assignments**: a map from each distinct term of either rule to `True`, `False` or `Unknown`. This is the same space as `RuleEquivalence` and `RuleImplication`. "A holds" means A is `True`, the same reading as `Decision.IsSatisfied` and ticket 13.

| Term (proposed) | Definition over assignments `σ` |
| --- | --- |
| **Joint outcome** | The pair `(A(σ), B(σ))`. A pair is *reachable* when some `σ` produces it. |
| **Overlap** | `(True, True)` is reachable. Ticket 13's `Shared` flag. |
| **Conflict** | `(True, False)` or `(False, True)` is reachable: one rule says yes and the other says no, both definitely. |
| **Disagreement** | Some pair off the diagonal is reachable. Equal to "not equivalent" in `RuleEquivalence`. |
| **Covered (A by B)** | Every `σ` where A is `True` makes B `True`. Equal to `RuleImplication.Implies(A, B)` = `Holds`. |
| **Shadowed** (set level) | In an ordered list, rule *j* is covered by the `OR` of the rules before it, so a "first match wins" reading never reaches it. Section 4. |
| **Redundant** (set level) | A rule that can be removed from a set without changing the set's combined answer. Section 4. |

The rail formulas, on the existing `DualRail(Definite, Possible)`:

- is `True` = `Definite`
- is `False` = `NOT Possible`
- is `Unknown` = `Possible AND NOT Definite`

Cell `(x, y)` is reachable when `is_x(A) AND is_y(B)` is not the constant-false BDD. That is 9 `And` operations after the two builds. The witness comes from the existing `Witness` reader.

## 2. `Unknown` and the meaning of "conflict"

Three readings were considered:

| Reading | Conflict when | Comment |
| --- | --- | --- |
| **A. Strict (recommended)** | `(True, False)` or `(False, True)` | Both rules give a definite and opposite answer for the same context. |
| B. Satisfaction | One rule is `True`, the other is not `True` | Fail-closed view. Already answered by ticket 13 (`SatisfiesOnlyBefore`/`After`). |
| C. Any difference | Any off-diagonal pair | Already answered by `RuleEquivalence`. Every rule with a term can be `Unknown` (a fault makes any term `Unknown`), so `(True, Unknown)` is reachable for almost every pair and the word "conflict" would lose its meaning. |

Reading A has a property that makes it robust, checked by the prototype (section 9):

- **For rules built only from connectives, a conflict or an overlap exists over K3 assignments if and only if it exists over two-valued assignments.** Connectives are monotone in the information order (ADR-0005 decision 18): if `σ` gives `(True, False)`, every refinement of `σ`'s `Unknown` terms to `True` or `False` keeps `(True, False)`. So the answer does not depend on whether terms can be `Unknown` (or fault).
- The same argument gives a better example: an example for a definite pair can always be refined to an assignment with no `Unknown` term. The analyzer should prefer such an example (restrict every "is Unknown" BDD variable to false first, and fall back only when that is empty). A reader trusts "isManager = True, isContractor = True" more than one with `Unknown` in it.
- **External operators break both properties.** `IsUnknown(t0)` and `t1` can both be `True` only when `t0` is `Unknown`. The result is still exact over K3 assignments, but the example then needs an `Unknown` term, which in practice means "the predicate returned `Unknown` or faulted". The docs must say so.

The `Unknown` pairs are still reported in the table (reading C is free to compute), but the convenience members and the docs call only reading A a conflict.

## 3. What the analysis can and cannot claim

| Claim | Soundness |
| --- | --- |
| A pair is **unreachable** ("A and B never conflict", "A is covered by B") | **A proof** for every real context, provided the predicate-author contract holds (same term, same answer within one evaluation). Every context produces some assignment, so no context can do what no assignment does. |
| A pair is **reachable**, with an example | **A candidate only.** Terms are opaque and independent (as documented for `RuleEquivalence`), so the example can be impossible in the application: `ageAtLeast(n: 21) = True` with `ageAtLeast(n: 18) = False`. |

Consequences:

- Overlap reports are noisy for rules over shared terms. In the prototype, 763 of 780 random rule pairs over 20 terms overlap. The useful findings are the exceptions: pairs that **never** overlap (disjoint), pairs that **never** conflict, and rules that are covered. Reports should lead with those, and present reachable pairs as "possible, for example: ...".
- A later option is **assumptions**: an extra rule that the caller asserts is `True` in every real context (`ageAtLeast(n: 21) IMPLIES ageAtLeast(n: 18)`). The analysis ANDs its definitely-true rail into every cell. It removes false candidates and keeps the proofs sound only if the assumption is true. Not in slice 1 (Q6).
- Variable references (`from("source", "query")`) compare by text (Term identity), so two queries that read the same value are two independent terms. Same incompleteness, same direction.
- Quantifiers (ticket 22, if built) are opaque atoms. Unreachable claims stay sound.
- Value only: faults, evaluation order and short-circuiting are not part of the result, as for `RuleEquivalence`.

## 4. Set-level questions (second slice)

These need a combining reading, so the API must take it as an input. Proposed readings, each a single implication check on one shared BDD:

### Redundancy

| Set reading | Rule *i* is redundant when | Implementation |
| --- | --- | --- |
| **Any grants** (the set holds when any rule holds) | `True(Ri)` is inside the union of `True(Rj)`, `j ≠ i` | `Implies(Ri, OR(others))` |
| **All required** (the set holds when every rule holds) | The intersection of `True(Rj)`, `j ≠ i`, is inside `True(Ri)` | `Implies(AND(others), Ri)` |

Pitfall, shown by the prototype: in `[t0, t0 AND t1, t2, t2]` the last three rules are each redundant, but the two `t2` rules cannot both be removed. "Redundant" is per rule, given all the others. A useful report gives a set that can be removed **together**: walk the list from the end and drop a rule when the rules still kept cover it (Q5).

Redundancy **inside one rule** (an operand that adds nothing) is out of scope. `Simplify` and the lints `TRE0012`, `TRE0013`, `TRE0017` to `TRE0022` already cover the common cases.

### Shadowing

For an ordered list read as "the first rule that applies wins", rule *j* is shadowed when the rules before it already cover it. Under K3 this has two readings, because an earlier rule can be `Unknown`:

| Reading | Rule *j* is shadowed when, wherever *j* is `True` | Example that differs |
| --- | --- | --- |
| **Blocked by `True`** | some earlier rule is `True` | Earlier `t0 OR NOT t0`, then `t1`: not shadowed (when `t0` is `Unknown`, the earlier rule is `Unknown` and `t1` can be reached). |
| Blocked by `True` or `Unknown` | some earlier rule is not `False` | Same pair: shadowed, because the earlier rule is never `False`. |

Which reading is right depends on what the combining algorithm does with an `Unknown` rule (XACML's "Indeterminate" stops a first-applicable set, for example). That is ticket 07's decision. Recommendation: do not ship shadowing before 07 settles it; if it ships earlier, take the reading as a parameter (Q4).

### Pairwise sweep

`RuleOverlap` over every pair of a list is n(n-1)/2 checks. With one shared BDD for the whole list each rule is built once, and each pair costs only the 9 cell operations. That needs the union of all terms under the term cap (section 5).

## 5. Cost and limits

The only analysis guard today is `MaxAnalysisTerms` (default 20), counted over the distinct terms of the rules being compared. `BddManager` has no node budget.

Prototype timings (section 9: public API only, each check recompiles a composite rule and builds a fresh BDD, so these are upper bounds; the random rules mix in `XOR`, `EQUIVALENT`, `If` and `AtLeast`, which are harder for a BDD than the `AND`/`OR` rules most hosts write):

| Rules | Distinct terms | Rule depth | Pairwise `Compare` | Per-rule redundancy (OR of the others) |
| --- | --- | --- | --- | --- |
| 20 | 10 | 4 | 190 pairs, 0.1 s | 0.3 s |
| 40 | 20 | 4 | 780 pairs, 0.4 s | 6.6 s |
| 40 | 20 | 5 | 780 pairs, 1.9 s | 33 s |
| 40 | 30 | 5 | 780 pairs, 8.7 s | over 120 s (stopped) |
| 80 | 40 | 4 | 3,160 pairs, 1.3 s | over 120 s (stopped) |

Findings:

- **Pairwise checks are cheap.** Each BDD holds only two rules. The 20-term cap per pair is rarely the limit.
- **Set-level checks blow up.** The `OR` of many rules is one large BDD, and its size depends on the rules, not only on the term count. A set of 40 rules usually has more than 20 distinct terms, so the default cap makes the set-level answer `Undecided` for most real sets, and a raised cap gives no time bound.
- Shared-manager prefix and suffix `OR`s (build each rule once, compute `OR(R1..Rk)` incrementally) remove the rebuild cost of the naive prototype but not the size of the union BDD.

Proposed limits:

| Limit | Slice | Proposal |
| --- | --- | --- |
| Term cap, pairwise | 1 | `maxAnalysisTerms` argument, default `CompilerOptions.Default.MaxAnalysisTerms`, counted over the pair. Same as ticket 13. Over the cap: `Undecided` with a reason. |
| Term cap, set | 2 | Counted over the union of the set. Over the cap: the set-level answers are `Undecided`; the pairwise answers still run pair by pair (Q3). |
| BDD node budget | 2 (or earlier) | An internal node limit on `BddManager`. Over it: abort the build and return `Undecided` with a reason. This is the only guard that bounds time. It would also protect ticket 13 and `RuleEquivalence` (Q7). |

## 6. API sketch

### Slice 1: pairwise

```csharp
namespace TruthWeaver.Analysis;

/// Decides which pairs of values two rules can produce for the same assignment of their terms.
public static class RuleOverlap
{
    public static RuleOverlapResult Compare<TContext>(
        CompiledRule<TContext> first,
        CompiledRule<TContext> second,
        int? maxAnalysisTerms = null);
}

public sealed record RuleOverlapResult(
    RuleOverlapOutcome Outcome,                 // Decided, Undecided
    IReadOnlyList<JointOutcome> Reachable,      // empty when Undecided
    string? Reason)
{
    /// The example for one pair, or null when the pair is unreachable (or the result is undecided).
    public JointOutcome? Find(TruthValue first, TruthValue second);

    public bool CanOverlap { get; }             // (True, True) reachable
    public bool CanConflict { get; }            // (True, False) or (False, True) reachable
}

/// One reachable pair and an assignment that produces it, keyed by the printed term.
public sealed record JointOutcome(
    TruthValue First,
    TruthValue Second,
    IReadOnlyDictionary<string, TruthValue> Example);
```

- Internal: `Analyzer.JointOutcomes(Expression left, Expression right)` returns the nine cells and witnesses. `CompareSatisfaction` (ticket 13) is three of these cells; it can stay as it is or become a view of the new method (implementation detail, no API change).
- Witnesses prefer assignments with no `Unknown` term (section 2).
- `CanOverlap` and `CanConflict` are `false` for `Undecided`, so a caller must check `Outcome` first. The alternative is `bool?` (Q2).
- No throwing member, so no `Try…` form. `ArgumentNullException` for a `null` rule, as in ticket 13.
- `RuleDiffResult` does not change: a diff compares versions of one rule, and ticket 13's relation already answers that.
- No new package and no new dependency. Everything is in `TruthWeaver.Analysis`.

### Slice 2: sets (shape only)

```csharp
public static class RuleSetAnalysis
{
    public static RuleSetReport Analyze<TContext>(
        IReadOnlyList<CompiledRule<TContext>> rules,
        RuleSetAnalysisOptions? options = null);   // reading (AnyGrants, AllRequired, FirstMatch), term cap, node budget
}

public sealed record RuleSetReport(
    IReadOnlyList<RulePairFinding> Pairs,          // index pairs: Disjoint, Covered, Conflict (with example)
    IReadOnlyList<RuleFinding> Rules,              // index: Redundant (covering indexes) or Shadowed (shadowing indexes)
    IReadOnlyList<int> RemovableTogether,
    string? Reason);
```

Rules are identified by their index in the input list, because TruthWeaver has no rule names (Q8).

### Other surfaces

- The CLI (ticket 16) can expose `truthweaver overlap a.rule b.rule` over slice 1 with no further library work.
- Declarative rule tests (ticket 20) could add `never-conflicts-with` and `never-overlaps-with` expectations later.

## 7. Risks

- **Candidate examples read like facts.** Opaque terms produce impossible examples (section 3). Mitigation: docs and member comments state that "unreachable" is a proof and "reachable" is a candidate; examples prefer definite term values.
- **"Overlap" is almost always true.** A report that lists overlaps first is noise. Mitigation: lead with `CanConflict`, disjoint and covered pairs.
- **The word "conflict" carries authorization meaning** (permit versus deny). TruthWeaver rules have no effect (`CONTEXT.md`: not a policy engine). Here a conflict is only "opposite definite values". The docs must define it that way, and ticket 07 must not redefine it.
- **External operators** make examples depend on `Unknown` terms (section 2). Correct, but surprising.
- **Set-level cost has no time bound** without a node budget (section 5). This is why sets wait for slice 2.
- **Two near-duplicate APIs.** `RuleImplication.Compare` and `RuleOverlap.Compare` both compare two rules. Mitigation: one docs section, "Comparing two rules", with a table of which question each answers. `RuleImplication` reads one rule as a new version of the other; `RuleOverlap` reads them as peers.

## 8. Minimal first slice (proposed ticket 21b)

1. Internal `Analyzer.JointOutcomes(left, right)`: both rules on one shared BDD, nine cells from the rail formulas in section 1, witnesses that prefer definite term values.
2. Public `RuleOverlap.Compare`, `RuleOverlapResult`, `RuleOverlapOutcome`, `JointOutcome`, with the term cap and `Undecided` as in ticket 13.
3. Property test against `K3Oracle`: for generated rule pairs (connectives and external operators), the reachable set equals the brute-force set over every K3 assignment, and every example produces its pair.
4. A test that, for connective-only rules, every definite-pair example has no `Unknown` term.
5. Docs: a new "Overlap and conflict" section next to "Looser or tighter" in `docs/rewriting-rules.md` (definitions, the proof-versus-candidate rule, limits), the glossary entries for **joint outcome**, **overlap** and **conflict**, `CONTEXT.md` vocabulary if the owner accepts the terms, `CHANGELOG.md`.

Not in slice 1: rule sets, redundancy, shadowing, assumptions, a BDD node budget, CLI and test-runner surfaces.

## 9. Prototype

A throwaway prototype was written in the scratchpad, not in the repository (`scratchpad/overlap/Program.cs`, a console app that references a Release build of `TruthWeaver` exported from commit `cb6926e` into the scratchpad). It uses only the public API: cell `(x, y)` is reachable when `RuleImplication.Implies(Is_x(A) AND Is_y(B), False)` does not hold, and the counter-example is the witness. This also shows slice 1 needs no new BDD capability. Results:

- **Exact:** for 150 random rule pairs over 4 terms (one third with `COALESCE` and inspections), the analysed 9-cell table equals the brute-force table over all 81 K3 assignments, with 0 mismatches. Every one of the nine cells was hit more than 100 times.
- **Monotonicity:** for connective-only pairs, the four definite cells are reachable over K3 if and only if they are reachable over two-valued assignments (0 mismatches). 85 `(True, False)` examples, refined by setting each `Unknown` term to `False`, all still gave `(True, False)`.
- **External operators:** `IsUnknown(t0)` and `t1` can both be `True` over K3, which no two-valued assignment allows.
- **Shadowing:** in `[t0 AND t1, t0, t0 AND t2, t3 OR t1, t1 AND t3]`, rules 3 (`t0 AND t2`) and 5 (`t1 AND t3`) are covered by the rules before them, and the brute force agrees.
- **Redundancy:** in `[t0, t0 AND t1, t2, t2]`, rules 2, 3 and 4 are each redundant, and the two `t2` rules cannot both go.
- **Cost:** the table in section 5.

## Open questions for the owner

Each is written for the grilling: one at a time, recommended option first.

**Q1. Go or no-go, and which slice first?**
1. Go: pairwise `RuleOverlap.Compare` first (section 8). Sets after ticket 07 settles combining semantics. *(recommended)*
2. Go: pairwise and set-level analysis together, with the set reading as a parameter.
3. No-go: ticket 13's `RuleImplication.Compare` is enough.

**Q2. What does "conflict" mean?**
1. Strict: one rule `True` and the other `False` for the same assignment. `Unknown` pairs are reported but are not conflicts. *(recommended)*
2. Satisfaction: one rule `True`, the other `False` or `Unknown` (ticket 13's reading).
3. Any difference in value.

**Q3. Result shape of the pairwise check.**
1. The reachable pairs with an example each, plus `CanOverlap`/`CanConflict` (`false` when undecided, check `Outcome` first). *(recommended)*
2. The same, with `bool?` members that are `null` when undecided.
3. A single relation enum (like `RuleRelation`) with conflict variants, no table.

**Q4. Shadowing reading for an ordered list (slice 2).**
1. Wait for ticket 07, which decides what an `Unknown` rule does in first-applicable combining. *(recommended)*
2. Blocked by an earlier `True` only.
3. Blocked by an earlier `True` or `Unknown`.
4. Both, as a parameter.

**Q5. Redundancy report (slice 2).**
1. Per rule, plus one set of rules that can be removed together (greedy, from the end of the list). *(recommended)*
2. Per rule only, "redundant given all the others".
3. A minimum removable set (exact, more expensive).

**Q6. Assumptions (caller-supplied facts between terms, such as `ageAtLeast(21) IMPLIES ageAtLeast(18)`).**
1. Later, as an optional rule argument on the comparison APIs, after slice 1 ships. *(recommended)*
2. In slice 1.
3. Never: document the opaque-term limit only.

**Q7. BDD node budget.**
1. Add an internal node budget in slice 2, returning `Undecided` with a reason; apply it to ticket 13 and `RuleEquivalence` too. *(recommended)*
2. Add it in slice 1.
3. No budget; the term cap stays the only guard.

**Q8. How rules in a set are identified (slice 2).**
1. By index in the input list; the host maps indexes to its own names. *(recommended)*
2. By a caller-supplied key (`IReadOnlyDictionary<string, CompiledRule<TContext>>`).

**Q9. Where to record the decision.**
1. A short ADR for the definitions (joint outcome, overlap, conflict, proof versus candidate), because ticket 07 builds on them. *(recommended)*
2. Docs and `CONTEXT.md` only, no ADR.

## Acceptance checklist (ticket 21)

- [x] Design note at `.scratch/feature-backlog-2026-10-11/notes/21-overlap-conflict-design.md` covering semantics (sections 1 to 4), API shape (section 6), cost (section 5), risks (section 7) and a recommended go (top, section 8).
- [x] Prototype code lives in the scratchpad only. No library code changed, nothing committed.
- [ ] A grilling session on the note is scheduled by the owner (this ticket does not decide).

Ticket coverage: "do A and B ever both hold" (sections 1, 6); "is one always shadowed" (sections 1, 4); "which contexts are decided differently" (sections 1, 2); "redundancy inside a rule set" (section 4); "same dual-rail BDD as ticket 13" (sections 1, 6, 9); cost limits (section 5); `Unknown` semantics for conflict (section 2); API sketches (section 6); recommendation (top).
