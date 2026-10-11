# Examples

Eight examples, each adding one piece: a single predicate, combined predicates, named arguments, `XOR`, `EQUIVALENT`, `ExactlyOne` and the threshold family, the full worked example (a class-based predicate, a delegate predicate and a data-source variable) in all four forms, the same rule assembled with `RuleBuilder`, a match against one or several constants, and the worked rule in every printing format. A bonus section draws a denied decision and shows how to turn it into a sentence. Back to the [README](../README.md).

## Which printer to use

A compiled rule prints in several formats. Pick the printer for the job:

| Job | Printer | Shown in |
| --- | --- | --- |
| Save a rule as text that a person can edit and compile again. See [Rule text](rule-text.md). | `PrintRuleText` | [Section 8](#rule-text) |
| Store a rule, or send it to another tool. | `PrintJson` | [Section 8](#json) |
| Read the structure of a rule in a log or a terminal. | `PrintPlainText` | [Section 6](#6-the-same-rule-assembled-with-rulebuilder-instead-of-text) |
| Draw a rule in a document or a UI that renders diagrams. | `PrintMermaid` | [Section 6](#6-the-same-rule-assembled-with-rulebuilder-instead-of-text) |
| Show why a decision came out as it did. | `PrintPlainText` or `PrintMermaid`, with the decision | [The bonus section](#bonus-explaining-a-denied-decision) |
| Write a rule as a formula in a report, a wiki or a paper. See [Equations](equations.md). | `PrintEquation` | [Section 8](#equations) |
| Write a formula that stays short when the terms are long. See [Equations](equations.md). | `PrintEquationWithLegend` | [Section 8](#an-equation-with-a-legend) |

## 1. A single predicate

<!-- doctest:rule ex1 -->
```text
lovesPineapple
```

```csharp
PredicateRegistry<Customer> registry = PredicateRegistry<Customer>.CreateBuilder().Add<LovesPineapple>().Build();
RuleCompiler<Customer> compiler = new(registry);
if (!compiler.TryCompile("lovesPineapple", out CompiledRule<Customer>? rule, out EquatableArray<Diagnostic> diagnostics))
{
    return;
}

Decision decision = await rule.EvaluateAsync(customer, serviceProvider, cancellationToken: ct);
```

## 2. Combining predicates: `AND` / `OR` / `NOT`

<!-- doctest:rule ex2 -->
```text
lovesPineapple AND NOT isBanned
```

`NOT` binds tighter than `AND`, and `AND` binds tighter than `OR`. The rule parses as `lovesPineapple AND (NOT isBanned)` and needs no parentheses.

## 3. Named arguments

<!-- doctest:rule ex3 -->
```text
hasTopping(topping: "greenOlives")
```

```csharp
PredicateRegistry<Customer> registry = PredicateRegistry<Customer>.CreateBuilder()
    .Add(
        new PredicateSchema(
            "hasTopping",
            "Has Topping",
            "Does the order include the given topping?",
            [new PredicateArgumentSchema("topping", "The topping to check for.", LiteralKind.String)]),
        (customer, args, ct) =>
            ValueTask.FromResult(customer.Toppings.Contains(args.GetString("topping")) ? TruthValue.True : TruthValue.False))
    .Build();
```

The order of arguments in the source text never matters. A predicate with several arguments compiles to the same term identity in any order. Argument values are case-sensitive: `"greenOlives"` and `"Greenolives"` are different terms (see [Term identity](../CONTEXT.md#term-identity)).

`PredicateSchema` and `PredicateArgumentSchema` both require a `Description`. `PredicateSchema` also requires a `Label`. The `Label` is a short display name that is separate from the machine-facing `Name` in rule text (for example `Name: "hasTopping"` and `Label: "Has Topping"`). A rule-authoring UI or generated documentation therefore always has text to show for every predicate and argument. [Outlining a compiled rule](rulebuilder.md#outlining-a-compiled-rule) shows how the label pairs with the label and description of an operator.

A DSL string-literal argument supports four escape sequences:

- `\"` is a quote.
- `\\` is a backslash.
- `\n` is a newline.
- `\t` is a tab.

For example, `hasTopping(topping: "Chef's \"Special\"")` compiles to a string argument with the value `Chef's "Special"`. Printing the compiled rule back to DSL text gives the same text. Any other backslash sequence (for example `\p`) is an `InvalidEscapeSequence` diagnostic. The compilation fails, and the compiler does not guess the intended value. This rule is specific to the DSL text format. JSON and YAML use the string escaping of their own format (`System.Text.Json` and YamlDotNet). See [Converting between DSL, JSON, and YAML](rule-formats.md#converting-between-dsl-json-and-yaml).

A predicate can take more than one named argument. The registration shape is the same, with a longer `PredicateArgumentSchema` array and an `EvaluateAsync` that makes more than one `Get*` call:

<!-- doctest:rule ex3b -->
```text
hasToppingAmount(topping: "pepperoni", amount: "extra")
```

```csharp
PredicateRegistry<Customer> registry = PredicateRegistry<Customer>.CreateBuilder()
    .Add(
        new PredicateSchema(
            "hasToppingAmount",
            "Has Topping Amount",
            "Does the order include the given topping at the given amount?",
            [
                new PredicateArgumentSchema("topping", "The topping to check for.", LiteralKind.String),
                new PredicateArgumentSchema("amount", "The amount requested (e.g. \"regular\" or \"extra\").", LiteralKind.String),
            ]),
        (customer, args, ct) =>
            ValueTask.FromResult(
                customer.ToppingAmounts.TryGetValue(args.GetString("topping"), out string? amount)
                && amount == args.GetString("amount")
                    ? TruthValue.True
                    : TruthValue.False))
    .Build();
```

`hasToppingAmount(topping: "pepperoni", amount: "extra")` and `hasToppingAmount(amount: "extra", topping: "pepperoni")` compile to the same term identity. The argument order in the source text does not matter, for any number of arguments.

## 4. `XOR`, `EQUIVALENT`, `ExactlyOne`, and the threshold family

<!-- doctest:rule ex4a -->
```text
AtLeast(2, approvedByAlice, approvedByBob, approvedByCarol)
```

The rule means "at least two of these three approvals." The sibling operators read the same way: `AtMost(1, ...)`, `GreaterThan(1, ...)`, `LessThan(2, ...)` and `Exactly(2, ...)`. They compile to one shared `ThresholdExpression` node. They differ only in the comparison against the count of true operands. See [Operations](strong-k3/specification/operations.md) for every operator.

`ExactlyOne(a, b, c)` is the n-ary "exactly one of these" operator. `XOR` is binary only. A third operand is a compile error that points at both alternatives. Use `ExactlyOne` for "exactly one". Use `PARITY(a, b, c)` for n-ary parity (an odd number of operands are true, and the result is `Unknown` if any operand is `Unknown`). The two differ from three operands. When `a`, `b` and `c` are all true, `PARITY` is `True` and `ExactlyOne` is `False`.

`EQUIVALENT` (`IFF`, `↔`) is the counterpart of `XOR`. It means "these two must agree":

<!-- doctest:rule ex4b -->
```text
isPrimaryReviewer EQUIVALENT isBackupReviewer
```

The rule is `True` when both are reviewers or neither is a reviewer. It is `False` when exactly one is a reviewer.

`IMPLIES` (or `→`) is material implication. It means "if this holds, that must hold too":

<!-- doctest:rule ex4c -->
```text
isContractor IMPLIES hasSignedNda
```

The rule is the same as `NOT isContractor OR hasSignedNda`. A non-contractor passes regardless of the NDA. When `isContractor` is `True`, the result is the value of `hasSignedNda`. Like `XOR` and `EQUIVALENT`, `IMPLIES` is binary. Put it in parentheses next to `AND`, `OR` or another infix operator, for example `(isContractor IMPLIES hasSignedNda) AND isActive`. In JSON and YAML, it is `{"op": "implies", "operands": [antecedent, consequent]}`. See [Syntax](strong-k3/specification/syntax.md) for the spellings, precedence and operand counts.

## 5. The full worked example, in all four forms

The worked rule has three kinds of term:

- **A class-based predicate.** `isDineIn` and `isTakeout` are classes that implement `IPredicate<PizzaOrder>`. The engine resolves them from the service provider on every call.
- **A delegate predicate.** `hasTopping` and `totalAtMost` are lambdas that you pass to `PredicateRegistryBuilder.Add(schema, evaluate)`. `hasCrust` is a delegate predicate from a ready-made factory.
- **A variable reference.** `from("limits", "$.maxTotal")` is not a value. It names the data source `limits` and a JSONPath query. The engine asks the data source for the value on every evaluation (see [Data sources and variable references](data-sources.md)). The predicate `totalAtMost` takes the answer as its `limit` argument.

This is the rule text as authored. `CanonicalText` prints the same rule with the optional `hasCrust` arguments filled in from their defaults:

<!-- doctest:rule worked -->
```text
hasTopping(topping: "greenOlives") AND (hasCrust(crust: "thin") OR hasCrust(crust: "stuffed", ignoreCase: true) OR (isDineIn XOR isTakeout)) AND totalAtMost(limit: from("limits", "$.maxTotal"))
```

The same rule as JSON. The variable is an object with `from` and `query`:

<!-- doctest:json worked -->
```json
{
  "op": "and",
  "operands": [
    { "predicate": "hasTopping", "args": { "topping": "greenOlives" } },
    {
      "op": "or",
      "operands": [
        { "predicate": "hasCrust", "args": { "crust": "thin" } },
        { "predicate": "hasCrust", "args": { "crust": "stuffed", "ignoreCase": true } },
        {
          "op": "xor",
          "operands": [
            { "predicate": "isDineIn" },
            { "predicate": "isTakeout" }
          ]
        }
      ]
    },
    {
      "predicate": "totalAtMost",
      "args": { "limit": { "from": "limits", "query": "$.maxTotal" } }
    }
  ]
}
```

The same rule in YAML (`TruthWeaver.Yaml`):

<!-- doctest:yaml worked -->
```yaml
op: and
operands:
  - predicate: hasTopping
    args:
      topping: "greenOlives"
  - op: or
    operands:
      - predicate: hasCrust
        args:
          crust: "thin"
      - predicate: hasCrust
        args:
          crust: "stuffed"
          ignoreCase: true
      - op: xor
        operands:
          - predicate: isDineIn
          - predicate: isTakeout
  - predicate: totalAtMost
    args:
      limit: { from: limits, query: "$.maxTotal" }
```

[Example 6](#6-the-same-rule-assembled-with-rulebuilder-instead-of-text) shows the fourth form, `RuleBuilder`. All four forms compile to the same tree.

The data source `limits` answers the variable. The kitchen keeps its limits in a JSON document:

<!-- doctest:skip data document for the limits source, not a rule -->
```json
{ "maxTotal": 40 }
```

This code registers the predicates, declares the data source name, compiles the rule and evaluates it:

```csharp
(PredicateSchema hasCrustSchema, var hasCrustEvaluate) =
    StringPredicates.EqualsConfigurable<PizzaOrder>("hasCrust", order => order.Crust, "Has Crust", argumentName: "crust");

PredicateRegistry<PizzaOrder> registry = PredicateRegistry<PizzaOrder>.CreateBuilder()
    .Add<IsDineIn>()
    .Add<IsTakeout>()
    .Add(
        new PredicateSchema(
            "hasTopping",
            "Has Topping",
            "Does the order include the given topping?",
            [new PredicateArgumentSchema("topping", "The topping to check for.", LiteralKind.String)]),
        (order, args, ct) =>
            ValueTask.FromResult(order.Toppings.Contains(args.GetString("topping")) ? TruthValue.True : TruthValue.False))
    .Add(hasCrustSchema, hasCrustEvaluate)
    .Add(
        new PredicateSchema(
            "totalAtMost",
            "Total At Most",
            "Is the order total at most the given limit?",
            [new PredicateArgumentSchema("limit", "The highest total that is allowed.", LiteralKind.Int64)]),
        (order, args, ct) =>
            ValueTask.FromResult(order.Total <= args.GetInt64("limit") ? TruthValue.True : TruthValue.False))
    .Build();

// Declare the source names that rules may use. The JSONPath validator checks each query at compile time.
RuleCompiler<PizzaOrder> compiler = new(
    registry,
    new CompilerOptions(DataSources: new DataSourceDeclarations { ["limits"] = JsonQueryValidator.Instance }));
CompilationResult<PizzaOrder> result = compiler.Compile(ruleText);

if (!result.Succeeded)
{
    // Surface result.Diagnostics to whoever is authoring the rule.
    // The previously persisted rule (if any) stays active.
    return;
}

CompiledRule<PizzaOrder> rule = result.Rule;

// Supply a source for each name, usually per request.
DataSources sources = new() { ["limits"] = JsonDataSource.Parse("""{ "maxTotal": 40 }""") };
Decision decision = await rule.EvaluateAsync(order, serviceProvider, sources, cancellationToken: cancellationToken);

if (decision.IsSatisfied)
{
    // allowed
}
```

`IsDineIn` and `IsTakeout` are class-based predicates. The engine resolves them again from `serviceProvider` on every call. This is the correct shape for a predicate with a scoped dependency, such as a `DbContext`. `hasTopping` and `totalAtMost` are hand-written stateless lambdas. `hasCrust` comes from the ready-made `StringPredicates.EqualsConfigurable` factory (see [Predicate types](predicates.md)). It takes `crust` as the rule-text comparison target, plus the `ignoreCase` and `trim` arguments with defaults, so `hasCrust(crust: "thin")` compiles alone. All forms register against the same `PredicateRegistryBuilder<TContext>`. When a compilation fails, the previously persisted rule stays active.

For an order with the total 25, the rule is `True` when the document says `"maxTotal": 40` and `False` when it says `"maxTotal": 10`. The data source decides, and the rule text does not change. A YAML document (`YamlDataSource.Parse("maxTotal: 40")`) gives the same answer. If the host does not supply the `limits` source, `totalAtMost` is `Unknown`, the evaluation records a fault, and `decision.IsSatisfied` is `false`. [Failures](data-sources.md#failures) lists the other outcomes.

This code wires the registry into the DI container of a host instead of building it by hand:

```csharp
services.AddTruthWeaver<PizzaOrder>(builder => builder
    .Add<IsDineIn>()
    .Add<IsTakeout>());
```

## 6. The same rule, assembled with `RuleBuilder` instead of text

This builds the same tree as example 5, `hasTopping(topping: "greenOlives") AND (hasCrust(...) OR hasCrust(...) OR (isDineIn XOR isTakeout)) AND totalAtMost(limit: from("limits", "$.maxTotal"))`, without DSL, JSON or YAML text. `Arg.From` makes the variable reference. Use this approach when the shape of a rule comes from application logic (for example a dynamically assembled list of conditions) and not from an author who types it:

```csharp
using TruthWeaver.Building;

RuleBuilder rule = RuleBuilder.And(
    RuleBuilder.Predicate("hasTopping", ("topping", "greenOlives")),
    RuleBuilder.Or(
        RuleBuilder.Predicate("hasCrust", ("crust", "thin")),
        RuleBuilder.Predicate("hasCrust", ("crust", "stuffed"), ("ignoreCase", true)),
        RuleBuilder.Xor(RuleBuilder.Predicate("isDineIn"), RuleBuilder.Predicate("isTakeout"))),
    RuleBuilder.Predicate("totalAtMost", ("limit", Arg.From("limits", "$.maxTotal"))));

CompilationResult<PizzaOrder> result = rule.Compile(compiler);
```

This code renders the same rule as a Mermaid diagram:

```csharp
string mermaid = result.GetRuleOrThrow().PrintMermaid();
```

<!-- doctest:mermaid worked -->
```mermaid
flowchart TD
    Start(["Start"]) --> n0
    n0["AND"]
    n1["Has Topping (topping: #quot;greenOlives#quot;)"]
    n0 --> n1
    n2["OR"]
    n3["Has Crust (crust: #quot;thin#quot;, ignoreCase: false, trim: false)"]
    n2 --> n3
    n4["Has Crust (crust: #quot;stuffed#quot;, ignoreCase: true, trim: false)"]
    n2 --> n4
    n5["XOR"]
    n6["Is Dine In"]
    n5 --> n6
    n7["Is Takeout"]
    n5 --> n7
    n2 --> n5
    n0 --> n2
    n8["Total At Most (limit: from(#quot;limits#quot;, #quot;$.maxTotal#quot;))"]
    n0 --> n8
```

Set `TwoLineTermLabels` to show each term as a bold label with its argument values on a second line (see [Rendering a rule as a diagram](rulebuilder.md#rendering-a-rule-as-a-diagram)):

```csharp
string twoLine = result.GetRuleOrThrow().PrintMermaid(new MermaidOptions { TwoLineTermLabels = true });
```

<!-- doctest:mermaid worked two-line -->
```mermaid
flowchart TD
    Start(["Start"]) --> n0
    n0["AND"]
    n1["`**Has Topping**
topping: #quot;greenOlives#quot;`"]
    n0 --> n1
    n2["OR"]
    n3["`**Has Crust**
crust: #quot;thin#quot;, ignoreCase: false, trim: false`"]
    n2 --> n3
    n4["`**Has Crust**
crust: #quot;stuffed#quot;, ignoreCase: true, trim: false`"]
    n2 --> n4
    n5["XOR"]
    n6["`**Is Dine In**`"]
    n5 --> n6
    n7["`**Is Takeout**`"]
    n5 --> n7
    n2 --> n5
    n0 --> n2
    n8["`**Total At Most**
limit: from(#quot;limits#quot;, #quot;$.maxTotal#quot;)`"]
    n0 --> n8
```

Set `NodeShapes` to draw each kind of node in its own shape:

```csharp
string shapes = result.GetRuleOrThrow().PrintMermaid(new MermaidOptions { NodeShapes = true });
```

<!-- doctest:mermaid worked shapes -->
```mermaid
flowchart TD
    Start(["Start"]) --> n0
    n0{{"AND"}}
    n1("Has Topping (topping: #quot;greenOlives#quot;)")
    n0 --> n1
    n2{{"OR"}}
    n3("Has Crust (crust: #quot;thin#quot;, ignoreCase: false, trim: false)")
    n2 --> n3
    n4("Has Crust (crust: #quot;stuffed#quot;, ignoreCase: true, trim: false)")
    n2 --> n4
    n5{{"XOR"}}
    n6("Is Dine In")
    n5 --> n6
    n7("Is Takeout")
    n5 --> n7
    n2 --> n5
    n0 --> n2
    n8("Total At Most (limit: from(#quot;limits#quot;, #quot;$.maxTotal#quot;))")
    n0 --> n8
```

An operator (`AND`, `OR`, `XOR`) is a hexagon. A term is a box with rounded corners, and no operator uses that shape. A constant (`True`, `False` or `Unknown`) is a circle. The worked rule has no constants, so no circle appears. A variable is not a node. It is part of the label of the term that takes it, so `totalAtMost(limit: from(...))` is one rounded box.

This code renders the same rule as a text tree:

```csharp
string tree = result.GetRuleOrThrow().PrintPlainText();
```

<!-- doctest:tree worked -->
```text
AND
├─ Has Topping (topping: "greenOlives")
├─ OR
│  ├─ Has Crust (crust: "thin", ignoreCase: false, trim: false)
│  ├─ Has Crust (crust: "stuffed", ignoreCase: true, trim: false)
│  └─ XOR
│     ├─ Is Dine In
│     └─ Is Takeout
└─ Total At Most (limit: from("limits", "$.maxTotal"))
```

The second `hasCrust` term sets `ignoreCase: true` in the rule text. `StringPredicates.EqualsConfigurable` (see [Predicate types](predicates.md)) declares three rule-text arguments: `crust`, `ignoreCase` and `trim`. This term shows a rule that sets more than the one required argument that every other predicate in this example takes. Both outputs show the argument values of every term by default. The compiler fills in the `ignoreCase` and `trim` values of the first `hasCrust` term from the schema defaults, although its rule text does not name them. For this reason the two `hasCrust` terms are different in the output, which a predicate label alone does not show. Pass `showArgumentValues: false` to `PrintMermaid` or `PrintPlainText` to render labels that show only the structure (see [Rendering a rule as a diagram](rulebuilder.md#rendering-a-rule-as-a-diagram)).

`RuleBuilder` is not a fourth parser. Every builder method renders the same flat JSON tree shape as a hand-written JSON rule, and `Compile` gives that JSON to the same `CompileJson` that any other tool uses. A rule from a builder therefore gets every diagnostic that a hand-written rule gets. These include an unknown predicate, a bad argument, an out-of-range threshold, the operand count of `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND` and `NOR`, resource limits, and a structural tautology or contradiction. The builder does not bypass the Validate and Analyze stages of the [compilation pipeline](architecture.md#compilation-pipeline). See [RuleBuilder](rulebuilder.md) for the full API.

## 7. Matching against a constant, or any of several constants

This section shows two ways to write "has at least one topping of pepperoni, mushroom or green olives". The choice depends on whether a predicate already exists for each value.

**If a single-value predicate already exists** (for example `hasTopping(topping: "greenOlives")` from [example 3](#3-named-arguments)), use `OR` for each alternative. No new predicate is necessary:

<!-- doctest:rule ex7a -->
```text
hasTopping(topping: "pepperoni") OR hasTopping(topping: "mushroom") OR hasTopping(topping: "greenOlives")
```

Each call is a different term and a different memoization unit. The rule is clear for a few alternatives and becomes long as the set grows. The rule text contains the alternatives, so they are not passed as data.

**For a set of any size, write a predicate that takes an array argument** and checks the membership itself. The rule has one term and one predicate call, and the alternatives are rule-authored data, not repeated rule structure:

```csharp
public sealed class HasAnyTopping : IPredicate<Customer>
{
    public static PredicateSchema Schema =>
        new(
            "hasAnyTopping",
            "Has Any Topping",
            "Does the order include at least one of the given toppings?",
            [
                new PredicateArgumentSchema(
                    "toppings",
                    "The toppings to check for (any match).",
                    LiteralKind.StringArray
                ),
            ]);

    public ValueTask<TruthValue> EvaluateAsync(Customer customer, PredicateArguments args, CancellationToken ct)
    {
        IReadOnlyList<string> toppings = args.GetStringArray("toppings");
        bool result = toppings.Any(topping => customer.Toppings.Any(t => string.Equals(t, topping, StringComparison.Ordinal)));
        return ValueTask.FromResult(result ? TruthValue.True : TruthValue.False);
    }
}
```

A rule uses the predicate as follows:

<!-- doctest:rule ex7b -->
```text
hasAnyTopping(toppings: ["pepperoni", "mushroom", "greenOlives"])
```

**The predicate, not the engine, decides case sensitivity.** Term identity (which two term references are the same variable for memoization) is always exact and case-sensitive. `"pepperoni"` and `"Pepperoni"` are different arguments (see [Term identity](../CONTEXT.md#term-identity)). What the predicate does with the string that it reads through `GetString` or `GetStringArray` is ordinary C#. The example above uses `StringComparison.Ordinal`, which is case-sensitive. Change that one argument to `StringComparison.OrdinalIgnoreCase` and the same predicate is case-insensitive, with no other change. If you need both variants, write two predicates (for example `hasAnyTopping` and `hasAnyToppingIgnoreCase`). Do not thread a flag through the rule text. Then the name of each predicate shows its fixed behavior.

The same shape works for "equals one specific constant". Compare against a single value and do not check array membership (for example `args.GetGuid("id") == expectedId`, or the single-argument predicates `hasTopping` and `hasFlavor`). The schema decides whether the constants come from a `LiteralKind.String`, `Int64`, `Decimal`, `Boolean`, `DateTimeOffset` or `Guid` argument, scalar or array. The compiler validates and converts each kind in the same way. See the [Guid literal tests](../tests/TruthWeaver.Tests/GuidLiteralTests.cs) for a worked `Guid` example.

**"Matches a pattern", not "matches a fixed set"**, uses the same idea with `Regex.IsMatch` in place of set membership. The pattern is a rule-authored `string` argument, not a special literal kind:

```csharp
public sealed class HasToppingMatching : IPredicate<Customer>
{
    public static PredicateSchema Schema =>
        new(
            "hasToppingMatching",
            "Has Topping Matching",
            "Does the order include a topping whose code matches the given regular expression?",
            [new PredicateArgumentSchema("pattern", "The regular expression to match a topping code against.", LiteralKind.String)]);

    public ValueTask<TruthValue> EvaluateAsync(Customer customer, PredicateArguments args, CancellationToken ct)
    {
        Regex pattern = new(args.GetString("pattern"), RegexOptions.None, TimeSpan.FromMilliseconds(100));
        return ValueTask.FromResult(customer.Toppings.Any(pattern.IsMatch) ? TruthValue.True : TruthValue.False);
    }
}
```

A rule uses the predicate as `hasToppingMatching(pattern: "^EXTRA-.+$")`, which means "any topping code of the form `EXTRA-CHEESE`." Use `RegexOptions.IgnoreCase` in place of `RegexOptions.None` for a case-insensitive match, in the same way as the `StringComparison` choice above. The explicit timeout is more important here than in the other examples. A pattern is rule-authored text that can be very slow to match (catastrophic backtracking), by accident or on purpose. A predicate is the correct place to contain that risk. Without a timeout, the match can stall the evaluation of every rule that reaches this term.

`RegexPredicates` in [`TruthWeaver.Predicates`](../src/TruthWeaver.Predicates) already wraps this pattern with the same timeout, so a hand-written predicate is not always necessary.

## 8. The worked rule in every printing format

A compiled rule prints in several formats. Each format serves a different reader. This section prints the rule of [example 5](#5-the-full-worked-example-in-all-four-forms) with every printer. Each block is real output, and `dotnet test` checks it against the library.

The table at the top of this page shows which printer suits which job.

### Rule text

`PrintRuleText` returns the canonical text. It is the authored rule with every default argument filled in:

```csharp
string text = rule.PrintRuleText();
```

<!-- doctest:rule worked-printed -->
```text
hasTopping(topping: "greenOlives") AND (hasCrust(crust: "thin", ignoreCase: false, trim: false) OR hasCrust(crust: "stuffed", ignoreCase: true, trim: false) OR (isDineIn XOR isTakeout)) AND totalAtMost(limit: from("limits", "$.maxTotal"))
```

### JSON

`PrintJson` returns the same rule on one line. Use it to store a rule or to send it to another tool:

```csharp
string json = rule.PrintJson();
```

<!-- doctest:json worked -->
```json
{"op":"and","operands":[{"predicate":"hasTopping","args":{"topping":"greenOlives"}},{"op":"or","operands":[{"predicate":"hasCrust","args":{"crust":"thin","ignoreCase":false,"trim":false}},{"predicate":"hasCrust","args":{"crust":"stuffed","ignoreCase":true,"trim":false}},{"op":"xor","operands":[{"predicate":"isDineIn"},{"predicate":"isTakeout"}]}]},{"predicate":"totalAtMost","args":{"limit":{"from":"limits","query":"$.maxTotal"}}}]}
```

### Plain text and Mermaid

[Example 6](#6-the-same-rule-assembled-with-rulebuilder-instead-of-text) shows `PrintPlainText` (the text tree), `PrintMermaid` and `PrintMermaid` with `TwoLineTermLabels`. They draw the same rule.

### Equations

`PrintEquation` writes the rule on one line, the way a math text writes a formula. The `EquationOptions.Dialect` property selects the notation. [Equations](equations.md) describes the options.

The Unicode dialect is the default:

```csharp
string unicode = rule.PrintEquation();
```

<!-- doctest:equation worked -->
```text
hasTopping(topping: "greenOlives") ∧ (hasCrust(crust: "thin", ignoreCase: false, trim: false) ∨ hasCrust(crust: "stuffed", ignoreCase: true, trim: false) ∨ (isDineIn ⊕ isTakeout)) ∧ totalAtMost(limit: from("limits", "$.maxTotal"))
```

The LaTeX dialect writes LaTeX math. A term is a `\text` group, and the printer escapes the `$` of the query:

```csharp
string latex = rule.PrintEquation(new EquationOptions { Dialect = EquationDialect.LaTeX });
```

<!-- doctest:equation worked latex -->
```text
\text{hasTopping(topping: "greenOlives")} \land (\text{hasCrust(crust: "thin", ignoreCase: false, trim: false)} \lor \text{hasCrust(crust: "stuffed", ignoreCase: true, trim: false)} \lor (\text{isDineIn} \oplus \text{isTakeout})) \land \text{totalAtMost(limit: from("limits", "\$.maxTotal"))}
```

Set `LatexWrap` to `DoubleDollar` to get the `$$` envelope that Markdown tools read as display math:

```csharp
string display = rule.PrintEquation(
    new EquationOptions { Dialect = EquationDialect.LaTeX, LatexWrap = LatexWrapMode.DoubleDollar });
```

<!-- doctest:equation worked latex double-dollar -->
```text
$$\text{hasTopping(topping: "greenOlives")} \land (\text{hasCrust(crust: "thin", ignoreCase: false, trim: false)} \lor \text{hasCrust(crust: "stuffed", ignoreCase: true, trim: false)} \lor (\text{isDineIn} \oplus \text{isTakeout})) \land \text{totalAtMost(limit: from("limits", "\$.maxTotal"))}$$
```

A Markdown renderer that supports math draws the same text like this:

$$\text{hasTopping(topping: "greenOlives")} \land (\text{hasCrust(crust: "thin", ignoreCase: false, trim: false)} \lor \text{hasCrust(crust: "stuffed", ignoreCase: true, trim: false)} \lor (\text{isDineIn} \oplus \text{isTakeout})) \land \text{totalAtMost(limit: from("limits", "\$.maxTotal"))}$$

The AsciiMath dialect writes AsciiMath between backticks. A term is a quoted run, and the printer replaces each straight quote inside a term with the right double quotation mark:

```csharp
string ascii = rule.PrintEquation(new EquationOptions { Dialect = EquationDialect.AsciiMath });
```

<!-- doctest:equation worked ascii -->
```text
`"hasTopping(topping: ”greenOlives”)" ^^ ("hasCrust(crust: ”thin”, ignoreCase: false, trim: false)" vv "hasCrust(crust: ”stuffed”, ignoreCase: true, trim: false)" vv ("isDineIn" oplus "isTakeout")) ^^ "totalAtMost(limit: from(”limits”, ”$.maxTotal”))"`
```

### An equation with a legend

A rule with long terms is hard to read as an equation. `PrintEquationWithLegend` gives each distinct term a letter and returns the legend that maps the letters back to the terms:

```csharp
EquationWithLegend result = rule.PrintEquationWithLegend();
```

<!-- doctest:legend worked -->
```text
p ∧ (q ∨ r ∨ (s ⊕ t)) ∧ u
p = hasTopping(topping: "greenOlives")
q = hasCrust(crust: "thin", ignoreCase: false, trim: false)
r = hasCrust(crust: "stuffed", ignoreCase: true, trim: false)
s = isDineIn
t = isTakeout
u = totalAtMost(limit: from("limits", "$.maxTotal"))
```

The first line is `result.Equation`. The other lines are `result.LegendText`. [Operator styles and simple variables](#operator-styles-and-simple-variables) shows the same rule with `SimpleVariables` set.

### The same rule in each notation, side by side

The table prints two rules in each notation. The first rule is the worked rule. The second rule uses `XOR`, `EQUIVALENT` and a threshold, which have no infix symbol in some dialects:

<!-- doctest:rule review -->
```text
(isPrimaryReviewer XOR isBackupReviewer) AND (isContractor EQUIVALENT hasSignedNda) AND AtLeast(2, approvedByAlice, approvedByBob, approvedByCarol)
```

| Notation | Worked rule | `XOR`, `EQUIVALENT` and threshold rule |
| --- | --- | --- |
| Word (rule text) | `hasTopping(topping: "greenOlives") AND (hasCrust(crust: "thin", ignoreCase: false, trim: false) OR hasCrust(crust: "stuffed", ignoreCase: true, trim: false) OR (isDineIn XOR isTakeout)) AND totalAtMost(limit: from("limits", "$.maxTotal"))` | `(isPrimaryReviewer XOR isBackupReviewer) AND (isContractor EQUIVALENT hasSignedNda) AND AtLeast(2, approvedByAlice, approvedByBob, approvedByCarol)` |
| Symbolic | `hasTopping(topping: "greenOlives") ∧ (hasCrust(crust: "thin", ignoreCase: false, trim: false) ∨ hasCrust(crust: "stuffed", ignoreCase: true, trim: false) ∨ (isDineIn ⊕ isTakeout)) ∧ totalAtMost(limit: from("limits", "$.maxTotal"))` | `(isPrimaryReviewer ⊕ isBackupReviewer) ∧ (isContractor ↔ hasSignedNda) ∧ AtLeast(2, approvedByAlice, approvedByBob, approvedByCarol)` |
| C-style | `hasTopping(topping: "greenOlives") && (hasCrust(crust: "thin", ignoreCase: false, trim: false) \|\| hasCrust(crust: "stuffed", ignoreCase: true, trim: false) \|\| (isDineIn ^ isTakeout)) && totalAtMost(limit: from("limits", "$.maxTotal"))` | `(isPrimaryReviewer ^ isBackupReviewer) && (isContractor == hasSignedNda) && AtLeast(2, approvedByAlice, approvedByBob, approvedByCarol)` |
| Unicode (`PrintEquation`) | `hasTopping(topping: "greenOlives") ∧ (hasCrust(crust: "thin", ignoreCase: false, trim: false) ∨ hasCrust(crust: "stuffed", ignoreCase: true, trim: false) ∨ (isDineIn ⊕ isTakeout)) ∧ totalAtMost(limit: from("limits", "$.maxTotal"))` | `(isPrimaryReviewer ⊕ isBackupReviewer) ∧ (isContractor ↔ hasSignedNda) ∧ AtLeast(2, approvedByAlice, approvedByBob, approvedByCarol)` |
| LaTeX or MathJax | `\text{hasTopping(topping: "greenOlives")} \land (\text{hasCrust(crust: "thin", ignoreCase: false, trim: false)} \lor \text{hasCrust(crust: "stuffed", ignoreCase: true, trim: false)} \lor (\text{isDineIn} \oplus \text{isTakeout})) \land \text{totalAtMost(limit: from("limits", "\$.maxTotal"))}` | `(\text{isPrimaryReviewer} \oplus \text{isBackupReviewer}) \land (\text{isContractor} \leftrightarrow \text{hasSignedNda}) \land \text{AtLeast}(2, \text{approvedByAlice}, \text{approvedByBob}, \text{approvedByCarol})` |
| AsciiMath | `` `"hasTopping(topping: ”greenOlives”)" ^^ ("hasCrust(crust: ”thin”, ignoreCase: false, trim: false)" vv "hasCrust(crust: ”stuffed”, ignoreCase: true, trim: false)" vv ("isDineIn" oplus "isTakeout")) ^^ "totalAtMost(limit: from(”limits”, ”$.maxTotal”))"` `` | `` `("isPrimaryReviewer" oplus "isBackupReviewer") ^^ ("isContractor" <=> "hasSignedNda") ^^ "AtLeast"(2, "approvedByAlice", "approvedByBob", "approvedByCarol")` `` |

Notes on the table:

- The Word row is `PrintRuleText`. The Symbolic and C-style rows are the operator spellings of `OperatorStyle.Symbolic` and `OperatorStyle.CStyle`. These two styles change the operator labels of `PrintPlainText` and `PrintMermaid` (`PrintMermaid` takes the style in `MermaidOptions.OperatorStyle`). No printer writes them as one line, so the two rows replace only the operators of the Word row. The Symbolic row is the same text as the Unicode row. [Operator styles and simple variables](#operator-styles-and-simple-variables) lists every symbol and states the C-style caveat.
- The Unicode, LaTeX and AsciiMath rows are the output of `PrintEquation`. The LaTeX row is the output without a wrap. [Equations](equations.md#latex-wrap-modes) shows the `$$` and MathJax wraps.
- `AtLeast` has no infix symbol in any notation. Every notation writes it in function-call form.

When each notation reads best:

- **Word** reads best for the author of a rule, and in logs and diagnostics.
- **Symbolic and Unicode** read best in a report, a chat message or a comment, where the reader knows logic notation.
- **C-style** reads best for a developer who thinks in `&&` and `||`. Use it as a reading aid.
- **LaTeX and MathJax** read best in a paper, a wiki or a page that renders math. `MathJaxSafe` is the safe choice for GitHub Markdown.
- **AsciiMath** reads best in a tool that renders AsciiMath and where LaTeX is too heavy to type by hand.

### Operator styles and simple variables

`OperatorStyle` selects the operator symbols that `PrintPlainText` and `PrintMermaid` write. `PrintPlainText` on a compiled rule always uses `Word`. To choose another style, call `PlainTextTreePrinter.Print` with the outline of the rule, or pass `MermaidOptions.OperatorStyle` to `PrintMermaid`. `PrintRuleText` and `PrintEquation` do not take the style. The Word row of the notation table is `PrintRuleText`, and the Unicode row is the Symbolic symbols.

| Operator | `Word` | `Symbolic` | `CStyle` |
| --- | --- | --- | --- |
| `NOT` | `NOT` | `¬` | `!` |
| `AND` | `AND` | `∧` | `&&` |
| `OR` | `OR` | `∨` | `\|\|` |
| `XOR` | `XOR` | `⊕` | `^` |
| `EQUIVALENT` | `EQUIVALENT` | `↔` | `==` |
| `IMPLIES` | `IMPLIES` | `→` | `IMPLIES` |
| `NAND` | `NAND` | `↑` | `NAND` |
| `NOR` | `NOR` | `↓` | `NOR` |
| `If` | `If` | `If` | `?:` |
| `COALESCE` | `COALESCE` | `??` | `??` |
| `AtLeast`, `AtMost`, `Exactly`, `ExactlyOne` and the other cardinality operators | Function-call form | Function-call form | Function-call form |

An operator with no spelling in a style keeps its word. This is why `IMPLIES`, `NAND` and `NOR` read the same in `CStyle`, and why the cardinality operators read the same in every style.

The tree of the second rule, `review`, shows all three styles. It holds `XOR`, `EQUIVALENT` and a threshold operator:

```csharp
string word = PlainTextTreePrinter.Print(review.Outline(), OperatorStyle.Word);
string symbolic = PlainTextTreePrinter.Print(review.Outline(), OperatorStyle.Symbolic);
string cStyle = PlainTextTreePrinter.Print(review.Outline(), OperatorStyle.CStyle);
```

<!-- doctest:tree review -->
```text
AND
├─ XOR
│  ├─ Is Primary Reviewer
│  └─ Is Backup Reviewer
├─ EQUIVALENT
│  ├─ Is Contractor
│  └─ Has Signed Nda
└─ AtLeast(2)
   ├─ Approved By Alice
   ├─ Approved By Bob
   └─ Approved By Carol
```

<!-- doctest:tree review symbolic -->
```text
∧
├─ ⊕
│  ├─ Is Primary Reviewer
│  └─ Is Backup Reviewer
├─ ↔
│  ├─ Is Contractor
│  └─ Has Signed Nda
└─ AtLeast(2)
   ├─ Approved By Alice
   ├─ Approved By Bob
   └─ Approved By Carol
```

<!-- doctest:tree review cstyle -->
```text
&&
├─ ^
│  ├─ Is Primary Reviewer
│  └─ Is Backup Reviewer
├─ ==
│  ├─ Is Contractor
│  └─ Has Signed Nda
└─ AtLeast(2)
   ├─ Approved By Alice
   ├─ Approved By Bob
   └─ Approved By Carol
```

`CStyle` is a reading aid, not C semantics. In C, `==` is true when both sides are equal, and `null == null` is true. `EQUIVALENT` with two `Unknown` operands is `Unknown`, not `True`. Rule text accepts `&&`, `||` and `!`, but not `^` and `==`. Use the `CStyle` output to read a rule, and do not copy it back into a rule.

`SimpleVariables` is an `EquationOptions` switch. It replaces each term with a letter and prints no legend, so the equation stays short:

```csharp
string simple = rule.PrintEquation(new EquationOptions { SimpleVariables = true });
```

<!-- doctest:equation worked simple -->
```text
p ∧ (q ∨ r ∨ (s ⊕ t)) ∧ u
```

Use `PrintEquationWithLegend` when the reader needs the terms as well.

## Bonus: explaining a denied decision

`Decision`, `Fault` and `Trace` give the structured reason for a denial. The host turns that reason into a sentence for an end user or a support agent. [Humanizer](https://github.com/Humanizr/Humanizer) is a convenient pairing: predicate names are camelCase words, and fault counts are numbers:

The trace of the decision shows where the denial came from. This code draws it:

```csharp
string trace = rule.PrintMermaid(decision, new MermaidOptions { NodeShapes = true });
```

The host did not supply the `limits` data source of the [worked rule](#5-the-full-worked-example-in-all-four-forms). The diagram colors each node by its result. Green is `True`, yellow is `Unknown` and gray with a dashed border is a node that the evaluation skipped. The first `hasCrust` term is `True`, so the `OR` stops there and skips the rest of its operands. `totalAtMost` is `Unknown` because its variable has no source, and so the `AND` is `Unknown`. The decision is not satisfied.

<!-- doctest:mermaid worked shapes trace -->
```mermaid
flowchart TD
    Start(["Start"]) --> n0
    n0{{"AND"}}
    n1("Has Topping (topping: #quot;greenOlives#quot;)")
    n0 --> n1
    n2{{"OR"}}
    n3("Has Crust (crust: #quot;thin#quot;, ignoreCase: false, trim: false)")
    n2 --> n3
    n4("Has Crust (crust: #quot;stuffed#quot;, ignoreCase: true, trim: false)")
    n2 --> n4
    n5{{"XOR"}}
    n6("Is Dine In")
    n5 --> n6
    n7("Is Takeout")
    n5 --> n7
    n2 --> n5
    n0 --> n2
    n8("Total At Most (limit: from(#quot;limits#quot;, #quot;$.maxTotal#quot;))")
    n0 --> n8
    class n0 brUnknown
    class n1 brTrue
    class n2 brTrue
    class n3 brTrue
    class n4 brSkipped
    class n5 brSkipped
    class n6 brSkipped
    class n7 brSkipped
    class n8 brUnknown
    classDef brTrue fill:#d4edda,stroke:#28a745,color:#155724;
    classDef brFalse fill:#f8d7da,stroke:#dc3545,color:#721c24;
    classDef brUnknown fill:#fff3cd,stroke:#ffc107,color:#856404;
    classDef brSkipped fill:#e9ecef,stroke:#adb5bd,color:#6c757d,stroke-dasharray: 4 3;
```

```csharp
using Humanizer;

if (!decision.IsSatisfied && decision.Faults.Count > 0)
{
    string summary = decision.Faults.Count.ToQuantity("predicate");
    Console.WriteLine($"Couldn't reach a decision: {summary} failed to answer.");

    foreach (Fault fault in decision.Faults)
    {
        // "hasTopping" -> "has topping"
        Console.WriteLine($"  - {fault.Term.PredicateName.Humanize()}: {fault.Exception.Message}");
    }
}
```
