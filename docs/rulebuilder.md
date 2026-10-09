# RuleBuilder, outlines and diagrams

How to assemble a rule in code, describe a compiled rule as a tree of labels, and draw it. Back to the [README](../README.md).

## Building rules programmatically

`RuleBuilder` assembles a rule from C# calls instead of text. Every `RuleBuilder` method renders to the same flat JSON tree that `CompileJson` reads, and `Compile` passes that JSON to `CompileJson`. A rule from a builder therefore gets every diagnostic that a hand-written rule gets: an unknown predicate, a bad argument, an out-of-range threshold, a wrong operand count, a resource limit and a structural tautology or contradiction. No builder call skips the Validate and Analyze stages of the [compilation pipeline](architecture.md#compilation-pipeline). To convert a rule between the text formats, see [Rule formats](rule-formats.md#converting-between-dsl-json-and-yaml).

[Example 6](examples.md#6-the-same-rule-assembled-with-rulebuilder-instead-of-text) shows `RuleBuilder` from start to finish.

## RuleBuilder reference

Every operator has a static factory method on `TruthWeaver.Building.RuleBuilder`:

| Operator | Factory method |
| --- | --- |
| `True` / `False` / `Unknown` | `RuleBuilder.Constant(bool value)` / `RuleBuilder.Constant(TruthValue value)` |
| A term | `RuleBuilder.Predicate(string name)` / `RuleBuilder.Predicate(string name, params (string Name, object Value)[] arguments)` |
| `AND` | `RuleBuilder.And(params RuleBuilder[] operands)` |
| `OR` | `RuleBuilder.Or(params RuleBuilder[] operands)` |
| `NOT` | `RuleBuilder.Not(RuleBuilder operand)` |
| `XOR` | `RuleBuilder.Xor(RuleBuilder left, RuleBuilder right)` |
| `EQUIVALENT` | `RuleBuilder.Equivalent(RuleBuilder left, RuleBuilder right)` (`RuleBuilder.Xnor` is kept and forwards to it) |
| `IMPLIES` | `RuleBuilder.Implies(RuleBuilder antecedent, RuleBuilder consequent)` |
| `NAND` | `RuleBuilder.Nand(RuleBuilder left, RuleBuilder right)` |
| `NOR` | `RuleBuilder.Nor(RuleBuilder left, RuleBuilder right)` |
| `PARITY` | `RuleBuilder.Parity(params RuleBuilder[] operands)` |
| `ANY` / `ALL` / `NONE` | `RuleBuilder.Any(params RuleBuilder[] operands)` / `RuleBuilder.All(...)` / `RuleBuilder.None(...)` |
| `BETWEEN(min, max)` | `RuleBuilder.Between(int min, int max, params RuleBuilder[] operands)` (JSON/YAML: `{"op": "between", "min": 1, "max": 2, "operands": [...]}`) |
| `COALESCE` | `RuleBuilder.Coalesce(params RuleBuilder[] operands)` |
| `IsTrue` / `IsFalse` / `IsUnknown` / `IsKnown` | `RuleBuilder.IsTrue(RuleBuilder operand)` / `RuleBuilder.IsFalse(...)` / `RuleBuilder.IsUnknown(...)` / `RuleBuilder.IsKnown(...)` (JSON/YAML: `{"op": "isTrue", "operands": [x]}`, `isFalse`, `isUnknown`, `isKnown`) |
| `If` | `RuleBuilder.If(RuleBuilder condition, RuleBuilder whenTrue, RuleBuilder whenFalse)` (JSON/YAML: `{"op": "if", "operands": [condition, whenTrue, whenFalse]}`) |
| `ExactlyOne` | `RuleBuilder.ExactlyOne(params RuleBuilder[] operands)` |
| `AtLeast(k)` / `AtMost(k)` / `GreaterThan(k)` / `LessThan(k)` / `Exactly(k)` | `RuleBuilder.AtLeast(int k, params RuleBuilder[] operands)` (and the four siblings, same shape) |

### Operand lists of unknown length

When the number of operands is known only at run time, `And`, `Or`, `Parity`, `Any`, `All`, `None`, `ExactlyOne` and `Coalesce` also have an `IEnumerable<RuleBuilder>` overload. It folds a short list when you build the rule. This avoids a node that the compiler would reject with `MalformedTree`. Two or more items build the same node as the `params` overload:

| Operator | 0 items | 1 item `x` |
| -------- | ------- | ---------- |
| `And` / `All` | `Constant(True)` | `x` |
| `Or` / `Any` | `Constant(False)` | `x` |
| `Parity` / `ExactlyOne` | `Constant(False)` | `x` |
| `None` | `Constant(True)` | `Not(x)` |
| `Coalesce` | `Constant(Unknown)` | `x` |

An empty list that silently becomes a constant can hide a mistake. For example, an empty list of role checks under `And` is `True`. Check the count first when that matters.

The two forms give different results for the same operands. An array or an explicit argument list binds the `params` overload. A `List<RuleBuilder>` binds the `IEnumerable` overload. The fold is deliberate and does not change.

```csharp
RuleBuilder x = RuleBuilder.Predicate("isActive");

RuleBuilder.And(new[] { x });                 // params: one operand, MalformedTree at compile time
RuleBuilder.And(new List<RuleBuilder> { x }); // IEnumerable: folds to x
```

`Between`, `AtLeast`, `AtMost` and `Exactly` also have an `IEnumerable<RuleBuilder>` overload, but it never folds. A counted operator has no identity constant, so the sequence builds the same node as the `params` overload and goes through the same count validation. An unmeetable count such as `AtLeast(2, [])` gives the same compile diagnostic as with `params`. An empty list that you pass to a counted operator is probably a bug, so use that diagnostic as a prompt to check how you built the list. A `null` sequence throws `ArgumentNullException`, and the builder enumerates the sequence once. `GreaterThan` and `LessThan` have no enumerable overload.

### Compiling a builder

`RuleBuilder.Compile(compiler)` is a thin wrapper around `compiler.CompileJson(builder.ToJson())`. `ToJson()` alone is also useful, for example to log or store the tree that a builder assembled without compiling it at once.

### Joining compiled rules

`RuleBuilder.FromCompiled(rule)` returns a builder that holds the tree of a rule that is already compiled. Use it with any builder operator to join rules without a JSON round trip. Both rules must have the same context type.

```csharp
CompilationResult<Customer> joined = RuleBuilder
    .And(RuleBuilder.FromCompiled(baseRule), RuleBuilder.FromCompiled(tenantRule))
    .Compile(compiler);
```

The joined rule compiles against the registry of `compiler`. The compiler validates every term again, because the two source rules may come from other registries. A predicate that is missing from that registry, or that has a different schema there, gives the normal compile diagnostics. Data-source declarations and the compiler options (depth and node limits) apply to the whole joined tree. A term that appears in both rules is one term in the joined rule, so its predicate runs once for each evaluation.

## Outlining a compiled rule

Every predicate has a required `Label` and `Description` on its `PredicateSchema`. Every operator has the same, which `OperatorInfo.Describe` in `TruthWeaver.Ast` exposes. `CompiledRule<TContext>.Outline()` combines both into one recursive outline of a whole compiled rule. A rule-authoring UI or a generated "what does this rule mean" report can walk the outline without access to the closed-set AST types:

```csharp
CompiledRule<Customer> rule = compiler.Compile("lovesPineapple AND hasTopping(topping: \"greenOlives\")").CompiledRule!;

OutlineNode description = rule.Outline();
// description.Label       == "AND"
// description.Description == "True iff every operand is true. Short-circuits at the first False."
// description.Operands[0].Label == "Loves Pineapple"   (from LovesPineapple's PredicateSchema.Label)
// description.Operands[1].Label == "Has Topping"       (from hasTopping's PredicateSchema.Label)
```

A simple recursive print gives the shape of a "what does this rule mean" report:

```csharp
void Print(OutlineNode node, int depth = 0)
{
    Console.WriteLine($"{new string(' ', depth * 2)}{node.Label} — {node.Description}");
    foreach (OutlineNode operand in node.Operands)
    {
        Print(operand, depth + 1);
    }
}
```

## Rendering a rule as a diagram

`OutlineNode` also feeds [`MermaidTreePrinter`](../src/TruthWeaver/Printing/MermaidTreePrinter.cs) and [`PlainTextTreePrinter`](../src/TruthWeaver/Printing/PlainTextTreePrinter.cs). They render it as a Mermaid `flowchart` and as an indented text tree. Each can show the structure only, or add the result and short-circuit path of one evaluation:

```csharp
CompiledRule<Customer> rule = compiler.Compile("lovesPineapple AND hasTopping(topping: \"greenOlives\")").CompiledRule!;
OutlineNode description = rule.Outline();

// Structure only:
string mermaid = MermaidTreePrinter.Print(description);
string plainText = PlainTextTreePrinter.Print(description);

// Colored/annotated by one evaluation (Mermaid: green = contributed True, red = contributed False,
// gray = short-circuited; plain text: a "[true]"/"[false]"/"[skipped]" suffix per node):
Decision decision = await rule.EvaluateAsync(customer, serviceProvider, cancellationToken: ct);
string coloredMermaid = MermaidTreePrinter.Print(description, decision.TraceTree);
string annotatedText = PlainTextTreePrinter.Print(description, decision.TraceTree);
```

The result of `MermaidTreePrinter` is plain Mermaid text. Paste it into any Mermaid renderer, or give it to a UI that already embeds one. The diagram shows the structure of the rule and, when you pass a trace, why one evaluation gave its result. The output always has a synthetic `Start` node that points at the root, so the diagram shows where evaluation begins.

The result of `PlainTextTreePrinter` needs no renderer. It holds the same information as an indented tree, and it suits a log line or a terminal.

Both printers include the rule-text argument values of each term in its label by default, for example `Has Crust (crust: "thin")`. Pass `showArgumentValues: false` to either `Print` overload, or to `PrintMermaid` and `PrintPlainText` on `CompiledRule<TContext>`, for labels that show only the structure. `CompiledRule<TContext>` also exposes both printers as `PrintMermaid()`, `PrintMermaid(decision)`, `PrintPlainText()` and `PrintPlainText(decision)`, so no separate `Outline()` call is needed.
