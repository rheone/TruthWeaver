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

Pass a `MermaidOptions` to `MermaidTreePrinter.Print` or `PrintMermaid` to change the output. The default options give the output shown above.

| Option | Values | Default |
| --- | --- | --- |
| `Direction` | `TopDown`, `LeftRight`, `BottomTop`, `RightLeft` | `TopDown` |
| `NodeShapes` | `true` gives an operator a hexagon, a term a rounded box and a constant a circle. `false` gives every node a rectangle. | `false` |
| `OperatorStyle` | `Word`, `Symbolic`, `CStyle` | `Word` |
| `ShowArgumentValues` | `true`, `false` | `true` |
| `TwoLineTermLabels` | `true` shows a term as a bold label and a plain argument line. `false` shows one line. | `false` |
| `Palette` | A `MermaidPalette`: `Light`, `ColorblindSafe`, `Monochrome`, `Dark` or your own. | `MermaidPalette.Light` |
| `NodeStyle` | A `Func<OutlineNode, NodeStyle?>` that picks `NodeStyle.Highlight`, `NodeStyle.Mute` or `NodeStyle.Custom(name)` for a node. | none |
| `CompactChainThreshold` | The operand count above which a flat `AND` or `OR` of only terms and constants is drawn in a group box. `null` turns the grouping off. | `null` |

```csharp
string diagram = rule.PrintMermaid(new MermaidOptions { Direction = MermaidDirection.LeftRight, NodeShapes = true });
```

### Two-line term labels

`TwoLineTermLabels` writes each term as a Mermaid markdown string. The label is bold. The argument values follow on a second, plain line. The option works together with `ShowArgumentValues`. When `ShowArgumentValues` is `false`, a term shows only its bold label. Operators and constants keep their one-line labels.

The output has no HTML. GitHub removes HTML and CSS from a diagram, so dimmed or smaller text is not available there. A renderer that you host yourself can apply it.

### Palettes

A `MermaidPalette` defines the `classDef` styles of the evaluation states (true, false, unknown, skipped) and of the highlight and mute classes. `PrintMermaid(decision, options)` uses the palette to color the nodes. Each property is a Mermaid style declaration, for example `fill:#d4edda,stroke:#28a745,color:#155724`. A style must not contain a line break or a semicolon. The printer removes them.

| Preset | Use | True | False | Unknown | Skipped |
| --- | --- | --- | --- | --- | --- |
| `Light` | The default, on a light page | green `#d4edda` | red `#f8d7da` | yellow `#fff3cd` | dashed gray `#e9ecef` |
| `ColorblindSafe` | Readers with a red-green color deficiency | blue `#cfe8f7`, stroke `#0072B2` | orange `#f9dcc6`, stroke `#D55E00` | yellow `#f7f2a8`, stroke `#8c8300` | dashed gray `#e9ecef` |
| `Monochrome` | A grayscale print | white, 4 px solid stroke | white, dashed stroke | white, dotted stroke | gray `#eee`, thin dashed stroke |
| `Dark` | A dark page | green `#14532d` | red `#7f1d1d` | brown `#713f12` | dashed slate `#1f2937` |

`ColorblindSafe` uses the Okabe-Ito colors, which stay distinct for the common forms of color blindness. In `Monochrome`, a stroke style separates the states, so a print without colors keeps them apart. Text on a fill has a contrast ratio of at least 4.5:1 (WCAG AA) in every preset. The one exception is the skipped state of `Light`, which has 3.95:1 because it keeps the colors of the earlier default output.

To make your own palette, copy a preset with `with`:

```csharp
MermaidPalette brand = MermaidPalette.Light with { True = "fill:#e0f2f1,stroke:#00796b,color:#004d40" };
string diagram = rule.PrintMermaid(decision, new MermaidOptions { Palette = brand });
```

The palette also defines a `Highlight` style and a `Mute` style for nodes that a caller wants to emphasize or to push back.

### Node styles

The `NodeStyle` callback runs for every node. It can mark a node without an evaluation. The callback receives the `OutlineNode` of the node and returns one of these values.

| Value | Result |
| --- | --- |
| `NodeStyle.Highlight` | The node gets the class `brHighlight`. The printer defines it from `Palette.Highlight`. |
| `NodeStyle.Mute` | The node gets the class `brMute`. The printer defines it from `Palette.Mute`. |
| `NodeStyle.Custom("name")` | The node gets the class `name`. The printer does not define it. Add a `classDef name ...` line to the output. A name holds letters, digits, underscores and hyphens. |
| `null` | The node keeps its evaluation color, or no class. |

The callback runs after the evaluation coloring. When it returns a style, that style replaces the evaluation color of the node. A node has one class. The printer writes the `classDef` line of the highlight or mute class only when a node uses it.

```csharp
string diagram = rule.PrintMermaid(
    decision,
    new MermaidOptions { NodeStyle = node => node.Label == "OR" ? NodeStyle.Highlight : null }
);
```

### Chain grouping

A flat `AND` or `OR` with many terms draws one edge for each operand. Set `CompactChainThreshold` to draw such a chain, with its operator node, inside a Mermaid `subgraph` box. The box title names the operator and the operand count. The grouping applies when the operator has more operands than the threshold, and every operand is a term or a constant. No operand is hidden, and the evaluation coloring still applies to each node. A chain with as many operands as the threshold or fewer, and a chain with an operator among its operands, keep the plain layout.

```csharp
string diagram = rule.PrintMermaid(new MermaidOptions { CompactChainThreshold = 4 });
```

### Call styles

Three call styles give the same output for the same settings. Each one builds a `MermaidOptions`.

```csharp
// Optional parameters
string a = rule.PrintMermaid(direction: MermaidDirection.LeftRight, nodeShapes: true);

// Options record
string b = rule.PrintMermaid(new MermaidOptions { Direction = MermaidDirection.LeftRight, NodeShapes = true });

// Fluent builder
string c = rule.PrintMermaid(
    new MermaidOptionsBuilder().WithDirection(MermaidDirection.LeftRight).WithNodeShapes().Build()
);
```

The optional parameters are `direction`, `nodeShapes`, `twoLineTermLabels`, `palette`, `nodeStyle` and `compactChainThreshold`. They follow `showArgumentValues` on `PrintMermaid` and `MermaidTreePrinter.Print`. `MermaidTreePrinter.Print` also takes `style` for the operator style.

The result of `PlainTextTreePrinter` needs no renderer. It holds the same information as an indented tree, and it suits a log line or a terminal.

Both printers include the rule-text argument values of each term in its label by default, for example `Has Crust (crust: "thin")`. Pass `showArgumentValues: false` to either `Print` overload, or to `PrintMermaid` and `PrintPlainText` on `CompiledRule<TContext>`, for labels that show only the structure. `CompiledRule<TContext>` also exposes both printers as `PrintMermaid()`, `PrintMermaid(decision)`, `PrintPlainText()` and `PrintPlainText(decision)`, so no separate `Outline()` call is needed.
