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
