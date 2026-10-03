# Executable documentation examples

The runnable examples in `README.md` and `CONTEXT.md` are checked on every `dotnet test` by
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
   label and argument defaults the documentation shows) when a new example needs one.
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
| Operation document | Any `docs/strong-k3/<category>/<name>.md` except `README.md` | The name is in the approved inventory ([PROPOSAL.md](strong-k3/PROPOSAL.md) section 4), in the right category directory, with that Kind and a matching `Category:` line; all required sections are present and non-empty; each Truth Table, Evaluation Table or Canonical Form section holds its marker. |

Cells use `T`, `F`, `U` (or the full words), backticks allowed. `OP` is an inventory name, case-insensitive; the inventory and its
oracle bindings are in `K3Operation.cs`. Run the checks with
`dotnet test tests/TruthWeaver.Tests --filter-class "*K3ReferenceTests"`.
