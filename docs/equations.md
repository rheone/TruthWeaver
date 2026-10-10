# Equations

How to print a compiled rule as a flat, single-line infix equation. Back to the [README](../README.md).

`CompiledRule<TContext>.PrintEquation` writes the rule the way a math text writes a formula, for example `a ∧ (b ∨ c)`. The [tree printers](rulebuilder.md#rendering-a-rule-as-a-diagram) draw a diagram. The equation is a line of text that you can put in a report, a log, a chat message or a Markdown page. It is a read view. The canonical rule text does not change.

```csharp
string equation = rule.PrintEquation();
string latex = rule.PrintEquation(new EquationOptions { Dialect = EquationDialect.LaTeX });
```

## Operators

The connectives print as infix symbols. Every other operator prints in function-call form. The call uses the DSL spelling and the DSL argument order, with the count arguments first, for example `AtLeast(2, a, b, c)`.

| Operator | Unicode | LaTeX |
| --- | --- | --- |
| `AND` | `∧` | `\land` |
| `OR` | `∨` | `\lor` |
| `NOT` | `¬` | `\lnot` |
| `XOR` | `⊕` | `\oplus` |
| `EQUIVALENT` | `↔` | `\leftrightarrow` |
| `IMPLIES` | `→` | `\rightarrow` |
| `NAND` | `↑` | `\uparrow` |
| `NOR` | `↓` | `\downarrow` |

`ExactlyOne`, the threshold family (`AtLeast`, `AtMost`, `Exactly`, `GreaterThan`, `LessThan`), `BETWEEN`, `PARITY`, `ANY`, `ALL`, `NONE`, `If`, `COALESCE` and the four inspections have no standard infix symbol. They print in function-call form in every dialect.

Parentheses appear only around a connective that is an operand of another connective. The root has no outer pair. A negation and a function call never get parentheses.

## Terms

A term prints as its full call by default, for example `hasCrust(crust: "thin")`. Set `ShowArgumentValues` to `false` to print the predicate name only, for example `hasCrust`.

## Dialects

Set `EquationOptions.Dialect` to choose the notation.

| Dialect | Output |
| --- | --- |
| `Unicode` (default) | Plain text with the symbols in the table above. No delimiters and no escaping. |
| `LaTeX` | LaTeX math. A term, a constant and a function name are `\text` groups. |

## LaTeX wrap modes

Set `EquationOptions.LatexWrap` to choose the envelope of a `LaTeX` equation. The other dialects ignore it.

| Mode | Envelope | Use it for |
| --- | --- | --- |
| `None` (default) | No delimiters. | A `.tex` file or a Pandoc pipeline that supplies its own math mode. |
| `DoubleDollar` | `$$...$$` | Display math in Markdown and in most LaTeX tools. |
| `MathJaxSafe` | `` $`...`$ `` | GitHub Markdown and other MathJax renderers. |

For the rule `a AND hasCrust(crust: "thin")`, the three modes write:

```text
\text{a} \land \text{hasCrust(crust: "thin")}
$$\text{a} \land \text{hasCrust(crust: "thin")}$$
$`\text{a} \land \text{hasCrust(crust: "thin")}`$
```

### Escaping

A predicate name or an argument value can hold a character that LaTeX reads as a command. The printer escapes these characters: `_ % & # { } \ ^ ~ $`.

| Mode | How the printer escapes a character |
| --- | --- |
| `None`, `DoubleDollar` | Inside the `\text` group, with the text-mode command: `\_`, `\%`, `\&`, `\#`, `\{`, `\}`, `\$`, `\textbackslash{}`, `\textasciicircum{}`, `\textasciitilde{}`. |
| `MathJaxSafe` | The `\text` group ends, and a math command writes the character: `\_`, `\%`, `\&`, `\#`, `\{`, `\}`, `\$`, `\backslash`, `\hat{\ }`, `\sim`. A backtick becomes `\unicode{x60}`. |

MathJax does not support text-mode commands such as `\textbackslash`, so `MathJaxSafe` uses only math commands from the MathJax subset that GitHub renders. The `` $`...`$ `` envelope makes Markdown treat the equation as code, so an underscore does not start emphasis or a subscript. A backtick in a name would end that code span, and so `MathJaxSafe` replaces it.

For the label `has_crust`, `MathJaxSafe` writes `` $`\text{has}\_\text{crust}`$ ``.
