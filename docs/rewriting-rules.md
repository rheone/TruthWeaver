# Rewriting rules and rule equivalence

How to transform a compiled rule into an equivalent one, and how to check that two rules are equivalent. The meaning of each operator is in the [Strong Kleene (K3) reference](strong-k3/README.md). Back to the [README](../README.md).

## Rewriting rules

A compiled rule is immutable, so a rewrite never edits it. A rewrite returns a **new** `CompiledRule` over the same predicates. The new rule evaluates to the same value as the original for every `True`/`False`/`Unknown` assignment of its terms. Rewrites are opt-in. The compiler never applies one, so a rule always prints and round-trips as it was written.

Each rewrite is verified per operator. The K3 laws hold for connectives only. `Simplify()` handles the external operators (`COALESCE` and the inspections) with its own rules. See [Strong Kleene connectives and external operators](strong-k3/specification/semantics.md#strong-kleene-connectives-and-external-operators).

| Rewrite | Result |
| --- | --- |
| [`ExpandToPrimitives()`](#expand-to-primitives) | Only the primitive kernel. |
| [`ExpandToNand()`](#nand-only-and-nor-only) / [`ExpandToNor()`](#nand-only-and-nor-only) | Only `NAND` (or only `NOR`) as the logical operator. |
| [`CompressToDerived()`](#compress-to-derived-operators) | Primitive shapes written as derived operators. |
| [`Canonicalize()`](#canonical-form) | One deterministic representation. |
| [`Simplify()`](#simplify) | An equivalent, cheaper rule. |
| [`ToNnf()`](#normal-forms) | Negation normal form: `NOT` only above a term or an atom. |
| [`ToCnf()`](#normal-forms) / [`ToDnf()`](#normal-forms) | An `AND` of `OR`s (CNF), or an `OR` of `AND`s (DNF). |
| [`SimplifyWithSteps()`](#see-which-laws-simplify-applied) | The same rule, and the list of laws applied. |

### What a rewrite returns

A rewrite returns one of three shapes. The rule for which shape is: a rewrite that can make the rule larger returns a `CompilationResult<TContext>`, because it can be refused at its size cap. A rewrite that never makes the rule larger returns a `CompiledRule<TContext>`. A rewrite that also reports its steps returns a `SimplifyResult<TContext>`.

| Shape | Rewrites | How to read the result |
| --- | --- | --- |
| `CompilationResult<TContext>` | `ExpandToPrimitives()`, `ExpandToNand()`, `ExpandToNor()`, `ToNnf()`, `ToCnf()`, `ToDnf()` | Check `Succeeded`, then use `Rule` (or call `GetRuleOrThrow()`). A result over the cap has no rule and a `TRE0016` error. `ToNnf()`, `ToCnf()` and `ToDnf()` can also return a `TRE0031` warning with a rule. |
| `CompiledRule<TContext>` | `CompressToDerived()`, `Canonicalize()`, `Simplify()` | Use the rule. It is never larger than the original. |
| `SimplifyResult<TContext>` | `SimplifyWithSteps()` | Use `Rule` and `Steps`. |

To assert that any of these is sound in a test, use [`RewriteAssertions.AssertSound`](testing-assertions.md#assert-that-a-rewrite-is-sound). It has an overload for each of the first two shapes.

The Evaluation behavior section of each Operation page states how the rewrites treat that operator. For example, [NAND](strong-k3/derived/nand.md#evaluation-behavior) states what `CompressToDerived` and `ExpandToNand` do with it.

### Expand to primitives

`ExpandToPrimitives()` replaces every derived operator with its definition in the primitive kernel: `NOT`, `AND`, `OR`, `AtLeast`, `AtMost`, `Exactly` and `COALESCE`.

```csharp
CompiledRule<MyContext> rule = compiler.Compile("a IMPLIES ANY(b, c)").GetRuleOrThrow();
CompiledRule<MyContext> kernel = rule.ExpandToPrimitives().GetRuleOrThrow();

Console.WriteLine(kernel.CanonicalText);   // only primitive operators
Console.WriteLine(rule.CanonicalText);     // unchanged: (a IMPLIES ANY(b, c))
```

| Derived operator | Expands to |
| --- | --- |
| `a IMPLIES b` | `NOT a OR b` |
| `a XOR b` | `(a AND NOT b) OR (NOT a AND b)` |
| `a EQUIVALENT b` | `(a AND b) OR (NOT a AND NOT b)` |
| `a NAND b` / `a NOR b` | `NOT (a AND b)` / `NOT (a OR b)` |
| `PARITY(a, b, ...)` | `Exactly(1, ...) OR Exactly(3, ...) OR ...` (every odd count) |
| `ExactlyOne(...)` | `Exactly(1, ...)` |
| `ANY(...)` / `ALL(...)` / `NONE(...)` | `AtLeast(1, ...)` / `AtLeast(n, ...)` / `AtMost(0, ...)` |
| `BETWEEN(min, max, ...)` | `AtLeast(min, ...) AND AtMost(max, ...)` (a vacuous bound is dropped) |
| `GreaterThan(k, ...)` / `LessThan(k, ...)` | `AtLeast(k + 1, ...)` / `AtMost(k - 1, ...)` |
| `If(c, t, f)` | `(c AND t) OR (NOT c AND f) OR (t AND f)` |
| `IsTrue(x)` | `COALESCE(x, False)` |
| `IsFalse(x)` | `COALESCE(NOT x, False)` |
| `IsUnknown(x)` | `COALESCE(x, True) AND COALESCE(NOT x, True)` |
| `IsKnown(x)` | `COALESCE(x, False) OR COALESCE(NOT x, False)` |

Every row is checked against an independent truth-table oracle for all `True`/`False`/`Unknown` inputs. Classical shortcuts fail in Strong Kleene logic, so the `If` row keeps its third term. The [consensus term](strong-k3/specification/semantics.md#the-invalid-consensus-removal) explains why. Nothing stays unexpanded: the inspections are expressible with `COALESCE`, the primitive that can see `Unknown`.

For `ExpandToPrimitives()`, note two points:

- **Size.** An operator whose definition mentions an operand twice (`XOR`, `EQUIVALENT`, `If`, the inspections) repeats the text of that operand, so a deeply nested rule can grow a lot. The printed text of the expanded rule compiles back to the same rule only when it fits `CompilerOptions.MaxNodeCount` (512 by default). See [A large result may not recompile](#a-large-result-may-not-recompile). `CompilerOptions.MaxRewriteNodeCount` caps the result itself (see [Size cap](#size-cap)).
- **Faults.** A predicate that throws is `Unknown` plus a `Fault` in the expanded rule, exactly as in the original. Terms are still memoized by identity.

### Size cap

The three expanding rewrites (`ExpandToPrimitives()`, `ExpandToNand()` and `ExpandToNor()`) can produce a tree far larger than the rule they start from. Each returns a `CompilationResult<TContext>` and refuses to build a result larger than the rule's own `CompilerOptions.MaxRewriteNodeCount`. The default is **100,000** nodes, counted as a printed tree, so a sub-expression that is shared in memory but written twice counts twice. An over-cap rewrite never throws and is not built: `Succeeded` is `false`, `CompiledRule` is `null`, and one `TRE0016` error says which rewrite hit which cap. To allow a bigger result, raise `MaxRewriteNodeCount` when you compile the rule, or pass the cap for one call as the `maxNodeCount` argument. The normal-form rewrites (`ToNnf`, `ToCnf` and `ToDnf`) take the same argument after `NormalFormOptions`:

```csharp
CompilationResult<MyContext> expanded = rule.ExpandToNand(1_000_000);
if (!expanded.Succeeded)
{
    Console.WriteLine(expanded.FormatDiagnostics());   // TRE0016: ExpandToNand would produce more than ...
}
```

The code is in [Diagnostics](strong-k3/specification/diagnostics.md). This table shows how the size grows, so you can predict a refusal:

| Rewrite | Growth |
| --- | --- |
| `ExpandToPrimitives()` | Linear for most operators. `XOR`, `EQUIVALENT`, `If` and the inspections repeat an operand, so nesting them multiplies the printed size by about two per level (exponential in nesting depth). |
| `ExpandToNand()` / `ExpandToNor()` | The primitive size, times a small constant for the NAND or NOR rewrite, plus `C(n, k)` operand subsets for each `AtLeast(k, ...)` over `n` operands (`AtMost(k)` costs `C(n, k + 1)`, `Exactly(k)` both). Each subset is rebuilt as a NAND or NOR conjunction, so a wide threshold is refused quickly. A rewrite is also refused when its primitive form alone is over the cap. |

`CompressToDerived()`, `Canonicalize()` and `Simplify()` never make a rule larger and have no cap.

### A large result may not recompile

`MaxRewriteNodeCount` and `MaxNodeCount` are two separate caps. `MaxRewriteNodeCount` (100,000 by default) limits what a rewrite builds. `MaxNodeCount` (512 by default) limits what the compiler accepts from text, JSON, YAML or `RuleBuilder`. A rewrite result can be under the first cap and over the second. That result is a valid `CompiledRule`: it evaluates, and `CanonicalText` prints it. But its text compiles back to the same rule only when the compiler has a `MaxNodeCount` at least as large as the result. Otherwise `Compile` returns a `TRE0010` error and no rule.

The expanding rewrites and the normal forms (`ExpandToPrimitives()`, `ExpandToNand()`, `ExpandToNor()`, `ToNnf()`, `ToCnf()` and `ToDnf()`) can reach that case. `CompressToDerived()`, `Canonicalize()` and `Simplify()` never return a larger rule, so a rule that compiled still compiles after them. Neither cap changes the other. To store a large result as text and read it back, raise `MaxNodeCount` on the compiler that reads it.

### NAND-only and NOR-only

`ExpandToNand()` and `ExpandToNor()` rewrite a rule so that the only logical operator is one universal connective, `NAND` or `NOR`. They expand to the primitive kernel first, then rewrite it:

| Primitive | `ExpandToNand()` | `ExpandToNor()` |
| --- | --- | --- |
| `NOT a` | `a NAND a` | `a NOR a` |
| `a AND b` | `(a NAND b) NAND (a NAND b)` | `(a NOR a) NOR (b NOR b)` |
| `a OR b` | `(a NAND a) NAND (b NAND b)` | `(a NOR b) NOR (a NOR b)` |
| `AtLeast(k, ...)` | `OR` over every k-subset of the `AND` of that subset | same, with the target connective's `AND`/`OR` |
| `AtMost(k, ...)` | `NOT AtLeast(k + 1, ...)` | same |
| `Exactly(k, ...)` | `AtLeast(k) AND AtMost(k)` (a vacuous side is dropped) | same |

Longer `AND`/`OR` chains fold left, because both are associative in K3. The threshold rewrite is monotone, so it is exact for `Unknown` operands too. It has `C(n, k)` subsets, so very wide thresholds produce very large trees.

**`COALESCE` is the one boundary.** A circuit of `NAND` or `NOR` is monotone in the information order, and `COALESCE` and the inspections are not. See [NAND](strong-k3/derived/nand.md#evaluation-behavior) and [COALESCE](strong-k3/functions/coalesce.md#evaluation-behavior). So `COALESCE`, and the inspections (`IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown`) that expand to it, stay as `COALESCE` nodes with their operands rewritten. A rule without them is purely `NAND` (or `NOR`). The same guarantees hold as for every rewrite: a new rule, the original untouched, identical results and faults.

### Compress to derived operators

`CompressToDerived()` goes the other way. It recognizes primitive shapes and writes them as readable derived operators. The usual input is an expanded rule, but it accepts any rule. It does not promise to recover the exact rule that was expanded. It returns an equivalent rule that is **never larger** (counted in nodes) and that compresses to itself.

| Primitive shape | Becomes |
| --- | --- |
| `NOT a OR b` (either order) | `a IMPLIES b` |
| `NOT (a AND b)` / `NOT a OR NOT b` | `a NAND b` |
| `NOT (a OR b)` / `NOT a AND NOT b` | `a NOR b` |
| `(a AND NOT b) OR (NOT a AND b)` | `a XOR b` |
| `(a AND b) OR (NOT a AND NOT b)` | `a EQUIVALENT b` |
| `(c AND t) OR (NOT c AND f) OR (t AND f)` | `If(c, t, f)` |
| `Exactly(1, ...) OR Exactly(3, ...) OR ...` (every odd count, 3+ operands) | `PARITY(...)` |
| `AtLeast(1, ...)` / `AtLeast(n, ...)` / `AtMost(0, ...)` / `Exactly(1, ...)` | `ANY` / `ALL` / `NONE` / `ExactlyOne` |
| `NOT AtLeast(k, ...)` / `NOT AtMost(k, ...)` | `AtMost(k - 1, ...)` / `AtLeast(k + 1, ...)` |
| `AtLeast(m, ...) AND AtMost(M, ...)` over the same operands | `BETWEEN(m, M, ...)` |
| `COALESCE(NOT x, False)` | `IsFalse(x)` |
| `COALESCE(x, True) AND COALESCE(NOT x, True)` | `IsUnknown(x)` |
| `COALESCE(x, False) OR COALESCE(NOT x, False)` | `IsKnown(x)` |

Every row is an identity of Strong Kleene logic, checked against the truth-table oracle for every `True`/`False`/`Unknown` assignment. A `COALESCE` with a constant that matches none of the rows, such as `COALESCE(x, True)`, is already the shortest form and stays as written. `COALESCE(x, False)` also stays, because [IsTrue](strong-k3/functions/istrue.md#evaluation-behavior) is not recovered from it. Classical-only shapes are never matched: `a OR NOT a` stays as written. The operand order inside a matched `OR` or `AND` can differ from the original. That changes the order in which predicates run, never a result.

### Canonical form

`Canonicalize()` gives rules that are equivalent under a fixed set of Strong Kleene-sound rewrites one deterministic representation. You can then compare, cache and de-duplicate rules by their `CanonicalText`. It is deterministic and idempotent (`Canonicalize()` of a canonical rule is the same rule). It evaluates like the original for every `True`/`False`/`Unknown` assignment, and it is never larger than the original. To find rules that are not yet canonical, switch on the `NotCanonical` lint (`TRE0030`, see [lint rules](diagnostics.md#lint-rules-opt-in)).

The rewrites, in the order they are applied (bottom-up, repeated until stable):

1. **Aliases collapse.** `ANY(...)` and `AtLeast(1, ...)` become `OR`. `ALL(...)` and `AtLeast(n, ...)` become `AND`. `GreaterThan(k)` becomes `AtLeast(k + 1)`. `LessThan(k)` becomes `AtMost(k - 1)`. `ExactlyOne(...)` becomes `Exactly(1, ...)`.
2. **Double negation.** `NOT NOT x` becomes `x` (this holds in K3).
3. **Flatten.** `AND` inside `AND`, `OR` inside `OR` and `COALESCE` inside `COALESCE` are spliced into the parent. All three are associative.
4. **Sort.** The operands of the commutative operators (`AND`, `OR`, `XOR`, `EQUIVALENT`, `NAND`, `NOR`, `PARITY`, `ExactlyOne`, the threshold family, `BETWEEN`) are sorted by their canonical text, ordinally.
5. **Deduplicate.** Repeated operands of `AND` and `OR` are removed (`a AND a` is `a`, because idempotence holds in K3). Counting operators keep repeats, because they count.

`COALESCE`, `IMPLIES` and `If` keep their operand order because it carries meaning. **Nothing is folded and no complement law is used.** `a OR NOT a` is not `True` in Strong Kleene logic (it is `Unknown` when `a` is), so it stays as a two-operand `OR`. Constant folding and the other cost-reducing rewrites belong to `Simplify()`.

> [!IMPORTANT]
> The canonical rule has the same *value* as the original but not the same *evaluation order*. Reordering, flattening and removing duplicates can change which predicate runs first, which predicates run at all once a short-circuit applies, and so which faults are reported. Use a canonical rule as a comparison or storage key. Keep evaluating the rule as written if invocation order matters.

### Simplify

`Simplify()` replaces a rule with an equivalent, cheaper one. It starts from the [canonical form](#canonical-form) and then applies only rewrites that are identities of Strong Kleene logic, repeating until nothing changes. The result evaluates like the original for every `True`/`False`/`Unknown` assignment, is never larger (counted in nodes), and simplifying it again changes nothing.

| Rewrite | Example |
| --- | --- |
| Identity and annihilator constants | `a AND True` is `a`; `a AND False` is `False`; `a OR False` is `a`; `a OR True` is `True` |
| `Unknown` is kept | `a AND Unknown` stays; `True AND Unknown` is `Unknown`; `NOT Unknown` is `Unknown` |
| Constant folding | `NOT True` is `False`; any operator over constants folds to a constant |
| Idempotence, double negation, flattening | `a AND a` is `a`; `NOT NOT a` is `a`; `a AND (b AND a)` is `a AND b` |
| Absorption | `a AND (a OR b)` is `a`; `a OR (a AND b)` is `a` |
| De Morgan, only where it removes nodes | `NOT (NOT a AND NOT b)` is `a OR b`; `NOT a NAND NOT b` is `a OR b` |
| Negation through derived operators | `NOT a IMPLIES b` is `a OR b`; `NOT a XOR b` is `a EQUIVALENT b`; `NOT IsKnown(a)` is `IsUnknown(a)` |
| `COALESCE` | `COALESCE(Unknown, a)` is `a`; `COALESCE(a, True, b)` is `COALESCE(a, True)`; `COALESCE(IsKnown(a), b)` is `IsKnown(a)`; `COALESCE(IsTrue(a), False)` is `IsTrue(a)` |
| Inspections | `IsKnown(True)` is `True`; `IsUnknown(IsTrue(a))` is `False`; `IsTrue(NOT a)` is `IsFalse(a)` |
| `If` | `If(True, a, b)` is `a`; `If(c, a, a)` is `a` |
| Derived operator with a constant operand | `a IMPLIES False` is `NOT a`; `a XOR True` is `NOT a`; `a NAND False` is `True` (expanded one level, simplified, kept only if no larger) |
| Thresholds with `True`/`False` operands | `AtLeast(2, True, a, b)` is `a OR b`; `AtMost(0, True, a, b)` is `False`; `Exactly(2, True, True, a)` is `NOT a` |

"Never `Unknown`" operands (constants, the inspections, a `COALESCE` with such an operand, and operators over only those) also let `COALESCE` and the inspections be removed.

**Classical rules that deliberately do not apply.** Each of these is valid in two-valued logic and false in Strong Kleene logic, because it fails when `a` is `Unknown`. `Simplify()` never uses them, so the rule keeps its value:

- Excluded middle (`a OR NOT a` is `True`) and non-contradiction (`a AND NOT a` is `False`).
- `a IMPLIES a` and `a EQUIVALENT a` as `True`, and `a XOR a` as `False`.
- Complement absorption (`a AND (NOT a OR b)` as `a AND b`, `a OR (NOT a AND b)` as `a OR b`). With `a` and `b` set to `Unknown` and `False`, `Unknown AND (Unknown OR False)` is `Unknown`, but `Unknown AND False` is `False`.
- Choosing a branch of `If(c, t, f)` when `c` is `Unknown`. It yields a value only when both branches agree.

[Laws that fail](strong-k3/specification/semantics.md#laws-that-fail) lists each law with its counter-example. Only plain absorption (`a AND (a OR b)`) holds, because `AND` and `OR` form a lattice.

> [!IMPORTANT]
> Like `Canonicalize()`, simplification keeps the *value* but not the evaluation order or side effects. Operands can be reordered, merged or dropped. An annihilated `AND` never evaluates its other operands, so a predicate that the original would have invoked (and any fault it would have reported) may not run.

The rewrite does not use the dual-rail findings of the analyzer. The analyzer reports those as diagnostics (`StructuralTautology`, `StructuralContradiction`) for authors. Every simplification here is a local, structural rule that is easy to check.

### See which laws Simplify applied

`SimplifyWithSteps()` does the same work as `Simplify()` and also lists each change. It returns a `SimplifyResult<TContext>` with the simplified `Rule` and a list of `Steps`.

```csharp
CompiledRule<MyContext> rule = compiler.Compile("(a AND True) AND (a OR b)").GetRuleOrThrow();
SimplifyResult<MyContext> result = rule.SimplifyWithSteps();

foreach (RewriteStep step in result.Steps)
{
    Console.WriteLine($"{step.Law}: {step.Before} => {step.After}");
}

// result.Rule has the same canonical text as rule.Simplify().
```

Each `RewriteStep` has three members:

| Member | Meaning |
| --- | --- |
| `Law` | A `RewriteLaw`: `AliasCollapse`, `DoubleNegation`, `Flatten`, `Reorder`, `Idempotence`, `ConstantFold`, `Identity`, `Annihilator`, `Absorption`, `DeMorgan`, `NegationThroughDerived`, `Coalesce`, `Inspection`, `If`, `DerivedWithConstant` or `Threshold`. |
| `Before` | The canonical text of the changed subtree before the step. |
| `After` | The canonical text of the changed subtree after the step. |

- **Order.** The steps are in the order the rewrite applied them. The canonical-form laws come first: aliases, double negation, flattening, ordering and repeated operands. The simplification laws follow. The canonical-form laws run again between passes.
- **Subtree text.** A step shows the subtree as it stood when the rewrite reached it. An earlier step can already have changed its operands, so `Before` is not always a substring of the original rule text.
- **Reorder.** Sorting the operands of a commutative operator is a step. The value does not change, but the text does.
- **One step for an expansion.** A derived operator with a constant operand (`a XOR True`) is one `DerivedWithConstant` step. The expansion inside it is not listed.
- **Already simple.** A rule that `Simplify()` leaves as written returns an empty list.

**Relation to `RuleDiff`.** `RuleDiff.Compare(before, after)` compares two finished rules. It reports what differs and whether the meaning is preserved. It does not report why the rules differ. The step list reports why: each step names a law. To show a rewrite, print the steps. For the net difference, call `RuleDiff.Compare(rule, result.Rule)`. The diff and the step list always agree on whether the rule changed.

### Normal forms

`ToNnf()`, `ToCnf()` and `ToDnf()` rewrite a rule into negation, conjunctive or disjunctive normal form. Each returns a `CompilationResult<TContext>` like the other size-capped rewrites, and each result has the same value as the original for every `True`/`False`/`Unknown` assignment.

```csharp
CompiledRule<MyContext> rule = compiler.Compile("NOT (a AND (b OR c))").GetRuleOrThrow();

Console.WriteLine(rule.ToNnf().GetRuleOrThrow().CanonicalText);   // NOT a OR (NOT b AND NOT c)
Console.WriteLine(rule.ToDnf().GetRuleOrThrow().CanonicalText);   // NOT a OR (NOT b AND NOT c)
Console.WriteLine(rule.ToCnf().GetRuleOrThrow().CanonicalText);   // (NOT a OR NOT b) AND (NOT a OR NOT c)
```

| Form | Shape | How |
| --- | --- | --- |
| NNF | `NOT` only directly above a term or an atom | Strong Kleene De Morgan laws and double negation. Derived operators expand first. |
| CNF | An `AND` of `OR`s of literals and atoms | NNF, then `OR` is distributed over `AND`. |
| DNF | An `OR` of `AND`s of literals and atoms | NNF, then `AND` is distributed over `OR`. |

A literal is a term or the `NOT` of a term. The Strong Kleene connectives form a distributive lattice with De Morgan negation, so distribution is an identity. No classical complement law is used: `a AND NOT a` and `a OR NOT a` stay as written. A repeated literal in a clause and a repeated clause are dropped, because idempotence holds. Each form is idempotent: rewriting a rewritten rule changes nothing.

- **Atoms.** `COALESCE`, the inspections (`IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown`) and `If` are not information-monotone. Each is an atom: no `NOT` is pushed into it. The operands inside it are normalized.
- **Size.** A normal form can be larger than the rule. CNF and DNF can grow exponentially. Each form is capped by `CompilerOptions.MaxRewriteNodeCount` (see [Size cap](#size-cap)). A larger result is not built: `CompiledRule` is `null` and one `TRE0016` error is returned.
- **Value, not order.** Distribution and de-duplication can change which predicates run and which faults appear. The value never changes.

#### Thresholds and `ExpandThresholds`

A threshold (`AtLeast`, `AtMost`, `Exactly`, `GreaterThan` and `LessThan`) has one group for every subset of its operands. The cardinality operators that expand to thresholds (`PARITY`, `BETWEEN` and `ExactlyOne`) follow the same rule. The expansion has `C(n, k)` groups, so it is opt-in. Pass a `NormalFormOptions` value:

```csharp
CompiledRule<MyContext> rule = compiler.Compile("a AND AtLeast(2, b, c, d, e)").GetRuleOrThrow();

// Default: the threshold stays an atom and the result carries a TRE0031 warning.
CompilationResult<MyContext> kept = rule.ToDnf();
Console.WriteLine(kept.Diagnostics[0].Message);   // ToDnf kept AtLeast(2, b, c, d, e) as an atom. Expanding it would add about 19 nodes.

// Opt in: the threshold expands to AND/OR, within the node cap.
CompilationResult<MyContext> expanded = rule.ToDnf(new NormalFormOptions(ExpandThresholds: true));
```

| `ExpandThresholds` | Result |
| --- | --- |
| `false` (default) | A threshold stays an atom. Its operands are normalized. One `TRE0031` warning per distinct threshold gives the growth estimate in nodes. A `NOT` above it stays above it. |
| `true` | A threshold expands to `AND`, `OR` and `NOT` over its operands. A result over `MaxRewriteNodeCount` returns `TRE0016` and no rule. |

A threshold that is only an `OR` or an `AND` (`AtLeast(1, ...)`, `AtLeast(n, ...)`, `AtMost(0, ...)` or `AtMost(n - 1, ...)`) always expands, with no warning. So do `ANY`, `ALL` and `NONE`. The warning code is in [Diagnostics](strong-k3/specification/diagnostics.md).

## Rule equivalence

`RuleEquivalence.Compare(first, second)` answers the question "do these two rules always give the same result?" under Strong Kleene logic. It uses the same dual-rail BDD as the analyzer. The check is exact, not sampled, and it returns a `RuleEquivalenceResult`:

| `Outcome` | Meaning |
| --- | --- |
| `Equivalent` | Same value for every `True`/`False`/`Unknown` assignment of the terms. |
| `NotEquivalent` | Some assignment differs. `CounterExample` maps every distinct term in either rule (keyed by its printed form, such as `hasRole(role: "Y")`) to the value it takes. |
| `Undecided` | The rules have more distinct terms between them than `CompilerOptions.MaxAnalysisTerms` (default 20). `Reason` says so. Nothing is guessed. |

```csharp
RuleEquivalenceResult result = RuleEquivalence.Compare(
    compiler.Compile("NOT (a AND b)").GetRuleOrThrow(),
    compiler.Compile("NOT a OR NOT b").GetRuleOrThrow());
// result.Outcome == RuleEquivalenceOutcome.Equivalent

RuleEquivalenceResult excluded = RuleEquivalence.Compare(
    compiler.Compile("a OR NOT a").GetRuleOrThrow(),
    compiler.Compile("TRUE").GetRuleOrThrow());
// excluded.Outcome == NotEquivalent, excluded.CounterExample["a"] == TruthValue.Unknown
```

Limits to know:

- **Terms are opaque and independent.** Two terms are the same variable only when their predicate name and arguments match. The check cannot know that two different predicates are related, so `isManager` and `isDepartmentHead` are treated as unrelated.
- **The cap is on distinct terms across both rules**, not on size. A rule pair at exactly the cap is decided. Pass the `maxAnalysisTerms` argument to change it.
- **Value only.** The check does not compare evaluation order, short-circuiting or faults.
- **A counter-example is one witness**, not all of them. Terms that the difference does not depend on are reported as `False`.
- **Strong K3 is not two-valued logic.** `a OR NOT a` is not equivalent to `TRUE`, because it is `Unknown` when `a` is.

`RuleDiff.Compare` uses this check. `RuleDiffResult.PreservesMeaning` is `true` when the rules are equivalent (including when they are structurally identical), `false` when they are not, and `null` when the term cap makes the question undecidable (default 20). To use a larger cap, call `RuleEquivalence.Compare` directly, or pass the term cap as the optional third argument of `RuleDiff.Compare(before, after, maxAnalysisTerms)`.
