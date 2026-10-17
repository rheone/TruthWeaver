> **Superseded (owner, 2026-10-16):** a duplicate spike. The authoritative note is [22-quantifiers-design.md](22-quantifiers-design.md). The decisions at the end of this file are carried into it where they fit.

# 22: Spike — quantifiers over collections

**Status:** spike design note. Produces no library change. A grilling session on
this note is the next step (ticket 22b stays on hold until then).

## Scope recap

Explore `forall x in <query>: <sub-rule>` and `exists x in <query>: <sub-rule>`:
grammar across DSL/JSON/YAML/builder, the scope a sub-rule reads an element in,
K3 semantics (empty collection, `Unknown` elements, fault budgets), the trace
shape for a variable-size evaluation, composition with data sources
(ADR-0006) and with the analyzer/BDD (whose term set is fixed at compile
time today), effect on rewrites and `MaxNodeCount`, and a recommendation with
a minimal first slice.

## 1. What a quantifier would be built from

The codebase already has the piece quantifiers generalize: cardinality. ADR-0005
decision 6 defines `ALL = AtLeast(n, ...)` and `ANY = AtLeast(1, ...)` over a
**fixed, compile-time-known** operand list. A quantifier is the same cardinality
test (`AtLeast(n)` for `forall`, `AtLeast(1)` for `exists`) over an operand list
whose **length is known only at evaluation time** — it comes from a collection a
data source (ADR-0006) or a predicate supplies, not from the rule text.

That reframing is useful because it means the semantics should not be invented
from scratch: `forall`/`exists` should be defined as "`ALL`/`ANY` generalized to
a runtime-determined arity," and should inherit the existing dual-rail
(definite/possible count) cardinality-interval evaluation and analysis, not a
new value-combination rule. Concretely:

- **Empty collection.** `ThresholdExpansion`/`NormalForms.AtLeast` already treat
  `AtLeast(k, operands)` with `k <= 0` as the constant `True` regardless of `n`.
  So `ALL` over zero operands (`AtLeast(0, [])`... really `AtLeast(n=0, [])`) is
  `True`, and `ANY` over zero operands (`AtLeast(1, [])`, `k=1 > n=0`) is `False`.
  This is exactly the classical vacuous-truth convention (`forall` over `{}` is
  `True`, `exists` over `{}` is `False`), and it falls out of the existing
  threshold code for free if quantifiers reuse it. No new rule is needed for the
  empty case.
- **`Unknown` elements.** The threshold evaluator already reasons in terms of a
  definite-true count and a possible-true count (an `Unknown` operand is
  possible but not definite) rather than a single tri-state fold. A quantifier
  over a collection containing `Unknown`-valued elements is `Unknown` exactly
  when the collection's definite/possible interval straddles the threshold —
  same rule as `ALL`/`ANY` today, generalized to a count found at evaluation
  time instead of compile time.
- **Fault budget.** `EvaluationOptions.FaultBudget` is a property of the
  `Evaluator` instance for the whole evaluation (`CompiledRule.EvaluateAsync`
  constructs one `Evaluator` and passes it through the whole tree walk;
  `src/TruthWeaver/Evaluation/Evaluator.cs`, `src/TruthWeaver/Evaluation/CompiledRule.cs:1391-1430`).
  A per-element predicate fault inside a quantifier is just another fault in
  that same count — no new concept, provided the per-element evaluation runs
  through the same `Evaluator`/fault-tracking path as every other subtree
  (open decision 1 below is whether a *separate* per-element budget on top of
  that is wanted).

## 2. Grammar and API shape

A quantifier needs three things: a **source expression** that yields a sequence
of elements, a **bound variable** naming the current element inside the
sub-rule, and the **sub-rule** itself (a predicate expression over that element,
written once and evaluated once per element).

- **DSL:** `forall x in <source>: <sub-rule>` / `exists x in <source>: <sub-rule>`
  reads naturally and matches the ticket's sketch. `forall`/`exists`/`in` would
  join the reserved-word list (like `If`, `COALESCE`, `ANY`). Precedence: like
  `ANY(...)`/`If(...)`, a quantifier should be a function-call-shaped
  construct with no infix precedence, so it composes with `AND`/`OR` the same
  way `ANY(...)` does today, without decision-8's "no mixing infix operators"
  question arising.
- **JSON/YAML:** `{"op": "forall", "source": {...}, "as": "x", "predicate": {...}}`
  (and `"op": "exists"`), following the existing `{"op": ..., "operands": [...]}`
  shape family but with `source`/`as`/`predicate` replacing `operands`, because
  the arity is not fixed. `rule-tree.schema.json` gains a
  `quantifierOperatorNode` the same way `betweenOperatorNode` was added for
  `BETWEEN` (ADR-0005 decision 13).
- **`RuleBuilder`:** `RuleBuilder.ForAll(source, "x", body)` /
  `RuleBuilder.Exists(source, "x", body)`, where `body` is a normal
  `RuleBuilder` sub-tree that may reference the bound variable (see §3).
- **The source expression.** The natural source is an ADR-0006 data-source
  query that resolves to an array (`from("orders", "$.orders[*]")`): decision 6
  there already defines "an array argument collects every match." Today that
  array becomes a single `LiteralValue` of kind `*Array` handed to **one**
  predicate call, not a sequence TruthWeaver iterates itself. ADR-0006 decision
  8 explicitly puts "iteration over repeated subtrees" **out of scope** — this
  spike is the proposal to bring it back in, scoped narrowly to the quantifier
  construct rather than as a general rule-text iteration primitive.

## 3. Scope: typed element context vs. untyped nodes

This is the central design fork, and it interacts with how predicates are
typed today. `IPredicate<TContext>`/`PredicateRegistry<TContext>` compile and
evaluate against **one** context type per rule (`RuleCompiler<TContext>`,
`CompiledRule<TContext>`). A quantifier's sub-rule needs to evaluate a
predicate per collection element, and the element has to arrive at that
predicate somehow. Two shapes:

- **(A) Untyped element, data-source-native.** The source collection stays
  inside one data source's own node model (a JSON/YAML array), the bound
  variable is a *new* named data source scoped to one element
  (`IDataSource.ScopeAsync`, which already exists for exactly "a host wants to
  narrow one repeated subtree"), and the sub-rule's predicates read the
  element only through ordinary `from("x", "$.field")` variable references.
  The outer `TContext` is untouched; no new predicate-typing concept is
  needed. This is the cheaper, more consistent-with-ADR-0006 shape, but it
  only lets a quantifier range over something a data source can already
  express (JSON/YAML nodes, or a future source over anything else) — it
  cannot range over a plain `IEnumerable<T>` already sitting on `TContext`
  without first projecting it through a data source.
- **(B) Typed element, predicate-native.** The source is a predicate argument
  or a new predicate category that yields a `TContext`-derived element
  sequence directly (e.g. `IPredicate<TContext>` gains a sibling
  "enumerates `TElement`s from `TContext`" concept, and the sub-rule compiles
  against a synthesized `TContext` for the element). This lets a quantifier
  range over anything the host's object model has, not only JSON-shaped data,
  but it needs a second, parallel predicate-typing surface (an
  `IElementSource<TContext, TElement>` or similar), a second compile step (the
  sub-rule needs its own registry, scoped to `TElement`, resolved and compiled
  alongside the outer rule), and it does not reuse any of ADR-0006's existing
  query/scope/cardinality machinery.

**Recommendation: start with (A).** It composes directly with ADR-0006 (which
already has the scoping and array-cardinality primitives a quantifier needs)
and adds no second predicate-typing surface. (B) is a plausible *later*
extension once a concrete host need shows up (a predicate library already has
elements that are not reachable through a data source), but it should not be
in the first slice — it roughly doubles the design surface (a second registry
shape, a second compile path) for a need this spike has not seen evidence of
yet.

## 4. Composition with the analyzer and BDD

ADR-0005 decision 17 and `src/TruthWeaver/Analysis/Analyzer.cs`/`BddManager.cs`
build a dual-rail (definite/possible) BDD over **the distinct `TermIdentity`
values that occur in the compiled tree**, which is a fixed, finite, compile-time
set (`Analyzer.Analyze` collects terms by walking the tree once; `CompilerOptions.MaxAnalysisTerms`
caps that count). A quantifier's sub-rule, evaluated once per element at
runtime, does not have a fixed set of terms at compile time — the same
predicate call (e.g. `isActive(x)`) is logically a *different* term per
element, and the number of elements is not known until evaluation.

Two ways to keep the analyzer sound and finite:

1. **Treat the whole quantifier as one opaque term for analysis purposes** —
   give it a single pair of dual-rail BDD variables (as if it were an ordinary
   zero-argument predicate), and do not analyze inside the sub-rule at all.
   This keeps `Analyzer.Build`'s term count exactly as bounded as today (one
   more variable pair per quantifier node, not per element), is simple to
   implement, and is sound: nothing claims to know more about the quantifier
   than "it is some K3 value." It loses precision — `forall x in c: P(x) OR
   NOT P(x)`-shaped tautologies inside the sub-rule would not be caught — but
   that precision loss is explicit and bounded, not silently wrong.
2. **Symbolically model the quantifier's cardinality** (reason about "some `k`
   out of an unbounded `n`" the way `AtLeast` reasons about a bounded `n`).
   This is the "more honest" analysis but needs a new algebra (an unbounded
   threshold over an unknown count), is a materially larger analyzer change,
   and has no existing building block to reuse.

**Recommendation: (1) for the first slice.** It is a few hours of work (one
new `DualRail` case in `Analyzer.Build`, emitting a fresh opaque variable pair)
against a multi-week new-algebra project, and it does not foreclose (2) later
— the opaque treatment is always a valid (if imprecise) approximation.

## 5. Effect on `MaxNodeCount` and the rewrites

`RuleNodeCompiler.Build` counts nodes as it compiles (`this.nodeCount++`,
`src/TruthWeaver/Compilation/RuleNodeCompiler.cs:153-174`) and this is
**compile-time**, independent of any runtime collection size: a quantifier
node is compiled once, with its sub-rule compiled once as a template, so it
contributes a small, fixed number of nodes to the count regardless of how many
elements the collection holds at evaluation time. This is an important
property to preserve deliberately: it means `Compile` keeps its "never throws
for authoring errors, bounded by `MaxNodeCount`/`MaxDepth`" contract untouched,
and a large or unbounded collection at evaluation time is a *runtime* cost
question, not a compile-time one (a separate, new concern — see open decision
2 below, since nothing today bounds per-evaluation element-iteration work).

The expansion/compression/canonicalization/simplification rewrites
(ADR-0005 decision 10, `src/TruthWeaver/Rewriting/`) are all defined over a
**fixed-arity** tree today (`ExpandToPrimitives`, `ThresholdExpansion.AtLeast`,
etc. all iterate a known `IReadOnlyList<Expression> operands`). A quantifier
node has no such fixed operand list — its "operands" are one sub-rule template
plus an evaluation-time-only element count — so none of the existing rewrites
can look inside it. The right contract for the first slice is the one
`COALESCE` already has for `NAND`/`NOR` expansion (ADR-0005 decision 10):
**a quantifier is a rewrite boundary.** `ExpandToPrimitives`/`ExpandToNand`/
`ExpandToNor`/`CompressToDerived`/`Canonicalize`/`Simplify` all recurse into
the sub-rule (so the sub-rule's own operators still expand/simplify/canonicalize
normally) but do not try to unfold the quantifier itself into a fixed-arity
form. This is sound (every rewrite stays "never larger," because nothing is
unfolded) and costs nothing but a pass-through case in each rewrite's switch.

## 6. The trace shape

`TraceNode` (`src/TruthWeaver.Abstractions/TraceNode.cs`) pairs a compiled
node 1:1 with its trace: in particular `RuleCoverageAccumulator.Pair`
(`src/TruthWeaver/Evaluation/RuleCoverageAccumulator.cs:257-287`, which backs
ticket 04, rule coverage over a corpus) asserts `traceNode.Children.Count ==
children.Length` for the *compiled* operand count and throws a mismatch
otherwise. A quantifier's trace cannot honor that invariant literally: the
compiled tree has one sub-rule template, but the trace of one evaluation has
one child *per element actually evaluated* (which varies by collection size
and by short-circuiting within `forall`/`exists`, e.g. `exists` stops at the
first `True` element under `EvaluationMode.ShortCircuit`).

Proposed shape: the quantifier's `TraceNode.Children` holds one child per
element visited (in source order, truncated by short-circuiting exactly as
`AND`/`OR` chains already truncate today), each child being the sub-rule's own
trace tree for that element, and the quantifier node's own `Text` carries the
bound variable and source (e.g. `forall x in orders`). This is internally
consistent with how `AND`/`OR` already report a variable number of evaluated
children (`NotEvaluated` short-circuited tail) — the only new fact is that the
*maximum* possible child count is not fixed at compile time. The real
consequence is for consumers that assume compiled-arity equals traced-arity:
`RuleCoverageAccumulator` is the one in-repo example, and it needs a
quantifier-aware case (count element-visits as "term-like" coverage rather than
failing the 1:1 arity assertion) before ticket 04 (rule coverage) and ticket 22
(quantifiers) can coexist — flagged as a sequencing dependency, not solved
here. `CompiledRule.Outline()`/`DotPrinter`/`MermaidTreePrinter`, which render
the *compiled* tree without trace data, are unaffected: they only ever show
the sub-rule once, as a template child, which is already correct for a
structure diagram.

## 7. Prototype

This spike is scoped to a design note (no library code, per the ticket and
per this task's instructions). The semantics in §1 were desk-checked against
`ThresholdExpansion`/`NormalForms.AtLeast`'s existing empty-collection and
`Unknown`-operand handling rather than against new running code: those two
code paths already implement exactly the vacuous-truth and
definite/possible-interval behavior a quantifier needs, generalized from a
compile-time `n` to a runtime `n`. An executable prototype (an `Evaluator`-only
path that walks a `LiteralValue` array and folds the sub-rule's
`TruthValue` per element using the same counting the threshold evaluator uses)
is a short follow-up, and per the ticket's own checklist belongs in a scratch
branch or `.scratch/`, not in this note or in library code.

## Recommendation

**Go**, with a minimal first slice:

1. One construct, not two generalized ones: `forall`/`exists` only (no
   general `map`/`reduce`/arbitrary-aggregate-over-collection sugar).
2. Source shape (A) from §3: the collection comes from an ADR-0006 data
   source, the bound variable is a per-element `ScopeAsync` source, no new
   predicate-typing surface.
3. Semantics are `ALL`/`ANY` generalized to a runtime count, reusing the
   existing definite/possible cardinality interval — no new value table.
4. Analyzer treats the whole quantifier as one opaque dual-rail term (§4,
   option 1) — sound, cheap, intentionally imprecise.
5. Rewrites treat the quantifier as a boundary: recurse into the sub-rule,
   never unfold the quantifier itself (§5).
6. Trace: one child per element visited, same short-circuiting convention as
   `AND`/`OR`/`COALESCE` (§6) — and ticket 04 (rule coverage) needs a
   quantifier-aware update before it can be used on a rule containing one.

This slice reuses four existing mechanisms (cardinality intervals, data-source
scoping, the rewrite-boundary pattern `COALESCE` already established, and the
short-circuit trace convention) instead of inventing new ones, which is why it
is scoped as a "go": the risk is concentrated in one genuinely new piece (the
opaque-term analyzer case) and one sequencing dependency (ticket 04), not
spread across the whole stack.

## Open decisions (for grilling, one at a time)

1. **Does a quantifier need its own fault budget, separate from the whole
   evaluation's `EvaluationOptions.FaultBudget`?**
   *Recommended: no for the first slice.* A per-element fault is just another
   fault in the existing whole-evaluation budget (§1); a separate per-quantifier
   budget is a real feature (e.g. "tolerate up to 3 bad elements out of a
   thousand") but is additive and can follow once a host asks for it.
2. **Is there a cap on elements iterated per evaluation (a runtime work
   budget), given `MaxNodeCount` only bounds the compiled template (§5)?**
   *Recommended: yes, a new `EvaluationOptions.MaxQuantifierElements` (or
   similar), defaulting to a generous but finite number,* so a host cannot be
   made to iterate an attacker-controlled unbounded collection inside one
   evaluation; exceeding it should behave like a fault budget exhaustion
   (`Unknown` plus a fault), not an exception, to stay inside ADR-0001/0002's
   fail-as-data model.
3. **Should the element source be restricted to ADR-0006 data sources only
   (shape (A)), or should a predicate-native element source (shape (B), §3)
   be designed now even though no concrete host need has been identified?**
   *Recommended: data sources only for the first slice;* revisit (B) if a
   host need appears, since it is additive (a quantifier over a predicate's
   own typed sequence could be a second `source` kind later without changing
   the first slice's grammar for the data-source case).
4. **Does `forall`/`exists` allow nesting (a quantifier inside another
   quantifier's sub-rule, over a nested collection)?**
   *Recommended: allow it* — nothing in §1–§6 assumes single-level nesting,
   the sub-rule is an ordinary rule tree that may itself contain a quantifier,
   and disallowing it would need an explicit, enforced restriction for no
   clear benefit. `MaxDepth` already bounds nesting depth generally.
5. **What does ticket 21's overlap/conflict analysis do with a rule containing
   a quantifier, given ticket 21 is also a spike running over the same term
   model the analyzer uses?**
   *Recommended: flag as a cross-spike dependency, not decided here* — ticket
   21's note should state whether it treats a quantifier as an opaque term too
   (consistent with §4) once both notes exist, since the two spikes share the
   BDD/term-identity model.

## Decisions (owner, grilled 2026-10-16)

1. Fault budget: shared with the whole evaluation's `FaultBudget`. No per-quantifier budget in the first slice.
2. Element cap: yes. A new `EvaluationOptions` cap on elements iterated, with a generous finite default. Exceeding it gives `Unknown` plus a fault, not an exception.
3. Element sources: design the predicate-native source (shape B) now, as well as data sources (shape A). This differs from the recommendation; the build ticket (22b) must include the shape B design.
4. Nesting: allowed. `MaxDepth` bounds it.
5. Ticket 21: analysis treats a quantifier as one opaque dual-rail term.
