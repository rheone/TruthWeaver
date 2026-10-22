# Examples rework and richer rewrite documentation

**Status:** done 2026-10-10

## Request

1. Rework `docs/examples.md`: each example set includes a data source reference, a predicate and a variable; every printing format is shown (rule text, JSON, plain text, Mermaid, and the equation dialects Unicode, LaTeX/MathJax and AsciiMath); symbol sets are explained; a Mermaid diagram draws operators in a different shape from variables and predicates; the existing examples are reused and improved.
2. Explain normal forms and simplification with printed expressions, Mermaid diagrams and MathJax, in a document linked from the root documentation.
3. Add anything of value the owner did not think of.

## Decisions (grilled 2026-10-10)

| Question | Decision |
| --- | --- |
| What is a "symbol set"? | The existing `OperatorStyle` choice (`Word`, `Symbolic`, `CStyle`) and `SimpleVariables` letters with the legend. Documentation only. A user-defined symbol table is not built. |
| Where do normal forms live? | Expand `docs/rewriting-rules.md` in place with the other notations and Mermaid. No new page. It is the secondary document, linked from the root `README.md`. |
| Granularity | Seven tickets, as listed below. |

## Findings from the code (checked 2026-10-10)

- `docs/examples.md` has seven examples and a bonus section. Data source variables (`docs/data-sources.md`) do not appear in it.
- Printers exist: `PrintRuleText`, `PrintJson`, `PrintPlainText`, `PrintMermaid(MermaidOptions)`, `PrintEquation(EquationOptions)`, `PrintEquationWithLegend`. `docs/equations.md` covers the dialects.
- `MermaidOptions.NodeShapes` already gives operators, terms and constants different shapes, off by default. Whether a variable and a predicate term differ is not yet checked (ticket 03).
- `docs/rewriting-rules.md` has reference sections for expand to primitives, compress to derived, canonical form, `Simplify`, laws applied, and normal forms (NNF, CNF, DNF, thresholds). It has no diagrams.

## Extras added (not requested)

- A "which printer for which job" table (ticket 04).
- Before and after `RuleMetrics.Depth` and `NodeCount` figures, so CNF/DNF growth is visible (ticket 05).
- A short Strong K3 point with a truth table: distribution and De Morgan hold, excluded middle does not, so `a OR NOT a` is not simplified to `True` (ticket 05).
- A decision-trace Mermaid diagram for the denied-decision example (ticket 03).

## Tickets

| Ticket | Change | Blocked by | Status |
| --- | --- | --- | --- |
| [01](issues/01-examples-use-data-source-predicate-and-variable.md) | Examples use a predicate, a variable and a data source | None | done |
| [02](issues/02-examples-show-every-printing-format.md) | Examples show every printing format | 01 | done |
| [03](issues/03-mermaid-operator-shapes.md) | Mermaid example with distinct operator shapes | 01 | done |
| [04](issues/04-examples-symbol-sets-and-printer-guide.md) | Symbol sets and a printer guide | 02 | done |
| [05](issues/05-normal-forms-notations.md) | Normal forms in rewriting-rules.md with printed, Mermaid and MathJax output | None | done |
| [06](issues/06-simplification-notations.md) | Simplify, Canonicalize and CompressToDerived with the same notations | 05 | done |
| [07](issues/07-link-docs-from-root.md) | Link the content from the root README and glossary | 04, 06 | done |
