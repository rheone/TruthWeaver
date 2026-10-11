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
| **Round-trips losslessly?** | Yes, by definition. | Yes. `parse(print(x))` is structurally equal to `x`. | Yes. The same guarantee holds. |
| **Nesting for `AND`/`OR`/`XOR`/`EQUIVALENT`/`IMPLIES`/`NAND`/`NOR`/`??`** | Infix with precedence (see [Precedence and grouping](strong-k3/specification/syntax.md#precedence-and-grouping)). `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND`, `NOR` and `??` need parentheses next to `AND`, `OR` or each other (see [The mixing rule](strong-k3/specification/syntax.md#the-mixing-rule)). | Explicit `{"op": "...", "operands": [...]}` nodes. There is no precedence to get wrong. | The same explicit `op` and `operands` shape as JSON. |
| **Notes on nodes** | Yes: a `//` or `/* */` comment. See [Notes](#notes). | Yes: a `note` property. | Yes: a `note` key. A YAML `#` comment is not a note: the compiler ignores it and a printed rule does not have it. |

The grammar of the DSL is in [Rule text](rule-text.md). The JSON shape is defined by the schema in [rule-tree.schema.json](../src/TruthWeaver/Json/rule-tree.schema.json). The shape of each operator in each format is in [Formats](strong-k3/specification/syntax.md#formats).

A predicate argument cannot be null. In JSON, `null` is an error. In YAML, an unquoted `null`, `~` or empty value is the same error, with the same diagnostic code and expectation. To pass the text "null", quote it: `role: "null"`.

You can also build a rule without writing text in any of these formats. See [RuleBuilder](rulebuilder.md).

`CanonicalText` is more than minimal. It puts parentheses around an operand when that operand is a different operator from the one it sits under, for example `a AND b OR c` prints as `(a AND b) OR c`. It does this even where precedence alone already makes the parse clear. The goal is a rule that a reader understands at a glance, without working out the precedence, and that also parses back to the same tree.

## Converting between DSL, JSON, and YAML

A compiled rule converts without loss to any of the three formats. Print the rule in one format and compile the result in the other. Nothing in the compiled tree depends on the format:

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
