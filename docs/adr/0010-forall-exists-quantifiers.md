# ADR-0010: FORALL and EXISTS over collections

## Status

Accepted (2026-10-16). Amended 2026-10-17: the registration shape of the untyped element read is fixed (see the
amendment under decision 3, which closes the first open question). Amends [ADR-0005](0005-strong-k3-language-surface.md)'s operator and connective
lists in place (dated note there, pointing here). Records the owner's decisions from the grilling session
on `.scratch/feature-backlog-2026-10-11/notes/22-quantifiers-design.md` (spike output, 2026-10-11) and the
scope `.scratch/feature-backlog-2026-10-11/issues/22b-quantifiers-build.md` (parent ticket) set on
2026-10-16. Ticket: [22b-1](../../.scratch/feature-backlog-2026-10-11/issues/22b-1-quantifiers-adr.md).
Parent: [22b](../../.scratch/feature-backlog-2026-10-11/issues/22b-quantifiers-build.md). This ADR defines
the grammar, semantics, limits and analyzer/rewrite boundaries that the implementing tickets (22b-2 through
22b-6) build against; it changes no shipped API itself.

## Context

A host today writes one C# predicate per collection question (`allLinesInStock`, `anyLineOverLimit`)
because the rule language has no way to range over a collection. A rule author cannot combine element
conditions with the engine's own operator set; every combination needs a new predicate. The design note
above explores adding two connectives, `FORALL` and `EXISTS`, that range over a collection the host
registers, with the element condition written as an ordinary rule body.

The note's section 3 shows that, in Strong Kleene terms, `FORALL` and `EXISTS` are exactly the generalized
`AND` and `OR` over the elements, so they are information-monotone connectives, not external operators
([ADR-0005](0005-strong-k3-language-surface.md) decision 18's distinction). The note's section 6 shows why
they still need a new ADR rather than only new evaluator code: the term set of a quantified rule is not
fixed at compile time (it depends on the collection's size at evaluation time), so the analyzer, the BDD
and every consumer that lists "the terms of a rule" must treat a quantifier as an opaque atom. That
boundary, and the other boundaries the note works out (trace shape, rewrites, limits), are owner decisions
this ADR records before any grammar, compiler, evaluator or analyzer code changes.

## Decision

### 1. FORALL and EXISTS are Strong Kleene connectives

`FORALL` and `EXISTS` join the K3 connective list of [ADR-0005](0005-strong-k3-language-surface.md)
decision 18 (`NOT`, `AND`, `OR`, `IMPLIES`, `EQUIVALENT`, `XOR`, `NAND`, `NOR`, `PARITY`, the cardinality
operators and `If`). Over a known, finite list of element values `v1..vn`, `FORALL` is `AND(v1..vn)` and
`EXISTS` is `OR(v1..vn)`; both are monotone in the information order, so the "no tautologies" result that
holds for every connective (ADR-0005 decision 18) extends to them: `FORALL(x IN c, b)` is never a tautology
when `b` contains a term. They are not primitives or derived operators in the sense of decision 3 (they do
not reduce to a fixed kernel formula, because `n` is known only at evaluation time); they are a third kind,
a **quantifier**, alongside the gates and the cardinality functions.

### 2. Grammar

- **DSL.** A function-call form with no precedence, like `PARITY`, `ANY` and `If`
  (`FORALL(line IN lines, qtyAbove(n: 0) AND inStock)`, `EXISTS(line IN lines, IsTrue(backordered))`). The
  head is `<variable> IN <collection>`: `<variable>` is an identifier, `<collection>` is a registered
  collection name. `FORALL` and `EXISTS` are new reserved words, case-insensitive on input, canonical upper
  case on output, so a predicate cannot be registered with either name. `IN` is a **contextual** keyword: it
  is reserved only in a quantifier head, so a predicate named `In` is unaffected elsewhere. `ALL` and `ANY`
  are not reused as quantifier spellings; they stay the n-ary cardinality operators over written operands,
  because overloading one word with two arities and two parse paths is confusing. The bound variable name is
  **required** and is part of tree equality and canonical text (it labels the trace, for example `line[2]`,
  and the body's element reads resolve through it).
- **JSON and YAML.** `{"op": "forall" | "exists", "var": "line", "in": "lines", "operands": [<body>]}`,
  following the existing `{"op", "operands"}` pattern with extra fields, as `between` does with `min`/`max`.
  `operands` holds exactly one item, the body (`MalformedTree` otherwise), so body diagnostics get the path
  `$.operands[0]...` like every other node. `rule-tree.schema.json` gains a quantifier operator node.
- **`RuleBuilder`.** `RuleBuilder.ForAll(variable, collection, body)` and `RuleBuilder.Exists(...)`, which
  render to the same JSON tree, so the existing cross-form equality test covers them like every other
  operator.
- **Printers.** The canonical printer writes `FORALL(line IN lines, body)`; the tree printers write
  `FORALL line IN lines` (word style), `∀ line ∈ lines` (symbolic) and keep the word in C style; the
  equation printers write `∀ line ∈ lines . (body)`. The `∀`/`∃` input-only symbol aliases are not part of
  slice 1 (see "Not in slice 1" below); the symbolic *output* spelling above is unaffected, because ADR-0005
  decision 2 already lets a symbol be output-only before it is an accepted input alias. The Outline has one
  node with the body as its only child.

### 3. Element scope: both typed and untyped reads, in slice 1

The design note's section 2 frames this as a choice between two designs for how a quantifier's body reads
the element: **A**, a typed collection binding (`PredicateRegistryBuilder.AddCollection<TItem>`, element
predicates over `Element<TOuter, TItem>` with `Outer`/`Item`/`Index` properties - the note's recommendation)
and **B**, untyped reads through `from("line", "...")` data-source arguments. The parent ticket's decision
departs from the note's recommendation of A alone: **the collection itself is always a typed collection
binding (option A's range) in slice 1** - the note's option B range (`FORALL(line IN from("order",
"$.lines[*]"), ...)`, enumerating a data source directly) is deferred to 22c, below - **but a body reads an
element through both mechanisms in slice 1**: typed element predicates registered on the collection's
element registry (`Element<TOuter, TItem>`), and, additionally, an untyped data-source read of the element
itself, so a body term may use `from(line, "$.qty")` against the per-element value, as if that element were
individually scoped the way `IDataSource.ScopeAsync` scopes a node today (ADR-0006 decision 8). A collection
whose `TItem` is not projectable to a data-source node supports only the typed path; a body that still
writes `from(<variable>, ...)` against such a collection is an undeclared-source diagnostic, the same
diagnostic ADR-0006 decision 3 already gives for a variable that names no supplied source. The exact
registration shape for the untyped projection (an additional delegate parameter on `AddCollection`, a
separate opt-in registration call, or an interface `TItem` can implement) was left open when this ADR was
accepted; the amendment below fixes it.

Reused unchanged from the note for the typed path: `AddCollection<TItem>(string name, Func<TContext,
IEnumerable<TItem>?> selector, Action<PredicateRegistryBuilder<Element<TContext, TItem>>> elements)`.
Nested collections register on the element builder, so an outer chain is typed (`Element<Element<Order,
OrderLine>, Tag>`). Collection names are a namespace separate from predicate and data-source names; an
unknown collection name is a diagnostic with a "did you mean" suggestion over the collection names in scope.
A body term resolves only in the element registry (and, for the untyped path, the element's own bound data
source) of its own quantifier; outer data reaches an element predicate through `Element.Outer` in C#, not
through rule text. Outer predicates usable directly inside a body are out of scope for slice 1 (below).

> **Amendment (2026-10-17, the `elementSource` registration shape):** the untyped element read is opted into with an
> additional, optional delegate parameter on `AddCollection<TItem>`, not with a separate registration call and not with
> an interface that `TItem` implements:
>
> ```csharp
> AddCollection<TItem>(
>     string name,
>     Func<TContext, IEnumerable<TItem>?> selector,
>     Action<PredicateRegistryBuilder<Element<TContext, TItem>>> elements,
>     Func<TItem, IDataSource>? elementSource = null)
> ```
>
> The owner confirmed this shape on 2026-10-17 (main review ticket 08), as the code already had it. Why:
> `elementSource` is a projection from the element to an `IDataSource`, so a host can use an existing source type such as
> `JsonDataSource` without changing its element class (an interface on `TItem` would force that), and a collection that
> needs only the typed path passes nothing, so the typed registration of this decision is unchanged. The rules of the
> shape:
>
> - **`null` (the default) means typed reads only.** A body that writes `from(<variable>, ...)` against such a collection
>   is the ordinary undeclared-source diagnostic of ADR-0006 decision 3.
> - **The source is named by the quantifier's variable.** When the body is compiled, the variable is declared as a data
>   source name with no query validator, because the dialect of the query is the projected source's own. A name that was
>   already declared keeps its validator.
> - **The element binding wins.** Inside the body, a source supplied to the evaluation under the same name as the
>   variable is hidden, because the variable names the element lexically. Every other supplied source stays reachable.
> - **The projection is lazy and a failure is a data failure.** The delegate runs at the first query of that element, not
>   when the element is bound. A delegate that throws is a source error: the term is `Unknown` plus a fault
>   ([ADR-0001](0001-kleene-failure-model.md), [ADR-0006](0006-data-sources-for-expression-variables.md) decision 9),
>   and the loop over the other elements goes on.

### 4. Strong Kleene semantics

| Case | Result | Fault | Rationale |
| --- | --- | --- | --- |
| Empty collection | `FORALL` = `True`, `EXISTS` = `False` | No | The identity elements of `AND` and `OR`; standard in classical and K3 logic, and the SQL `ALL`-over-empty-subquery precedent. |
| Element body is `Unknown` | Counts as `Unknown` in the fold | Only if the body itself faulted | Same as an `Unknown` operand of `AND`/`OR`. |
| Selector returns `null` | `Unknown` | No | The collection is not known; matches the catalog's default `NullBehavior.Unknown` and stays fail-closed. |
| Selector throws | `Unknown` | Yes, one fault for the collection | [ADR-0001](0001-kleene-failure-model.md): a failure becomes `Unknown` plus a fault. |
| `MaxElementEvaluations` exceeded | `Unknown` for the current quantifier | Yes, one fault | See Limits below. |

A `null` collection reading as `Unknown` means `FORALL(x IN c, True)` is **not** constant `True` (it is
`Unknown` when the collection is missing), so no rewrite may fold it to a constant; see Rewrites below.

### 5. Evaluation rules

- **Order and short-circuit.** Elements evaluate in selector order. `FORALL` stops at the first `False`
  element and `EXISTS` at the first `True`, unless `EvaluationMode.Exhaustive` is set; a skipped element is
  recorded as not evaluated, as `AND`/`OR` already do. Short-circuiting never changes the value.
- **Materialize once.** The selector is called at most once per (binding, outer element) within one
  evaluation, and its result is materialized to a list. This extends the predicate-author contract
  (`CONTEXT.md`) to collection selectors: the same answer for the same input within one evaluation.
- **Memoization is per element.** The memo key inside a body is `(scope path, term identity)`, where the
  scope path is the chain of `(binding, index)` pairs (for example `lines[2]/tags[0]`). A term repeated
  within one element's body is one call; the same term in two elements is two calls, because the element
  context differs. Two quantifiers over the same binding in one rule share the scope path and so share memo
  entries for equal terms.
- **Fault budget is global.** `EvaluationOptions.FaultBudget` counts every fault in the evaluation, wherever
  it happened; there is no separate per-quantifier budget. When the budget aborts inside a body, the
  remaining elements and the rest of the rule are not evaluated, exactly as today.
- **Cancellation and timeout are unchanged.** They throw `OperationCanceledException` and are not faults.

### 6. Limits

- **`EvaluationOptions.MaxElementEvaluations`**, a new runtime bound: the total number of body evaluations
  across every quantifier in one evaluation (nested quantifiers multiply: 50 x 50 is 2,500 body runs). Over
  the bound, the current quantifier is `Unknown` with one fault and the evaluation aborts, the same way the
  fault budget aborts. Default: 10,000. This is deliberately not unlimited, unlike every other
  `EvaluationOptions` default: the rule *author*, not the host calling `EvaluateAsync`, controls how deeply a
  rule nests quantifiers and over what collections, so an unbounded default would let authored rule text
  alone make one evaluation arbitrarily expensive - the same reason `CompilerOptions`' structural limits have
  non-`null` defaults.
- **`CompilerOptions.MaxQuantifierDepth`**, a new compile-time bound: how deeply quantifiers may nest, so the
  product of collection sizes stays predictable before any data is read. Default: 2. Exceeding it is a
  compile error with its own diagnostic (name to be allocated by the implementing ticket; see Open
  questions).
- `CompilerOptions.MaxNodeCount` and `MaxDepth` are unchanged in meaning: a quantifier counts as one node
  plus its body, once. They stay structural and compile-time; they do not bound evaluation cost, because
  that cost depends on the collection's size at evaluation time, which is what the two new bounds above
  exist to cap instead. `MaxRewriteNodeCount` is unaffected, because no rewrite expands a quantifier (see
  Rewrites below).

### 7. Trace shape

A quantifier node has one child per element instead of one child per written operand - the one controlled
exception to "the trace tree mirrors the rule's shape". Each element child is labelled `<variable>[<index>]`
and holds the body's own trace tree; the flat log prefixes each body entry with the scope path (for example
`line[1]: inStock`). An empty collection has no element children; a `null` collection is the quantifier node
itself, with value `Unknown` and a note that the collection was absent. **Every consumer that pairs trace
nodes with rule nodes by position** (coverage, and the printers that take a `Decision`) must fold the
element children onto the one body node: the body node's coverage becomes "reached in at least one element",
and its outcomes become the union over elements. Recording every element, with no cap, is the slice 1
choice; a cap with a summary node is a later option, not part of this ADR.

A fault raised inside a body would otherwise read as a fault on the term in general, so `Fault` gains an
optional, additive `Scope` property in `TruthWeaver.Abstractions`. It holds the element scope path (for
example `lines[734]`, or `lines[2]/tags[0]` when nested) and is `null` for a fault outside any quantifier.
The change is additive: existing members, constructors and `TermIdentity` are unchanged. A fault for the
collection itself (a throwing selector, or `MaxElementEvaluations` exceeded) carries the path of the
quantifier's own scope, without an element index.

### 8. Analyzer and term identity

- **A quantifier is an opaque atom** in the outer analysis: it gets its own pair of BDD variables (is
  `True`, is `Unknown`), keyed by its canonical text, which includes the body. Two syntactically equal
  quantifiers in one rule are the same atom. This is sound, because every K3 value of the atom is reachable
  for some collection.
- **The body is analyzed on its own**, in its own scope, with its own term variables, so a tautology or
  contradiction inside a body is still reported with the body's own path. The body analysis never reports
  "the whole quantifier is constant", because a `null` collection makes the quantifier `Unknown` regardless
  of the body.
- `CompilerOptions.MaxAnalysisTerms` applies to each analysis separately: the outer terms plus quantifier
  atoms, and each body on its own.
- **Term identity gains a scope.** A body term is identified by its predicate and arguments, as today, plus
  its scope (the binding path, such as `lines/tags`), kept on the internal term expression. The public term
  identity type is unchanged in slice 1, because no consumer outside the engine needs to compare terms
  across scopes yet. Consumers that list "the terms of a rule" (distinct terms, coverage, truth table,
  equivalence, counterfactual, compare) list the outer terms and the quantifier atoms, not the terms inside a
  body; each such consumer's own documentation states this. Equivalence and compare work on atoms: two rules
  with the same quantifier text compare correctly, and two quantifiers with equivalent but differently
  written bodies are different atoms (sound, incomplete).
- This matches ticket 21's ([ADR-0009](0009-overlap-and-conflict-definitions.md)) joint-outcome analysis,
  which also treats a quantifier as an opaque atom rather than redefining term identity across scopes.

### 9. Rewrites

| Rewrite | Effect on a quantifier |
| --- | --- |
| `ExpandToPrimitives`, `ExpandToNand`, `ExpandToNor` | The quantifier itself is not expanded - a documented boundary, as `COALESCE` already is, because `n` is known only at evaluation time. Its body is rewritten in place. |
| `ToNnf` | `NOT FORALL(x IN c, b)` becomes `EXISTS(x IN c, NOT b)`, and the dual, for every collection including empty and `null`. |
| `ToCnf`, `ToDnf` | The quantifier is an atom; its body is put into normal form on its own. |
| `Canonicalize`, `CompressToDerived` | The body is rewritten in its own scope; the quantifier itself is not reordered or merged with another quantifier. |
| `Simplify` | No constant fold applies to either operator (a `null` collection makes `EXISTS(x IN c, False)` `Unknown`, not `False`). Body-level simplification still applies. The sound merge `FORALL(x IN c, a) AND FORALL(x IN c, b)` = `FORALL(x IN c, a AND b)` is **not** applied in slice 1, because it changes which faults are reported and the trace shape, even though it preserves the value. |

Every rewrite that touches a quantified rule must still pass the project's exhaustive truth-table oracle;
for quantifiers the oracle runs over collections of size 0 to 4 plus `null`, with every element assignment,
as the prototype (below) already did.

### 10. Interaction with definitions (ADR-0008)

Definitions expand at compile time, before scope resolution, so a definition can be used either inside a
quantifier's body or outside it. A use inside a body is checked against the body's element registry at the
place of use; a use outside a body is checked against the outer registry. When the same definition is used
in both places with terms that resolve in only one of the two registries, the diagnostic names both the use
and the definition's own declaration span, the same pairing [ADR-0008](0008-definitions-and-parameters.md)
decision 8 already requires for every diagnostic raised inside an expansion.

### 11. Not in slice 1

Deferred, not rejected, and not authorized by this ADR:

- **Data-source ranges** (`FORALL(line IN from("order", "$.lines[*]"), ...)`, enumerating a data source
  directly as the collection) - a later ticket, 22c, with its own [ADR-0006](0006-data-sources-for-expression-variables.md)
  amendment (its decision 8 currently puts "iteration over repeated subtrees" out of scope) and a new
  `IDataSource` enumeration member.
- Counted quantifiers (`AtLeast(k, x IN c, b)`), which fall out of the interval semantics once the plain
  quantifiers exist.
- The input-only symbol aliases `∀` and `∃` (the output spelling in decision 2 above is unaffected).
- Outer predicates usable directly inside a body (a lookup chain across scopes), which would need
  scope-qualified term identity broader than decision 8 above introduces.
- A trace cap/summary node for large element counts.
- The `Simplify` merge of two quantifiers over the same collection, noted in decision 9 above.

## Prototype evidence

A throwaway prototype of the evaluation semantics (not committed to the repository;
`.scratch/feature-backlog-2026-10-11/notes/22-quantifiers-design.md` section 12 records its results) checked,
over collections of 0 to 5 elements and every `{True, False, Unknown}` assignment (364 exhaustive cases):
`FORALL` equals the `AND` fold and `EXISTS` the `OR` fold; `NOT FORALL(x, b)` equals `EXISTS(x, NOT b)` for
every assignment including the empty collection; short-circuit and exhaustive evaluation give the same
value; refining one `Unknown` element to a definite value never changes an already-definite result
(monotonicity); a global fault budget aborts correctly across several faulting elements; a `null` collection
is `Unknown` with no fault and a throwing selector is `Unknown` with one fault; a term repeated within one
element is memoized once, the same term across different elements is called once per element; and a nested
50x50 quantifier with a budget of 1,000 stops at `Unknown` with one fault.

## Open questions for the owner

- **The untyped element-read registration shape (decision 3).** Resolved 2026-10-17: an additional optional
  delegate parameter on `AddCollection<TItem>`. See the amendment under decision 3.
- **Diagnostic codes.** This ADR names new diagnostics (unknown collection name, invalid or shadowed
  variable name, `MaxQuantifierDepth` exceeded, undeclared source for an untyped element read reusing
  ADR-0006's existing code) but does not allocate `TRE` codes for the ones that are new; the next free code
  is `TRE0042`. The implementing ticket allocates them in declaration order, as ADR-0008's ticket did for its
  own guardrails, and this ADR is amended in place with the allocation once decided.
- **Variable-name shadowing rules.** The grammar fixes the variable as required and part of tree equality
  (decision 2), but this ADR does not spell out what counts as "shadowed" (reusing an outer variable name, a
  predicate name, or a reserved word) beyond ADR-0006 decision 3's precedent for an undeclared source name;
  the implementing ticket's diagnostic design should follow the same shape as that precedent unless the owner
  says otherwise.

## Consequences

- `CONTEXT.md`'s Operator/Connective vocabulary entries and ADR-0005's own connective list (decision 18)
  will need `FORALL`/`EXISTS` added once the operators actually exist in the engine; this ADR records the
  decision ahead of that code, as ADR-0008 and ADR-0009 did for their own features, and the implementing
  ticket updates CONTEXT.md's operator/connective list in the same change that adds the AST node, per the
  project's docs-with-code rule.
- `CONTEXT.md` and `docs/glossary.md` gain entries for **quantifier**, **collection binding** and **element
  context**, added in this change, alongside this ADR (ticket 22b-1's own acceptance criterion).
- `rule-tree.schema.json`, the DSL lexer/vocabulary/parser, `RuleNode`, the JSON and YAML tree readers,
  `RuleBuilder`, the evaluator, the analyzer, every rewrite and every printer need new code (tickets 22b-2
  through 22b-6); this ADR does not change any of them itself.
- `CompilerOptions` gains `MaxQuantifierDepth` and `EvaluationOptions` gains `MaxElementEvaluations`,
  alongside the existing resource bounds; their defaults (2 and 10,000) are fixed by decision 6 above.
- The diagnostic code catalog (`docs/strong-k3/specification/diagnostics.md`) and `DiagnosticCodes` gain new
  entries for the guardrails named above, numbered by the implementing ticket (open question above).
- Analysis gets weaker for a rule that uses a quantifier: fewer tautologies/contradictions are reported
  across a quantifier boundary, and equivalence/overlap checks become incomplete across differently-written
  but equivalent bodies (decision 8). This must be stated on the analyzer, equivalence and overlap pages when
  those pages are next touched, not left to be discovered.
- Evaluation cost now depends on data size for a rule with a quantifier, which it never did before
  (decision 6's two new bounds exist to keep this predictable, not to make it free).
