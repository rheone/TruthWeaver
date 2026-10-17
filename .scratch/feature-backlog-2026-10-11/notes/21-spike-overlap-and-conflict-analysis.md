> **Superseded (owner, 2026-10-16):** a duplicate spike. The authoritative note is [21-overlap-conflict-design.md](21-overlap-conflict-design.md). The decisions at the end of this file are carried into it where they fit.

# 21: Spike — overlap and conflict analysis between rules

**Status:** spike design note. Produces no library change. A grilling session on
this note is the next step.

## Scope recap

Explore what "overlap" and "conflict" mean for a **set** of rules (not just the
pair ticket 13 already compares) and what the analyzer can answer from it: do
rules A and B ever both hold, is one always shadowed by another, which
contexts are decided differently by two rules, and is a rule inside a set made
redundant by the others. Cover cost limits, `Unknown` semantics for
"conflict", API sketches and a go/no-go recommendation. This spike shares the
BDD/term-identity model with `.scratch/feature-backlog-2026-10-11/notes/22-spike-quantifiers.md`
(its §4); cross-references are called out below.

## 1. What already exists (ticket 13) and what is new here

Ticket 13 shipped the pairwise building blocks this spike reuses, all in
`src/TruthWeaver/Analysis/`:

- `RuleEquivalence.Compare` (`RuleEquivalence.cs`) — are two rules the *same*
  Strong K3 function (same value for every assignment, including where each is
  `False` or `Unknown`)? Built on `Analyzer.FindDifference`.
- `RuleImplication.Implies` — does every assignment that satisfies rule A also
  satisfy rule B? Built on `Analyzer.CompareSatisfaction(...).OnlyLeft`.
- `RuleImplication.Compare` — the five-way `RuleRelation`
  (`Equivalent`/`Tighter`/`Looser`/`Overlapping`/`Disjoint`/`Undecided`,
  `RuleRelation.cs`) between a "before" and an "after" rule, defined purely on
  **where each rule is `True`** (`Decision.IsSatisfied`'s reading), via
  `Analyzer.CompareSatisfaction` (`Analyzer.cs:89-106`), which builds both
  rules' `Definite` rail on one shared `BddManager` so identical `TermIdentity`
  values share variables, then intersects/subtracts the two rails and reports
  a concrete counter-example (`Witness`, `Analyzer.cs:296-323`) for each side,
  keyed by the term's printed form.

All of this is exact (BDD-based, not sampled) and bounded by
`CompilerOptions.MaxAnalysisTerms` (default 20; `CapReason`,
`RuleImplication.cs:104-117`), counted across **both** rules' distinct terms.

What ticket 13 does not have, and this spike is about:

1. **A set-level API.** `RuleRelation` and `RuleEquivalence` take exactly two
   rules. "Do any two rules in this set of N overlap", "is rule 3 shadowed by
   the union of the others", and "which pairs disagree" are set questions a
   caller today has to hand-loop with `O(N^2)` calls themselves.
2. **A different notion of "conflict" than `RuleRelation.Overlapping`.**
   `Overlapping` means "some assignment satisfies both" — both rules are
   `True` together, which for independent rules evaluated separately is not
   itself a problem (two rules can both legitimately be `True`). The ticket's
   "which contexts are decided differently" is closer to `RuleEquivalence`'s
   `FindDifference`, generalized: a *conflict* is about two rules **disagreeing
   on the same decision question**, which only makes sense once a caller also
   says what "disagreement" means for their domain (opposite `True`/`False`?
   any difference including `Unknown`? see §2).
3. **Redundancy inside one set**, which is a new question even for N=2: is a
   rule entailed by the *union* (`OR`) of some subset of the others, so
   dropping it changes nothing? `RuleImplication.Implies` answers this for one
   candidate against one other rule or against a hand-built `OR` of others,
   but nothing today frames "which rules in this set are redundant" as a set
   query.

## 2. What "overlap" and "conflict" mean here

Both terms need a definition precise enough to implement, because the
existing building blocks support more than one plausible reading.

### Overlap

**Recommendation: reuse `RuleRelation.Overlapping`'s definition as-is — "some
assignment of the shared terms makes both rules `True`."** This is already
exact, already implemented (`Analyzer.CompareSatisfaction`), and is the
natural generalization to a pair drawn from a set: "do rules A and B ever both
hold" is literally the ticket's own phrasing, and `RuleRelation.Compare(A, B)`
already answers it (`Overlapping` or `Equivalent` both mean "yes, and more").
No new semantics needed for this one; the new work is making it a set-pair
question (§3) rather than inventing a new overlap definition.

### Conflict

This is the one genuinely new piece, because "two rules both hold" is not
itself a conflict — it only becomes one in a context the analyzer does not
see: a host combining several rules' `Decision`s into one outcome (allow/deny,
score, etc.), which is exactly the scope ticket 7 (policy sets and combining
algorithms, on hold, needs 18) owns. Three readings, in increasing strictness:

1. **Value disagreement (reuse `RuleEquivalence`).** Rules A and B *conflict*
   at an assignment when they take different Strong K3 values there —
   `Analyzer.FindDifference(A, B)` already finds one such assignment, or
   `null` if there is none (the rules are equivalent). This is "which contexts
   are decided differently," read literally: it includes `True` vs. `False`,
   `True` vs. `Unknown`, and `False` vs. `Unknown` as all being "different."
2. **Opposite-decision conflict.** Rules A and B conflict at an assignment
   only when one is `True` and the other is `False` — the case where, read as
   independent allow/deny gates, they actively disagree, as opposed to one
   being merely `Unknown` while the other has an opinion. This needs a new BDD
   term, not a reuse of `FindDifference`: `(A.Definite AND B.Not-Possible) OR
   (B.Definite AND A.Not-Possible)` on the same shared manager
   `CompareSatisfaction` already builds — cheap to add (one more Boolean
   combination of rails already in hand), not a new algebra.
3. **Satisfied-together-but-would-decide-differently.** A narrower case of (2)
   restricted to where `RuleRelation.Overlapping`'s witness lies: at the one
   assignment where both are `True`, do they *also* disagree on some other
   question (e.g. two rules each `True` but attached to incompatible actions)?
   This needs information the analyzer does not have — the action each rule's
   `True` is supposed to cause — so it is out of scope for the BDD/term layer
   and belongs with ticket 7's combining algorithm, not this spike's analyzer
   surface.

**Recommendation: implement reading (1) as `RuleEquivalence.Compare`
already does (ship nothing new, just expose the counter-example as "where
these two rules disagree"), and reading (2) as a new, small
`RuleConflictResult`/`RuleConflict.Compare` type analogous to `RuleRelation`
with values `NoConflict`/`Conflicts`/`Undecided` plus a `True`-vs-`False`
witness.** This keeps "conflict" precise (opposite firm answers) rather than
flagging every `Unknown`-involving mismatch as a problem, which would be noisy
for any rule set that uses `Unknown` deliberately (ADR-0001/0002's fail-as-data
model treats `Unknown` as a legitimate, expected value, not a defect). Reading
(3) is explicitly **not** this spike's job — flag it as ticket 7's concern.

### `Unknown` and "conflict"

The central semantic question the ticket calls out by name. Three sub-points,
all following from Strong K3 and the existing rails:

- **`Unknown` vs. `Unknown` is never a conflict** under either reading above:
  two rules that are both `Unknown` for the same reason or different reasons
  are not disagreeing about anything decidable, and `FindDifference`
  correctly does not flag `Unknown`-vs-`Unknown` as a difference (it is not a
  difference at all — both rails of both rules match).
- **`Unknown` vs. `True`/`False` is a *difference* (reading 1) but not a
  *conflict* (reading 2).** This is deliberate: a rule that cannot decide
  (`Unknown`, e.g. a fault or missing data) is not asserting the opposite of a
  rule that can; treating it as a conflict would surface every partially-
  evaluable context as a false alarm.
- **Fail-closed callers care about possibility, not just the definite rail.**
  A host using `Decision.IsSatisfied` (fail-closed: only `True` satisfies) may
  want "could these ever decide oppositely" to include "A is `True`, B is
  `Unknown`-but-possibly-`False`" as a softer warning tier, since under
  fail-closed reading B's `Unknown` *behaves like* `False` for gating
  purposes even though it is not structurally `False`. This is a real but
  separate question from (2)'s exact BDD-decidable one — flagged as open
  decision 3 below rather than folded into the "conflict" verdict, so the
  verdict stays an exact, non-opinionated fact about the rules' Strong K3
  values and any fail-closed-specific framing is a documented, opt-in
  reading on top of it (e.g. a second helper or a parameter), not a silent
  change to what "conflict" means.

## 3. API shape

Three new surfaces, all thin layers over the existing pairwise primitives —
no new BDD algebra beyond the one new rail combination in §2 reading (2).

```csharp
namespace TruthWeaver.Analysis;

// One pairwise conflict check, parallel to RuleImplication/RuleRelation.
public static class RuleConflict
{
    public static RuleConflictResult Compare<TContext>(
        CompiledRule<TContext> first,
        CompiledRule<TContext> second,
        int? maxAnalysisTerms = null);
}

public enum RuleConflictOutcome { NoConflict, Conflicts, Undecided }

public sealed record RuleConflictResult(
    RuleConflictOutcome Outcome,
    IReadOnlyDictionary<string, TruthValue>? Witness, // one is True, the other False
    string? Reason);

// Set-level analysis over N rules, built entirely from the pairwise primitives.
public static class RuleSetAnalysis
{
    // One row per unordered pair; O(N^2) pairwise calls, each capped as today.
    public static IReadOnlyList<RuleSetPairResult> ComparePairs<TContext>(
        IReadOnlyList<CompiledRule<TContext>> rules,
        int? maxAnalysisTerms = null);

    // Which rules are implied by the OR of the rest of the set (dropping them
    // changes no satisfying assignment of the set's union).
    public static IReadOnlyList<RuleRedundancyResult> FindRedundant<TContext>(
        IReadOnlyList<CompiledRule<TContext>> rules,
        int? maxAnalysisTerms = null);
}

public sealed record RuleSetPairResult(
    int FirstIndex, int SecondIndex,
    RuleRelation Relation, RuleConflictOutcome Conflict);

public sealed record RuleRedundancyResult(
    int Index, bool IsRedundant, string? Reason);
```

- `ComparePairs` is a straight `O(N^2)` loop calling `RuleImplication.Compare`
  and `RuleConflict.Compare` per pair; no new analysis primitive, just
  packaging. This is deliberately the minimal set-level surface: a table a
  caller can render or assert against, not a ranking or a graph.
- `FindRedundant` needs one new primitive: "is rule `i` implied by `OR` of
  `{rules} \ {i}`." This reuses `RuleImplication.Implies` with the second
  argument built as a transient `OrExpression` over the other rules'
  `CompiledRule.Root` trees (an in-memory tree, never compiled/printed/stored
  — `RuleImplication.Implies` only needs an `Expression`, not a serialized
  rule) and the existing `TContext` constraint (every rule in the set must
  share one `TContext`, same as `RuleImplication`/`RuleEquivalence` already
  require). `N` calls, each itself bounded by `MaxAnalysisTerms` over the
  terms of rule `i` plus all the others combined — this is the most expensive
  operation in the set, see §4.
- Everything stays `CompiledRule<TContext>`-typed and reuses
  `CompilerOptions.MaxAnalysisTerms`/the `Undecided` convention: no new "cap
  exceeded" behavior to design, just more call sites for the one that exists.

## 4. Cost limits

Three layers of cost, from cheapest to most expensive:

1. **Per-pair cost is unchanged from ticket 13**: one shared `BddManager` per
   pair, capped by `MaxAnalysisTerms` counted across that pair's distinct
   terms (default 20). This bound is already in production use and this
   spike adds no new per-pair cost beyond the one extra rail combination for
   `RuleConflict` (§2), which is `O(1)` BDD operations on rails already built.
2. **`ComparePairs` is `O(N^2)` pairs**, each independently capped. For a set
   of `N` rules this is `N(N-1)/2` independent `BddManager` builds — there is
   no sharing of BDD nodes *across* pairs today, because `CompareSatisfaction`
   constructs a fresh manager per call. For small `N` (tens of rules, the
   scale a hand-authored rule set is likely to be) this is fine; for a large
   `N` (hundreds+) this is the first thing that gets expensive, and it is a
   wall-clock/call-count cost distinct from `MaxAnalysisTerms`, which only
   caps term count inside one pair. **This spike recommends a second,
   explicit cap: `maxRules` (or reuse of an existing list-size convention) on
   `RuleSetAnalysis` methods**, returning a per-call `Undecided`-style
   sentinel (a `TooManyRules` outcome, or a thrown `ArgumentException` the
   way an unbounded `IReadOnlyList<CompiledRule<TContext>>` parameter would
   need one anyway) rather than silently doing `O(N^2)` work for an
   arbitrarily large caller-supplied list — open decision 1 below.
3. **`FindRedundant` is the expensive one.** Each of the `N` calls analyzes
   rule `i` against the `OR` of up to `N - 1` other rules, so its term count
   is the union of *all* rules' terms, not just one pair's — this hits
   `MaxAnalysisTerms`'s cap much sooner than `ComparePairs` does, and a
   realistic rule set (even a modest one, say 15 rules with 5 terms each and
   little overlap) can plausibly have 50+ distinct terms, over the default
   cap of 20. **This method will frequently return `Undecided` for real
   rule sets at the default cap**, which is an honest result (ADR-0005's "no
   silently wrong" principle) but means `FindRedundant` needs its own,
   clearly separate `maxAnalysisTerms` guidance in its doc comment ("this
   typically needs raising for sets larger than a handful of rules") rather
   than quietly inheriting the same default that works fine for
   `ComparePairs`.

## 5. Risks

- **Scope creep into ticket 7's territory.** "Conflict" only has teeth once a
  combining algorithm exists (deny-overrides, first-match, etc.); without one,
  this spike can report facts about the rules' Strong K3 values but cannot
  say whether a given overlap or disagreement is a *problem* for any specific
  host. The API in §3 is deliberately framed as "here are the facts," leaving
  "is this a problem" to ticket 7 or the caller.
- **`FindRedundant`'s cost cliff (§4.3)** makes it the part most likely to
  disappoint a caller who expects it to "just work" on a real-sized rule set
  without raising `maxAnalysisTerms` — this needs to be loud in the API
  surface (doc comment, maybe a distinct default), not a silent `Undecided`
  a caller has to debug.
- **No existing precedent for `IReadOnlyList<CompiledRule<TContext>>` as a
  parameter shape.** Every ticket-13 API takes exactly two rules; this is the
  first place TruthWeaver's public surface takes "a set of rules," and no
  other package (`TruthWeaver.Tool.Core`, the CLI) has a rule-set concept to
  model this against yet. Picking the shape now is a real design decision,
  not just packaging — flagged as open decision 2.
- **Redundancy's definition assumes OR-combination.** "Rule `i` is redundant"
  as defined in §3 only makes sense if the rules in the set are meant to be
  combined with logical `OR` (any one holding is enough) — a set meant to be
  combined with `AND`, or scored, or run through a policy combining algorithm,
  has a different (and ticket-7-shaped) notion of redundancy that this spike
  does not attempt.

## 6. Composition with quantifiers (ticket 22)

Per ticket 22's note §4/§5 and open decision 5 there: a quantifier
(`forall`/`exists`) is proposed as **one opaque dual-rail term** for analysis
purposes — it gets a single BDD variable pair and the analyzer does not look
inside its sub-rule. Everything in this spike (`RuleRelation.Compare`,
`RuleConflict.Compare`, `FindRedundant`) is built entirely on
`Analyzer.CompareSatisfaction`/`FindDifference`/`DistinctTerms`, which treat
any `Expression` node uniformly through `ExpressionShape`'s operand walk
(`Analyzer.cs:271-289`) — **so if ticket 22 ships the opaque-term treatment,
this spike's set-level analysis needs no further change**: a rule containing
a quantifier is just a rule with one more (opaque) term, and overlap/conflict/
redundancy are computed exactly as for any other term. The only
consequence worth stating explicitly in both notes: any "conflict" or
"overlap" verdict involving a quantifier is only as precise as the opaque
treatment allows (ticket 22 §4's known precision loss — e.g. it cannot see
that two different quantifiers over the same source happen to be logically
related), which is an acceptable, already-documented limitation rather than a
new one this spike introduces.

## Recommendation

**Go**, with a minimal first slice:

1. `RuleConflict.Compare` (§2 reading 2) — one new type, one new rail
   combination, no new algebra; ships alongside the existing
   `RuleRelation`/`RuleEquivalence` family.
2. `RuleSetAnalysis.ComparePairs` (§3) — thin `O(N^2)` packaging over
   `RuleImplication.Compare` and `RuleConflict.Compare`; no new per-pair cost.
3. Defer `FindRedundant` to a second slice (or drop it from the first ticket
   entirely) until open decision 1 (a rule-count cap) and the
   `MaxAnalysisTerms` guidance (§4.3) are settled — it is the one piece with
   a real, not-yet-bounded cost story, and shipping it unbounded would be the
   kind of silent-cost surprise ADR-0001/0002 explicitly try to avoid
   elsewhere in the library.
4. "Conflict" ships as reading (2) only (opposite firm answers); reading (1)
   is already available via `RuleEquivalence.FindDifference` and needs no new
   API; reading (3) is explicitly out of scope, deferred to ticket 7.

This is a "go" because every piece except the one new rail combination in §2
is packaging over code ticket 13 already shipped and already tests
(`RuleImplicationTests`, `RuleEquivalenceTests`); the risk is concentrated in
picking the right set-shape API (open decision 2) and the redundancy cost
story (open decision 1), not in new analysis machinery.

## Open decisions (for grilling, one at a time)

1. **Does `RuleSetAnalysis` need an explicit cap on the number of rules in
   the set (`maxRules`), separate from `MaxAnalysisTerms`'s per-pair term
   cap?**
   *Recommended: yes.* `ComparePairs` is `O(N^2)` calls and `FindRedundant` is
   `O(N)` calls each analyzing the union of all other rules' terms (§4); an
   unbounded caller-supplied list has no other backstop today. A generous
   default (e.g. 50) with an `Undecided`/`TooManyRules`-style result (not an
   exception) keeps the "never throws for a normal-sized input" contract the
   rest of the analyzer family already has.
2. **What is the right public shape for "a set of rules" — a plain
   `IReadOnlyList<CompiledRule<TContext>>` parameter (as sketched in §3), or a
   small named type (e.g. `RuleSet<TContext>`) that could later carry a
   combining-algorithm hint for ticket 7?**
   *Recommended: the plain list for this ticket.* Introducing a named
   `RuleSet<TContext>` type now, before ticket 7 exists to say what it needs
   to carry, risks guessing wrong about its shape; a list is the minimal
   commitment and costs nothing to wrap in a named type later if ticket 7
   needs one.
3. **Should a softer "possible conflict" tier exist (A is `True`, B is
   `Unknown`-but-possibly-`False`, read as a conflict under fail-closed
   gating per §2's third bullet), alongside the exact `RuleConflict`
   verdict?**
   *Recommended: not in the first slice.* It is a legitimate but
   fail-closed-specific reading layered on top of the exact Strong K3 facts;
   adding it now would blur what "conflict" means in the core verdict. Revisit
   once a host (plausibly ticket 18's ASP.NET Core adapter, which is already
   fail-closed) asks for it.
4. **Should `RuleConflict`/`RuleSetAnalysis` be a new type family (as
   sketched) or methods added to the existing `RuleImplication`/
   `RuleEquivalence` static classes?**
   *Recommended: new types* (`RuleConflict`, `RuleSetAnalysis`), mirroring how
   `RuleImplication` and `RuleEquivalence` are already separate classes for
   separate questions rather than one grab-bag class — keeps each class's
   public surface answering one question.

## Decisions (owner, grilled 2026-10-16)

1. Rule cap: yes. A `maxRules` cap (default about 50). Exceeding it returns an undecided-style `TooManyRules` result, not an exception.
2. Set shape: both. Analysis methods take `IReadOnlyList<CompiledRule<TContext>>`, plus a thin immutable `RuleSet<TContext>` wrapper with no combining-algorithm behavior yet. Ticket 7 may extend it.
3. No softer "possible conflict" tier in the first slice.
4. New types `RuleConflict` and `RuleSetAnalysis`, not methods on `RuleImplication` or `RuleEquivalence`.
5. A rule with a quantifier is one opaque term (ticket 22, decision 5).
