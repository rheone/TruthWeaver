# Rule formats

How to choose between rule text, JSON and YAML, and how to convert a rule from one to another. Back to the [README](../README.md).

## Choosing a rule format

Rule text (the DSL), JSON and YAML compile to the same tree through the same Parse, Validate, Analyze and Build pipeline (see [Compilation pipeline](architecture.md#compilation-pipeline)). No format is more real than another at evaluation time. The formats differ in who writes and reads them:

| | DSL string | JSON tree | YAML tree |
| --- | --- | --- | --- |
| **Canonical and persisted form?** | Yes. A `CompiledRule<TContext>` prints back to it. | No. It is an interchange format. | No. It is an interchange format. |
| **Best for** | A person who types or reads a rule directly: a database column, a code review, a log line. | A UI rule builder that generates or reads a tree without a parser. | The same as JSON, when the host tooling already prefers YAML (configuration files, GitOps). |
| **Package** | `TruthWeaver` | `TruthWeaver` | `TruthWeaver.Yaml` |
| **Compile with** | `compiler.Compile(text)` (same method as `compiler.CompileText(text)`) | `compiler.CompileJson(json)` | `compiler.CompileYaml(yaml)` |
| **Print with** | `rule.CanonicalText`, or `rule.PrintRuleText()` to keep [notes](#notes) | `rule.PrintJson()` | `rule.PrintYaml()` |
| **Round-trips losslessly?** | Yes. See [The conversion guarantee](#the-conversion-guarantee). | Yes. `parse(print(x))` is structurally equal to `x`, with the same notes. | Yes. The same guarantee holds. |
| **Nesting for `AND`/`OR`/`XOR`/`EQUIVALENT`/`IMPLIES`/`NAND`/`NOR`/`??`** | Infix with precedence (see [Precedence and grouping](strong-k3/specification/syntax.md#precedence-and-grouping)). `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND`, `NOR` and `??` need parentheses next to `AND`, `OR` or each other (see [The mixing rule](strong-k3/specification/syntax.md#the-mixing-rule)). | Explicit `{"op": "...", "operands": [...]}` nodes. There is no precedence to get wrong. | The same explicit `op` and `operands` shape as JSON. |
| **Notes on nodes** | Yes: a `//` or `/* */` comment. See [Notes](#notes). | Yes: a `note` property. | Yes: a `note` key. A YAML `#` comment is not a note: the compiler ignores it and a printed rule does not have it. |

The grammar of the DSL is in [Rule text](rule-text.md). The JSON shape is defined by the schema in [rule-tree.schema.json](../src/TruthWeaver/Json/rule-tree.schema.json). The shape of each operator in each format is in [Formats](strong-k3/specification/syntax.md#formats).

A predicate argument cannot be null. In JSON, `null` is an error. In YAML, an unquoted `null`, `~` or empty value is the same error, with the same diagnostic code and expectation. To pass the text "null", quote it: `role: "null"`.

You can also build a rule without writing text in any of these formats. See [RuleBuilder](rulebuilder.md).

`CanonicalText` is more than minimal. It puts parentheses around an operand when that operand is a different operator from the one it sits under, for example `a AND b OR c` prints as `(a AND b) OR c`. It does this even where precedence alone already makes the parse clear. The goal is a rule that a reader understands at a glance, without working out the precedence, and that also parses back to the same tree.

## Converting between DSL, JSON, and YAML

A compiled rule converts without loss to any of the three formats. Print the rule in one format and compile the result in the other. Nothing in the compiled tree depends on the format.

`RuleFormat` names the three formats: `Text`, `Json` and `Yaml`. The `TruthWeaver.Yaml` package, which reads and writes all three, has helpers that take it:

| Helper | What it does |
| --- | --- |
| `compiler.Convert(source, from, to)` | Compiles `source` as `from` and prints the rule as `to`. It returns a `RuleConversionResult`: the text, or no text and the diagnostics when the source does not compile. `TryConvert` is the `Try` form. |
| `compiler.CompileAs(source, format)` | The same result as `Compile`, `CompileJson` or `CompileYaml`. `TryCompileAs` is the `Try` form. |
| `rule.PrintAs(format)` | The same text as `PrintRuleText()`, `PrintJson()` or `PrintYaml()`. |

Each helper takes `includeNotes` (`true` by default) where it prints. A format value that is not defined throws `ArgumentOutOfRangeException`.

```csharp
RuleConversionResult result = compiler.Convert(yamlText, RuleFormat.Yaml, RuleFormat.Text);
if (result.Succeeded)
{
    Console.WriteLine(result.Text);                      // the rule text, with its notes
}
```

The same conversion with the format-specific methods:

```csharp
CompiledRule<PizzaOrder> rule = compiler.Compile(dslText).GetRuleOrThrow();

string json = rule.PrintJson();                       // DSL -> JSON
string yaml = rule.PrintYaml();                        // DSL -> YAML (TruthWeaver.Yaml)

CompiledRule<PizzaOrder> fromJson = compiler.CompileJson(json).GetRuleOrThrow();
string backToDsl = fromJson.CanonicalText;              // JSON -> DSL

// backToDsl == rule.CanonicalText always: parse(print(x)) is structurally
// equal to x in every direction, so converting formats never
// silently changes a rule's meaning.
```

`PrintJson(writer)` and `PrintYaml(writer)` write the same text to a `TextWriter`. See [Write a printer to a TextWriter](examples.md#write-a-printer-to-a-textwriter).

A rule-authoring UI can use this to offer "export as JSON or YAML" or "paste JSON, get back DSL to review". The UI needs a parser only for the format that the user is editing at that moment.

The conversion keeps the [notes](#notes) of the rule. `PrintRuleText()`, `PrintJson()` and `PrintYaml()` write them. `CanonicalText` does not.

### Constants

A constant is `true`, `false` or `unknown` in any letter case, or the letter `T`, `F` or `U` in either case, the same letters as the rule text. In JSON a `const` is a boolean or a string (`{"const": "U"}`); in YAML it is a scalar (`const: U`). The words and the letters give the same tree. The JSON and YAML writers keep writing `true`, `false` and `"unknown"`.

### The conversion guarantee

For every rule that compiles, a conversion in any of the six directions (text to JSON, text to YAML, JSON to text, JSON to YAML, YAML to text and YAML to JSON) gives a document that compiles to a tree equal to the tree of the source, with the same notes. A property test checks this for generated rules that use every operator, every literal kind, variable references (`from("source", "query")`) and notes, over many seeds.

The guarantee is about the compiled rule, not the source text. A conversion does not keep these:

- **The layout and spelling of the source.** Whitespace, delimiter choice, operator aliases and symbols, letter case and the spelling of a constant (`True`, `T`, `true`) are not in the tree. The printer writes its own spelling.
- **YAML `#` comments.** They are not notes. Use a `note` key to keep a comment.
- **Note text outside the normal form.** A note is normalized when it is read (see [Notes](#notes)), so trailing spaces and blank space around it are not kept.
- **Notes, after a rewrite.** A rewrite such as `Simplify` returns a rule with no notes, so a rewritten rule converts without them. `CanonicalText`, and a print with `includeNotes: false`, also leave the notes out.

## Definitions and parameters

A JSON or YAML document may name a reusable sub-expression once and use it, optionally with arguments, as many times
as the rule needs. This is the same mechanism [rule text's `let`](rule-text.md#definitions-and-parameters) gives a
DSL author, and it produces the identical compiled tree for the same definitions and uses.

A `definitions` object (JSON) or mapping (YAML), alongside the document's own root node, declares zero or more named
definitions, keyed by name. Each entry is `{"parameters": ["p1", "p2"], "body": <tree>}`; `parameters` is omitted or
empty for a parameterless definition. A use is a node of its own, wherever a tree node is expected (including nested
inside another node's `operands`): `{"op": "use", "name": "atLeastAge", "operands": [<arg1>, ...]}`, with `operands`
omitted or empty for a parameterless use. Inside a definition's own body, a bare `{"predicate": "p1"}` with no `args`
stands for the parameter `p1`, the same disambiguation rule text makes for a bare identifier; giving it `args` makes
it an ordinary term instead, shadowing the parameter.

This is the JSON form of rule text's own example:

<!-- doctest:rule jsondefinitions -->
```text
(approvedByAlice AND approvedByBob) AND NOT approvedByCarol
```

<!-- doctest:json jsondefinitions -->
```json
{
  "definitions": {
    "allApprovals": {
      "parameters": ["reviewer1", "reviewer2"],
      "body": {
        "op": "and",
        "operands": [{ "predicate": "reviewer1" }, { "predicate": "reviewer2" }]
      }
    }
  },
  "op": "and",
  "operands": [
    {
      "op": "use",
      "name": "allApprovals",
      "operands": [{ "predicate": "approvedByAlice" }, { "predicate": "approvedByBob" }]
    },
    { "op": "not", "operands": [{ "predicate": "approvedByCarol" }] }
  ]
}
```

<!-- doctest:yaml jsondefinitions -->
```yaml
definitions:
  allApprovals:
    parameters: [reviewer1, reviewer2]
    body:
      op: and
      operands:
        - predicate: reviewer1
        - predicate: reviewer2
op: and
operands:
  - op: use
    name: allApprovals
    operands:
      - predicate: approvedByAlice
      - predicate: approvedByBob
  - op: not
    operands:
      - predicate: approvedByCarol
```

`definitions` is valid only alongside the document's own root node, not on a node nested inside `operands` (a
`definitions` key there is an unknown-key error, the same as any other key a node does not accept). `use` has no such
restriction. `rule-tree.schema.json` accepts `definitions` structurally on any node, consistent with the rest of the
schema's own note that it describes JSON shape, not every compile-time rule; `RuleCompiler` is still the judge of
where a `definitions` section is actually honored.

Every guardrail diagnostic rule text's own section lists (a cycle, a name collision, a chain deeper than
`CompilerOptions.MaxDefinitionDepth`, a wrong argument count, an unknown definition, and the opt-in unused-definition
lint) applies identically to JSON and YAML, with the same diagnostic codes
([Diagnostics](strong-k3/specification/diagnostics.md)). A diagnostic's span still always points at the use (or, for
the unused-definition lint, the declaration); for YAML it is the real span of the `use` node's `name` value or the
`definitions` entry's own key, because YamlDotNet keeps every node's position. For JSON, which keeps no positions, it
is resolved the same way every other JSON diagnostic is: from the diagnostic's path (`$.operands[1].name` for a use,
`$.definitions.allApprovals` for a definition) back to a span in the original text.

## Quantifiers

`FORALL` and `EXISTS` follow the same `{"op": "...", "operands": [...]}` pattern as every other operator node, with
two extra fields at the node's own level: `var`, the bound variable name, and `in`, the collection name. `operands`
holds exactly one item, the body (a `MalformedTree` error otherwise), so body diagnostics get the path
`$.operands[0]...` like every other node. `rule-tree.schema.json` has a `quantifierOperatorNode` definition for this
shape. The meaning of `var`/`in`/`body`, and the rule-text spelling, is in
[Rule text's Quantifiers section](rule-text.md#quantifiers).

This is the JSON and YAML form of rule text's own example:

<!-- doctest:rule forall -->
```text
FORALL(line IN lines, inStock)
```

<!-- doctest:json forall -->
```json
{ "op": "forall", "var": "line", "in": "lines", "operands": [{ "predicate": "inStock" }] }
```

<!-- doctest:yaml forall -->
```yaml
op: forall
var: line
in: lines
operands:
  - predicate: inStock
```

## Notes

A note is free text on one node of a rule, for the people who read the rule. A note does not change what the rule means. A rule with notes compiles to the same tree, the same `CanonicalText` and the same diagnostics as the rule without them, and it evaluates the same way.

Each format writes a note in its own way:

| Format | How to write a note |
| --- | --- |
| Rule text | A comment: `// note` to the end of the line, or `/* note */`. A block comment can span lines. Block comments do not nest. |
| JSON | A `"note"` string property on the node. Any node can have one. |
| YAML | A `note` string key on the node. |
| `RuleBuilder` | `builder.WithNote("note")`. See [RuleBuilder](rulebuilder.md#notes). |

In rule text, a comment is the note of one node. The parser uses the first rule in this list that applies:

1. A comment on the same line as the token before it is the note of the smallest node that ends at that token: `b // note`.
2. Otherwise, the comment is the note of the smallest node that starts at the token after it: `/* note */ b`, or `// note` on the line above `b`.
3. Otherwise, the comment is the note of the smallest node that ends at the token before it.
4. Otherwise, the comment is the note of the smallest node that contains it, such as a call with a comment next to its bound.

A group is part of the node in it. Thus `/* note */ (a AND b)` is the note of the `AND` node, and `/* note */ a AND b` is the note of `a`. Two or more comments on one node join, in source order, with a line feed. An unterminated `/*` is a `SyntaxError`.

All the formats normalize a note in the same way. Each line ending becomes a line feed. Each line loses its trailing spaces. The note loses the blank space at its start and end. A note that is then empty is no note. A `//` comment also loses one space after the slashes.

This rule has a note on the root and a note on the `NOT` node:

<!-- doctest:rule noted -->
```text
// Dine-in orders only
(isDineIn AND /* banned customers cannot order */ NOT isBanned)
```

`PrintRuleText()` writes each note as a comment in front of its node. A noted `AND` or `OR` gets parentheses, so that its comment attaches to it and not to its first operand:

<!-- doctest:ruletext noted -->
```text
/* Dine-in orders only */ (isDineIn AND /* banned customers cannot order */ NOT isBanned)
```

When a note contains `*/`, the printer writes it as `//` lines, one for each line of the note. The same rule as JSON and as YAML:

<!-- doctest:json noted -->
```json
{
  "op": "and",
  "note": "Dine-in orders only",
  "operands": [
    { "predicate": "isDineIn" },
    {
      "op": "not",
      "note": "banned customers cannot order",
      "operands": [{ "predicate": "isBanned" }]
    }
  ]
}
```

<!-- doctest:yaml noted -->
```yaml
op: and
note: Dine-in orders only
operands:
  - predicate: isDineIn
  - op: not
    note: banned customers cannot order
    operands:
      - predicate: isBanned
```

`CompiledRule.Notes` holds the notes of a compiled rule. The key is the path of the node in the tree, in the same form that JSON and YAML diagnostics use: `$` is the root and `$.operands[1]` is its second operand. For the rule above, `Notes` is `{ "$": "Dine-in orders only", "$.operands[1]": "banned customers cannot order" }`.

The printers write the notes by default, so a conversion between the formats keeps them. To print the tree alone, for example to compare two rules, use `PrintRuleText(RuleTextOptions.Default with { IncludeNotes = false })`, `PrintJson(includeNotes: false)` or `PrintYaml(includeNotes: false)`. `CanonicalText` never has notes.

A rewrite returns a rule with no notes. This applies to `Simplify`, `Canonicalize`, `CompressToDerived`, `ExpandToPrimitives`, `ExpandToNand`, `ExpandToNor`, `ToNnf`, `ToCnf`, `ToDnf` and `OptimizeOrder`. The rewritten tree has different nodes, so a path in `Notes` no longer names the node that the note was written on. `RuleBuilder.FromCompiled(rule)` is not a rewrite: it keeps the notes of the rule under their new paths.
