# Rule formats

How to choose between rule text, JSON and YAML, and how to convert a rule from one to another. Back to the [README](../README.md).

## Choosing a rule format

Rule text (the DSL), JSON and YAML compile to the same tree through the same Parse, Validate, Analyze and Build pipeline (see [Compilation pipeline](architecture.md#compilation-pipeline)). No format is more real than another at evaluation time. The formats differ in who writes and reads them:

| | DSL string | JSON tree | YAML tree |
| --- | --- | --- | --- |
| **Canonical and persisted form?** | Yes. A `CompiledRule<TContext>` prints back to it. | No. It is an interchange format. | No. It is an interchange format. |
| **Best for** | A person who types or reads a rule directly: a database column, a code review, a log line. | A UI rule builder that generates or reads a tree without a parser. | The same as JSON, when the host tooling already prefers YAML (configuration files, GitOps). |
| **Package** | `TruthWeaver` | `TruthWeaver` | `TruthWeaver.Yaml` |
| **Compile with** | `compiler.Compile(text)` | `compiler.CompileJson(json)` | `compiler.CompileYaml(yaml)` |
| **Print with** | `rule.CanonicalText` | `rule.PrintJson()` | `rule.PrintYaml()` |
| **Round-trips losslessly?** | Yes, by definition. | Yes. `parse(print(x))` is structurally equal to `x`. | Yes. The same guarantee holds. |
| **Nesting for `AND`/`OR`/`XOR`/`EQUIVALENT`/`IMPLIES`/`NAND`/`NOR`/`??`** | Infix with precedence (see [Precedence and grouping](strong-k3/specification/syntax.md#precedence-and-grouping)). `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND`, `NOR` and `??` need parentheses next to `AND`, `OR` or each other (see [The mixing rule](strong-k3/specification/syntax.md#the-mixing-rule)). | Explicit `{"op": "...", "operands": [...]}` nodes. There is no precedence to get wrong. | The same explicit `op` and `operands` shape as JSON. |
| **Comments** | No | No. JSON has none. | Yes (`#`). This is a practical reason to prefer YAML for rule files that people maintain. |

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

A rule-authoring UI can use this to offer "export as JSON or YAML" or "paste JSON, get back DSL to review". The UI needs a parser only for the format that the user is editing at that moment.
