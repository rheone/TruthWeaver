# Executable documentation examples

The runnable examples in `README.md`, `CONTEXT.md`, `docs/data-sources.md`, `docs/architecture.md`, `docs/packages.md`, `docs/rule-text.md`, `docs/rule-formats.md`, `docs/rulebuilder.md`, `docs/rewriting-rules.md`, `docs/predicates.md`, `docs/examples.md`, `docs/diagnostics.md` and `docs/benchmarks.md` are checked on every `dotnet test` by
`tests/TruthWeaver.Tests/DocExamples/DocExampleChecker.cs` (run by `DocExampleTests`). A change that breaks a documented
rule, or changes documented output, fails the build. C# fragments are not checked.

## Adding an example

1. Put a marker comment on the line directly above the fenced block (blank lines in between are fine):
   `<!-- doctest:KIND ARGUMENT -->`.
2. Use the kind that fits:

| Marker | Block language | What is checked |
| --- | --- | --- |
| `<!-- doctest:rule ID -->` | `text` | The DSL text compiles; its canonical text is remembered as `ID`. |
| `<!-- doctest:json ID -->`, `<!-- doctest:yaml ID -->` | `json`, `yaml` | The rule compiles to the same canonical text as `rule ID`. |
| `<!-- doctest:tree ID -->`, `<!-- doctest:mermaid ID -->` | `text`, `mermaid` | The block equals `PrintPlainText()` or `PrintMermaid()` of `rule ID` (the `rule` block must come earlier in the file). |
| `<!-- doctest:diagnostics-dsl SOURCE -->` (also `-json`, `-yaml`) | `text` | The block equals `FormatDiagnostics(SOURCE)`; `SOURCE` is the rest of the marker line, verbatim. |
| `<!-- doctest:skip REASON -->` | any | Not runnable (pseudo-grammar, class diagram). The reason is mandatory. |

3. Predicates the examples use come from `BuildRegistry` in `DocExampleChecker.cs`. Add a predicate there (same name,
   label and argument defaults the documentation shows) when a new example needs one. The checker
   declares the data source names `user` and `request` (with `JsonQueryValidator`), so `from("user", ...)` examples compile; a new
   source name needs adding to the `Compiler` declarations in the same file.
4. An untagged `text`, `json`, `yaml`, `mermaid` or `ebnf` block fails the check, so a new example cannot be forgotten.
5. Run `dotnet test tests/TruthWeaver.Tests --filter-class "*DocExampleTests"`. A failure names the file, line and marker
   and prints the documented and actual output side by side; fix the documentation (or the code, if the example is right).

## Strong Kleene (K3) reference checks

`docs/strong-k3/` is not covered by the README/CONTEXT checker above. It is verified on every `dotnet test` by
`tests/TruthWeaver.Tests/ReferenceDocs/K3ReferenceChecker.cs` (run by `K3ReferenceTests`) against `K3Oracle`, not the engine.
A failure is reported as `path:line: message`.

| Check | Where | What is verified |
| --- | --- | --- |
| Links | Every `.md` under `docs/strong-k3/` | Relative links and `#heading` anchors resolve. Code spans and fenced blocks are skipped. |
| Truth table | `<!-- k3:truth OP [param=value ...] -->` above a table | Operand columns then a result column; all 3^n rows present once and equal to the oracle. |
| Evaluation table | `<!-- k3:eval OP n=N [param=value ...] -->` above a table | Columns definitely-true count, possibly-true count, result; every `0 <= d <= p <= N` present once and equal to the oracle. |
| Canonical form | `<!-- k3:canonical OP vars=a,b -->` or `n=2..4` above a fenced block | One function-call expression (for example `OR(NOT(a), b)`, `ATLEAST(k + 1, ...)`; `...` splices all operands) equal to the oracle for every assignment, operand count and valid parameter. |
| Operation document | Any `docs/strong-k3/<category>/<name>.md` except `README.md` | The name is in the inventory ([operations.md](strong-k3/specification/operations.md)), in the right category directory, with that Kind and the two-line category convention (see below). Every required section is present and non-empty. Every section name is known. Each Truth table, Evaluation table or Canonical form section holds its marker. |
| Operations index | `docs/strong-k3/specification/operations.md` | The index links the document of every inventory Operation. |

Cells use `T`, `F`, `U` (or the full words), backticks allowed. `OP` is an inventory name, case-insensitive; the inventory and its
oracle bindings are in `K3Operation.cs`. Run the checks with
`dotnet test tests/TruthWeaver.Tests --filter-class "*K3ReferenceTests"`.

## Category convention

The Classification section of every operation page starts with two lines:

```text
- Category: Gates / Operators
- Category index: [Gates / Operators](README.md)
```

The `Category:` label is the label of the page's directory. The `Category index:` link points to the `README.md` of that same directory. A page with no `Category index:` line, or with a link to another page, fails with the file and line.

## Operation page template

One Markdown file describes one Operation. The file name is the lower-case canonical name, such as `xor.md` or `atleast.md`. The sections appear in this order. A section that does not apply is omitted, never left empty. Section names are in sentence case, and the checker requires these exact spellings.

| Required section | Content |
| --- | --- |
| Name | The canonical name and the spelling in each format: the DSL word, the JSON and YAML `op` value and the `RuleBuilder` member |
| Classification | The category, a `Category index:` link and whether the Operation is a Strong Kleene connective or an external operator |
| Kind | `Primitive` or `Derived` |
| Arity | The operand counts, including parameters such as `k`, `min` and `max` |
| Input domain | `{T, F, U}` for each operand, and the integer range of each parameter |
| Output domain | `{T, F, U}`, or `{T, F}` for the inspections and `Project` |
| Definition | One plain sentence that a rule author can use |
| Syntax | The canonical DSL form, the symbol forms, the JSON and YAML shape and the `RuleBuilder` member |
| Aliases | Every accepted alternative spelling |
| Formal semantics | The mathematical definition |

| Section that applies in some cases | Present when |
| --- | --- |
| Formula | A closed formula exists |
| Truth table | The Operation has a fixed number of operands |
| Evaluation table | The Operation is parameterised, variadic or a cardinality operation. A page has a Truth table or an Evaluation table, never both. |
| Canonical form | The Operation is derived and has an established definition in primitives |
| Equivalent forms | Other equivalences hold, including the classical laws that fail |
| Examples | Always, unless the table already shows the cases |
| Edge cases | A single operand, `Unknown` propagation, faults and rejected parameters need a statement |
| Mermaid diagram | The diagram shows something that the formula does not |
| Evaluation behavior | The Operation has observable evaluation behavior: short-circuit, `NotEvaluated` nodes or rewrite behavior |
| Related operations | Neighbours and contrasts exist |

Reference pages link only to other pages under `docs/strong-k3/`. They link to no decision record, work item, `.scratch` file or changelog.
