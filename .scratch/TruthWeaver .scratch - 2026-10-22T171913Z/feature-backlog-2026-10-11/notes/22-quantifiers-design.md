# 22: Quantifiers over collections: design note

**Ticket:** [22 spike](../issues/22-spike-quantifiers.md). **Feeds:** [22b build](../issues/22b-quantifiers-build.md) (on hold).
**Status:** done. Spike output of 2026-10-11, grilled 2026-10-16; the owner's answers to the open questions are recorded in [ADR-0010](../../../docs/adr/0010-forall-exists-quantifiers.md). This note is kept as the design background.
**Read with:** [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md), [ADR-0006](../../../docs/adr/0006-data-sources-for-expression-variables.md), `CONTEXT.md`.

## Recommendation

**Go, with a narrow first slice.** Add two connectives, `FORALL` and `EXISTS`, that range over a **collection binding** the host registers on the predicate registry. A collection binding is a name, a typed selector `TContext -> IEnumerable<TItem>?`, and a registry of element predicates. Body terms are typed predicates over an element context. In Strong Kleene terms the two operators are the generalized `AND` and `OR` over the elements, so they are information-monotone connectives, not external operators.

Leave quantifiers over data-source queries (`FORALL(line IN from("order", "$.lines[*]"), ...)`) for a later slice. That slice needs an ADR-0006 amendment, a new `IDataSource` member, and predicates whose subject is an argument. The catalog has none of these today.

Why it is worth doing: today a host writes one C# predicate per collection question (`allLinesInStock`, `anyLineOverLimit`). The rule author cannot combine element conditions. A quantifier lets the author write the element condition as a rule body, with the full operator set, and keeps the predicate catalog element-sized.

Why not more: the term set of a quantified rule is not fixed at compile time. The analyzer, the rewrites and every consumer that reads "the terms of a rule" (coverage, truth table, equivalence, counterfactual, compare) must treat a quantifier as an opaque atom. The first slice accepts that and does not try to reason inside the iteration.

## 1. Grammar and forms

### DSL

```text
FORALL(line IN lines, qtyAbove(n: 0) AND inStock)
EXISTS(line IN lines, IsTrue(backordered))
NOT EXISTS(line IN lines, FORALL(tag IN tags, restricted))   // nesting
```

- A function-call form with no precedence, like `PARITY`, `ANY` and `If` (ADR-0005 decision 8). It needs no parentheses beside infix operators, and the body is a full expression, so a ternary or an infix chain in the body needs no extra rule.
- Head: `<variable> IN <collection>`. `<variable>` is an identifier. `<collection>` is a registered collection name (slice 1). The later data-source slice adds `from("source", "query")` in the same position.
- `FORALL` and `EXISTS` are new reserved words (any case, canonical upper case), so a predicate cannot take them. `IN` is a **contextual** keyword: it is a keyword only in a quantifier head. Reserving it everywhere would break a host predicate named `In`.
- `ALL` and `ANY` are not reused. They are n-ary cardinality operators over written operands. Overloading them with a binder would give one word two arities and two parse paths.
- Input-only symbol aliases `∀` and `∃` can come later, by the same rule as the other symbol aliases (ADR-0005 decision 2). They are not in slice 1.

### JSON and YAML

```json
{ "op": "forall", "var": "line", "in": "lines", "operands": [ { "predicate": "inStock" } ] }
```

```yaml
op: exists
var: line
in: lines
operands:
  - predicate: backordered
```

This follows the existing `{ "op", "operands" }` pattern with extra fields, as `between` does with `min` and `max`. `operands` has exactly one item, the body (`MalformedTree` otherwise), so body diagnostics have the path `$.operands[0]...`, like every other node. `rule-tree.schema.json` gains a `quantifierOperatorNode`. The data-source slice would let `in` also take the `{ "from", "query" }` argument object.

### RuleBuilder

```csharp
RuleBuilder.ForAll("line", "lines", RuleBuilder.Predicate("inStock"));
RuleBuilder.Exists("line", "lines", body);
```

The builder renders to the same JSON tree, so the cross-form equality test covers it as for every other operator.

### Printers

The canonical printer writes `FORALL(line IN lines, body)`. The tree printers write `FORALL line IN lines` (word style), `∀ line ∈ lines` (symbolic), and keep the word in C style. The equation printers write `∀ line ∈ lines . (body)` in each dialect. The Outline has one node with the body as its only child.

### Interaction with definitions (ticket 11)

Definitions expand at compile time, before scope resolution. A definition used inside a body is checked against the element registry at the place of use. A definition used outside a body is checked against the outer registry. If the same definition is used in both places with terms that exist in only one registry, the diagnostic names both the use and the definition (ticket 11 already requires that span pair).

## 2. Element scope: typed binding or untyped nodes

| | **A. Typed collection binding (recommended)** | B. Untyped data-source nodes |
| --- | --- | --- |
| Range | A registered collection name | `from("source", "query")` that matches many nodes |
| How the body reads the element | Element predicates over `Element<TOuter, TItem>` (properties `Outer`, `Item`, `Index`) | `from("line", "$.qty")`, with `line` bound as a scoped data source for each element |
| Body predicate subject | The element, through a typed selector, as the catalog does now | Still `TContext`. The element reaches the predicate only as an argument value |
| Compile-time checking | Body terms are checked against the element registry. Argument schemas apply | Only the query syntax is checked (if a validator is declared) |
| Catalog reuse | Every catalog factory works unchanged with `TContext = Element<Order, OrderLine>` | Needs new argument-subject predicates (`Compare(left:, right:)`). The catalog has none |
| AOT | Generic instantiation is known at registration. No reflection | No change |
| Changes to Abstractions | None for slice 1 | New `IDataSource` member for enumeration, new result type |
| ADR impact | New ADR for the operator (amends the ADR-0005 operator list) | Also amends ADR-0006 decision 8 ("no iteration over repeated subtrees") |

Registration sketch for option A:

```csharp
PredicateRegistry<Order> registry = PredicateRegistry<Order>.CreateBuilder()
    .Add(ScalarPredicates.IsEqualTo<Order>("orderStatus", o => o.Status))
    .AddCollection(
        "lines",
        (Order o) => o.Lines,
        lines => lines
            .Add(NumericPredicates.IsGreaterThan<Element<Order, OrderLine>>("qtyAbove", e => e.Item.Quantity))
            .Add(ScalarPredicates.IsTrue<Element<Order, OrderLine>>("inStock", e => e.Item.InStock)))
    .Build();
```

- `AddCollection<TItem>(string name, Func<TContext, IEnumerable<TItem>?> selector, Action<PredicateRegistryBuilder<Element<TContext, TItem>>> elements)`. Nested collections register on the element builder, so the outer chain is typed (`Element<Element<Order, OrderLine>, Tag>`).
- Collection names are a separate namespace from predicate names, like data-source names. An unknown name is a new diagnostic with a "did you mean" over the collection names at that scope.
- A body term resolves only in the element registry of its quantifier. Outer data reaches an element predicate through `Element.Outer` in C#, not through rule text. This keeps one registry per scope and needs no lookup chain. (A lookup chain, outer predicates usable in the body, is a possible later addition. It would need scope-qualified term identity, see section 6.)
- The variable name is not needed for resolution in option A. It labels the trace (`line[2]`) and keeps the head stable for the data-source slice, where `from("line", ...)` refers to it. Open question Q4 asks whether to keep it.
- Implementation: the binding stores a closure created at registration (when `TItem` is known) that evaluates a body with an `Evaluator<Element<TContext, TItem>>`. The evaluator state that is per evaluation (faults, flat trace, memo, source answers, abort flag, budgets, cancellation token) moves to one shared state object that the nested evaluators share. `RuleNodeCompiler<TContext>` needs only schema lookup inside a body, which is not generic, so the compiler takes a scoped schema lookup.

## 3. Strong Kleene semantics

Kleene's strong quantifiers are the greatest lower bound and least upper bound in the truth order over the domain. Over a known, finite list of element values `v1..vn`:

| Operator | `True` when | `False` when | Otherwise |
| --- | --- | --- | --- |
| `FORALL` | every `vi` is `True` | some `vi` is `False` | `Unknown` |
| `EXISTS` | some `vi` is `True` | every `vi` is `False` | `Unknown` |

This is exactly `AND(v1..vn)` and `OR(v1..vn)`. For `n >= 1` it is also `AtLeast(n, ...)` and `AtLeast(1, ...)` under the interval semantics. Both operators are monotone in the information order, so they are **connectives**. The "no tautologies" result for connectives extends to them: `FORALL(x IN c, b)` is never a tautology when `b` contains a term.

| Case | Result | Fault | Rationale |
| --- | --- | --- | --- |
| Empty collection | `FORALL` = `True`, `EXISTS` = `False` | No | The identity elements of `AND` and `OR`. Standard in classical and K3 logic. SQL `ALL` over an empty subquery is also true. |
| Element body is `Unknown` | Counts as `Unknown` in the fold | Only if the body faulted | Same as an `Unknown` operand of `AND`/`OR`. |
| Selector returns `null` | `Unknown` | No | The collection is not known. Same reading as the catalog's default `NullBehavior.Unknown`, and fail-closed. |
| Selector throws | `Unknown` | Yes, one fault for the collection | ADR-0001: a failure becomes `Unknown` plus a fault. |
| Element limit exceeded | `Unknown` | Yes, one fault | See section 5. |

Consequence of `null` = `Unknown`: `FORALL(x IN c, True)` is **not** constant `True` (it is `Unknown` for a missing collection). No rewrite may fold it. The constant cases that stay sound are listed in section 7.

### Evaluation rules

- **Order and short-circuit.** Elements are evaluated in selector order. `FORALL` stops at the first `False` element and `EXISTS` at the first `True`, unless `EvaluationMode.Exhaustive` is set. Skipped elements are recorded as not evaluated, as for `AND`/`OR`. Stopping never changes the value (checked by the prototype).
- **Materialize once.** The selector is called at most once for each (binding, outer element) in one evaluation, and the result is materialized to a list. This extends the predicate-author contract to selectors: same answer for the same input within one evaluation.
- **Memoization is per element.** The memo key inside a body is (scope path, term identity), where the scope path is the chain of (binding, index) pairs, for example `lines[2]/tags[0]`. A term repeated within one element's body is called once. The same term in two elements is two calls, because the context differs. Two quantifiers over the same binding in one rule share the scope path, so they share memo entries for equal terms.
- **Fault budget is global.** `FaultBudget` counts every fault in the evaluation, wherever it happened. A per-quantifier budget was considered and rejected: it adds a knob and a second abort rule for a case the global budget already bounds. When the budget aborts inside a body, the remaining elements and the rest of the rule are not evaluated, as now.
- **Cancellation and timeout** are unchanged. They throw `OperationCanceledException`. They are not faults.

## 4. Trace shape

The trace tree keeps "mirrors the rule's shape" with one controlled exception: a quantifier node has one child per element, not one child per operand.

```text
FORALL(line IN lines) = False
  line[0] = True
    AND = True
      qtyAbove(n: 0) = True
      inStock = True
  line[1] = False
    AND = False
      qtyAbove(n: 0) = True
      inStock = False
  line[2] = (not evaluated)
```

- Each element child is labelled `<variable>[<index>]` and holds the body's trace tree. `TraceNode` already allows this (label, value, not-evaluated flag, children), so slice 1 needs no change to the public trace types.
- The flat log prefixes each body entry with the scope path (`line[1]: inStock`), so a reader can tell elements apart.
- An empty collection has no element children. A `null` collection is the quantifier node with value `Unknown` and the note `[no collection]`.
- **Consumers that pair trace nodes with rule nodes by position** (`NodeCoverage`, `RuleCoverage`, the HTML and DOT printers that take a `Decision`) must fold the element children onto the one body node. A body node's coverage is then "reached in at least one element" and its outcomes are the union over elements.
- Size: the trace grows with the element count. Slice 1 records every element (simple and exact). A cap with a summary node (`… 997 more: 990 True, 7 Unknown`) is a later option (Q8).

## 5. Limits and `MaxNodeCount`

- `MaxNodeCount` and `MaxDepth` stay compile-time and structural. A quantifier counts as one node plus its body, once. They do not bound evaluation cost, because the cost depends on data size.
- New runtime bound: `EvaluationOptions.MaxElementEvaluations`, the total number of body evaluations across every quantifier in one evaluation (nested quantifiers multiply: 50 x 50 = 2,550 body runs). When the count goes over the bound, the current quantifier is `Unknown` with one fault, and the evaluation aborts as the fault budget does. The proposed default is 10,000 (Q7). It is the only default in `EvaluationOptions` that is not unlimited, because the rule author, not the host, controls nesting, which is the same reason `CompilerOptions` has defaults.
- New compile-time bound: `CompilerOptions.MaxQuantifierDepth` (default 2) limits how deeply quantifiers nest, so the product of collection sizes stays predictable. Exceeding it is a compile error with its own code.
- `MaxRewriteNodeCount` is unaffected, because no rewrite expands a quantifier.

## 6. Analyzer, BDD and term identity

The term set is not fixed at compile time, so the dual-rail BDD cannot have one variable per element.

- **The quantifier is an opaque atom** in the outer analysis. It gets its own pair of BDD variables (is `True`, is `Unknown`), keyed by its canonical text, which includes the body. Two equal quantifiers in one rule are the same atom. This is sound: every K3 value of the atom is reachable for some collection.
- **The body is analyzed on its own**, in its scope, with its own term variables, so a tautology or contradiction inside a body is still reported (`TRE0012`/`TRE0013` with the body's path). The body does not report "the whole quantifier is constant", because a `null` collection makes it `Unknown`.
- `MaxAnalysisTerms` applies to each analysis separately (outer terms plus atoms, and each body).
- **Term identity.** A body term is identified by its predicate and arguments, as now, plus its scope (the binding path, such as `lines/tags`). The scope is kept on the internal `TermExpression`. The public `TermIdentity` is not changed in slice 1, because nothing outside the engine needs to compare terms across scopes yet. Consumers that list the terms of a rule (`DistinctTerms`, coverage, truth table, counterfactual, compare) list the outer terms and the quantifier atoms, and not the body terms. Each of those consumers documents this.
- Equivalence (`RuleEquivalence`) and compare (ticket 13) work on atoms: two rules with the same quantifier text compare correctly; two quantifiers with equivalent but differently written bodies are different atoms (sound, incomplete). A later slice can canonicalize bodies before keying the atom.

## 7. Rewrites

| Rewrite | Effect on a quantifier |
| --- | --- |
| `ExpandToPrimitives`, `ExpandToNand`, `ExpandToNor` | The quantifier stays. Its body is rewritten. A quantifier cannot expand to a fixed kernel formula, because `n` is known only at run time. It is a documented boundary, as `COALESCE` is for the NAND/NOR expansion. CONTEXT.md's "primitive kernel" entry must say so. |
| `ToNnf` | `NOT FORALL(x IN c, b)` becomes `EXISTS(x IN c, NOT b)` and the dual. This is K3-sound for every collection, including the empty case (`NOT True` = `False` = `EXISTS` over nothing) and the `null` case (`Unknown` both sides). Checked by the prototype. |
| `ToCnf`, `ToDnf` | The quantifier is an atom. Its body is put in NNF. |
| `Canonicalize`, `CompressToDerived` | The body is rewritten in its own scope. The quantifier is not reordered or merged. |
| `Simplify` | Sound folds: `EXISTS(x IN c, False)` is **not** `False` (a `null` collection gives `Unknown`), so no constant fold applies to either operator. Body-level simplification applies. `FORALL(x IN c, a) AND FORALL(x IN c, b)` = `FORALL(x IN c, a AND b)` is sound (same materialized list, `null` gives `Unknown` on both sides) but changes fault and trace shape. Leave it out of slice 1. |

Every rewrite still has to pass the exhaustive oracle tests. For quantifiers the oracle runs over collections of size 0 to 4 plus `null`, with every element assignment.

## 8. Data sources (ADR-0006)

ADR-0006 decision 8 puts "iteration over repeated subtrees" out of scope. A data-source range therefore needs an amendment. The design that fits the existing decisions:

- Range: `FORALL(line IN from("order", "$.lines[*]"), body)`. The range query is checked by the source's `IQueryValidator` at compile time, when one is declared.
- Element access: inside the body, `line` is a data-source name bound to each matched node, as if each node were a source from `ScopeAsync`. Queries stay absolute within that node (`from("line", "$.qty")`). No relative `@.` syntax is needed. The bound name must not shadow a declared source name.
- Interface: `IDataSource` needs an enumeration member (for example `ValueTask<DataEnumerationResult> EnumerateAsync(string query, CancellationToken)` returning one scoped source per match). It can be a default interface member that returns `SourceFailure`, so existing third-party sources keep compiling.
- Memo: the `(source, query)` memo of decision 11 is keyed by (scope path, source, query) inside a body.
- The blocker is not the engine: a body term is still a `TContext` predicate, so an element value can only reach it as an argument. The catalog's predicates take their subject from a selector. This slice needs argument-subject predicates (`IsGreaterThan(value: from("line", "$.qty"), than: 0)`), which is a catalog design question of its own.

Recommendation: a separate later ticket (22c) after slice 1 ships, with its own ADR-0006 amendment. It reuses the same `QuantifierExpression` node with a different range kind.

## 9. Cost

Slice 1 is a full operator slice plus a registry and evaluator change. It is about the size of k3-conformance tickets 14 and 16 together, plus the registry work.

| Area | Work |
| --- | --- |
| AST | `QuantifierExpression(Kind, Variable, Range, Body)`, a `NodeShape` entry, an `OperatorDefinitions` entry for each kind |
| Parsing | Lexer and vocabulary (`FORALL`, `EXISTS`, contextual `IN`), DSL parser head, `RuleNode`, JSON and YAML tree readers, schema |
| Compiler | Scoped schema lookup, collection and variable diagnostics (unknown collection, invalid or shadowed variable name, `MaxQuantifierDepth`) |
| Registry | `AddCollection<TItem>`, `Element<TOuter, TItem>`, typed binding with a body-evaluation closure |
| Evaluator | Shared per-evaluation state, scoped memo, materialization, element budget, trace children |
| Analyzer | Opaque atom, separate body analysis |
| Rewrites | Pass-through for about ten rewriters, De Morgan in `ToNnf` |
| Printers | Canonical, JSON, YAML, plain and Mermaid tree, DOT, HTML, the equation dialects, truth table (atom column) |
| Consumers | Coverage folding, `DistinctTerms`, `RuleDiff`, builder |
| Docs | New ADR, two `docs/strong-k3/` pages plus the category index and root navigation (`K3ReferenceSyncChecker`), rule text grammar, rule formats, glossary, CONTEXT.md (operator list, connective list, primitive kernel note, trace entry), `CHANGELOG.md` |

## 10. Risks

- **Trace and coverage break the "mirrors the rule" assumption.** Every consumer that pairs trace nodes with rule nodes by position needs the fold. Missing one gives wrong coverage silently. Mitigation: a test that runs every `Decision` consumer on a quantified rule.
- **Evaluation cost depends on data.** A rule that was cheap becomes expensive when a collection grows. Mitigation: `MaxElementEvaluations` and `MaxQuantifierDepth`, both on by default.
- **Analysis gets weaker.** Rules with quantifiers report fewer tautologies and contradictions, and equivalence checks become incomplete. This must be stated on the analyzer and equivalence pages, not discovered.
- **Fault location.** `Fault` holds a `TermIdentity` and an exception. A fault in a body does not say which element. Without a fix, a fault on `line[734]` reads like a fault on the term in general (Q9).
- **Selector stability.** A selector that returns a different sequence on a second call would break memoization. Materializing once per evaluation removes the risk for the engine, but the contract must be documented for hosts.
- **Two registries in one rule.** A predicate name that exists in both the outer and the element registry means different things in and out of a body. That is correct but can surprise an author. The Outline and the diagnostics must name the scope.
- **Scope of change in `TruthWeaver.Abstractions`.** Slice 1 avoids changes there (no `TermIdentity` or `TraceNode` change), unless Q9 picks `Fault.Scope`.

## 11. Minimal first slice (proposed scope for 22b)

1. A new ADR: `FORALL`/`EXISTS` as connectives, the grammar, the semantics table in section 3, the limits, and the analyzer and rewrite boundaries. It amends the ADR-0005 operator and connective lists in place, dated.
2. `PredicateRegistryBuilder.AddCollection<TItem>` and `Element<TOuter, TItem>`, with nesting.
3. `FORALL(var IN collection, body)` and `EXISTS(...)` in the DSL, JSON, YAML (with schema) and `RuleBuilder`, with the cross-form equality test.
4. Evaluator: the semantics and rules in section 3, the trace in section 4, `MaxElementEvaluations`, and `MaxQuantifierDepth` in the compiler.
5. Analyzer as an opaque atom with separate body analysis. Rewrites as pass-through, plus De Morgan in `ToNnf`. Every printer writes the node.
6. Coverage folding for element children.
7. Docs as listed in section 9.

Not in slice 1: data-source ranges (22c), counted quantifiers (`AtLeast(k, x IN c, b)`, which fall out of the interval semantics later), symbol aliases `∀`/`∃`, outer predicates inside a body, trace caps, quantifier folds in `Simplify`.

## 12. Prototype

A throwaway prototype of the evaluator semantics was written in the scratchpad, not in the repository (`scratchpad/quant/quant.cs`, a .NET file-based app). It models terms, `NOT`, `AND`, `FORALL` and `EXISTS` with the evaluator's own rules: short-circuit with not-evaluated children, a global fault budget with abort, per-element memoization, materialization and an element budget. Results (all passed, 364 exhaustive assignments):

- `FORALL` equals the `AND` fold and `EXISTS` the `OR` fold for collections of 0 to 5 elements, every `{True, False, Unknown}` assignment.
- For `n >= 1`, `FORALL` = `AtLeast(n, ...)` and `EXISTS` = `AtLeast(1, ...)` under the interval semantics.
- `NOT FORALL(x, b)` = `EXISTS(x, NOT b)` for every assignment, including the empty collection.
- Short-circuit and exhaustive mode give the same value.
- Monotone: refining one `Unknown` element to `True` or `False` never changes a definite result.
- Faults in two elements count against one global budget. With `FaultBudget = 2` the second element fault aborts, and a later outer term is not evaluated.
- A `null` collection is `Unknown` with no fault. A throwing selector is `Unknown` with one fault.
- The same term in three elements is three predicate calls; a repeat within one element is memoized.
- Nested 50 x 50 quantifiers with a budget of 1,000 stop at `Unknown` with one fault.

## Open questions for the owner

Each is written for the grilling: one at a time, recommended option first.

**Q1. Go or no-go, and which slice first?**
1. Go: typed collection bindings first (section 11), data-source ranges later as 22c. *(recommended)*
2. Go: data-source ranges first, with an ADR-0006 amendment and argument-subject predicates.
3. No-go: hosts keep writing collection predicates in C#.

**Q2. How does a body read the element?**
1. Typed element predicates over `Element<TOuter, TItem>`, from a registry registered with the binding. *(recommended)*
2. Untyped data-source nodes through `from("line", "...")` arguments.
3. Both in the first slice.

**Q3. DSL spelling.**
1. `FORALL(line IN lines, body)` and `EXISTS(...)`, function-call form, contextual `IN`. *(recommended)*
2. Reuse `ALL` and `ANY` with a binder head: `ALL(line IN lines, body)`.
3. Binder syntax `FORALL line IN lines : body` with a precedence rule.

**Q4. The bound variable name.**
1. Required. It labels the trace and is part of tree equality and canonical text. *(recommended)*
2. Required, but tree equality ignores it (alpha-equivalence).
3. Leave it out in slice 1: `FORALL(lines, body)`. Add it with data-source ranges.

**Q5. Empty collection.**
1. `FORALL` is `True`, `EXISTS` is `False` (the `AND`/`OR` identities). *(recommended)*
2. Both are `Unknown`.

**Q6. A `null` or unreadable collection.**
1. `null` is `Unknown` with no fault. A throwing selector is `Unknown` with a fault. *(recommended)*
2. `null` reads as an empty collection.
3. A per-binding option like the catalog's `NullBehavior`, default `Unknown`.

**Q7. The runtime element bound.**
1. `EvaluationOptions.MaxElementEvaluations`, default 10,000. Over the bound is `Unknown` plus a fault, and the evaluation aborts. *(recommended)*
2. The same option, default unlimited (`null`), like the other evaluation options.
3. Over the bound throws, like `Timeout`.

**Q8. Trace size.**
1. Record every element in slice 1. Add a cap later if needed. *(recommended)*
2. Cap element children at a configurable count, with a summary node for the rest.
3. Record only the elements that decided the result (the first `False` for `FORALL`) and every non-`True` element.

**Q9. Fault location inside a body.**
1. Add an optional, additive `Fault.Scope` (`lines[734]`) in `TruthWeaver.Abstractions`. *(recommended)*
2. Put the scope only in the trace and the fault message. Change no public type.

**Q10. Analyzer treatment.**
1. The quantifier is an opaque atom. The body is analyzed on its own. *(recommended)*
2. Skip analysis for any rule that has a quantifier, with an info diagnostic.

**Q11. Where to record the decision.**
1. A new ADR that amends the ADR-0005 operator and connective lists in place, dated. *(recommended)*
2. An amendment section inside ADR-0005 only.

## Acceptance checklist (ticket 22)

- [x] Design note at `.scratch/feature-backlog-2026-10-11/notes/22-quantifiers-design.md` covering semantics (section 3), API shape (sections 1, 2), cost (section 9), risks (section 10) and a recommended go (top, section 11).
- [x] Prototype code lives in the scratchpad only. No library code changed, nothing committed.
- [ ] A grilling session on the note is scheduled by the owner (this ticket does not decide).

Settle list coverage: grammar and forms (section 1); element scope, typed versus untyped (section 2); K3 semantics, empty collection, `Unknown` elements, fault budget per element (section 3); trace shape (section 4); data sources (section 8); analyzer and BDD (section 6); rewrites (section 7); `MaxNodeCount` (section 5); evaluator-only prototype (section 12); recommendation and minimal first slice (top, section 11).
