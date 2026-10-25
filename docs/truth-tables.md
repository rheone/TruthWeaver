# Truth tables

How to print the truth table of a compiled rule. Back to the [README](../README.md).

`CompiledRule<TContext>.PrintTruthTable` lists every combination of values for the terms of a rule and shows the value of the rule for each combination. It is a read view. The canonical rule text does not change.

The examples on this page use this rule:

<!-- doctest:rule dine -->
```text
isDineIn AND NOT isBanned
```

```csharp
string table = rule.PrintTruthTable();
string csv = rule.PrintTruthTable(new TruthTableOptions { Format = TruthTableFormat.Csv });
rule.PrintTruthTable(writer, new TruthTableOptions { Format = TruthTableFormat.Json });
```

## The rows

The default table is the Strong Kleene (K3) table. Each term takes each of `False`, `Unknown` and `True`, so a rule with *n* terms has 3<sup>n</sup> rows. The cells use the letters `F`, `U` and `T`.

<!-- doctest:truthtable dine -->
```text
| isDineIn | isBanned | Result |
| --- | --- | --- |
| F | F | F |
| F | U | F |
| F | T | F |
| U | F | U |
| U | U | U |
| U | T | F |
| T | F | T |
| T | U | U |
| T | T | F |
```

The order of the table is the same on every call:

- **Columns.** The columns are the distinct terms in the order of their first occurrence, depth first and from left to right. A term that occurs twice has one column. The last column is `Result`.
- **Rows.** The first term changes slowest. The values of a term run `False`, `Unknown`, `True`. This is a layout order and not a number.

The table treats each term as a free variable. It calls no predicate and reads no data source. A rule with a variable reference, `from("source", "query")`, prints with the reference as the term. The rule gets the same table when the data source is missing or fails.

The table uses the same rules as `Compile` and `EvaluateAsync`. For every row, the result is the value that `EvaluateAsync` gives when each predicate answers with the value of its column.

## The classical table

Set `Classical` to print the table of two-valued logic. Each term takes `False` or `True` only, so a rule with *n* terms has 2<sup>n</sup> rows.

<!-- doctest:truthtable dine classical -->
```text
| isDineIn | isBanned | Result |
| --- | --- | --- |
| F | F | F |
| F | T | F |
| T | F | T |
| T | T | F |
```

A classical row can still show `Unknown`. This happens when the rule holds the constant `Unknown`.

## Terms and constants

By default, a column header is the term as a call, for example `hasCrust(crust: "thin")`. Two options change the header:

| Option | Meaning | Default |
| --- | --- | --- |
| `ShowArgumentValues` | When `false`, the header is the predicate name only. | `true` |
| `SimpleVariables` | The header is a letter, `p`, `q`, `r` and so on. | `false` |

The letters are the letters of [`PrintEquationWithLegend`](equations.md#simple-variables-and-the-legend). Print the equation with its legend to see which term each letter stands for. In the JSON format, each term is in the `terms` list with its letter and its full call.

<!-- doctest:truthtable dine classical simple -->
```text
| p | q | Result |
| --- | --- | --- |
| F | F | F |
| F | T | F |
| T | F | T |
| T | T | F |
```

`Constants` sets the text of the cells. It takes a `ConstantFormat`, as the [equation printers](equations.md#constants) do. The default style is `Letters`: `T`, `F` and `U`. The styles `Word` and `LogicSymbols` write `True`, `False` and `Unknown`, or `⊤`, `⊥` and `U`.

<!-- doctest:truthtable dine classical words -->
```text
| isDineIn | isBanned | Result |
| --- | --- | --- |
| False | False | False |
| False | True | False |
| True | False | True |
| True | True | False |
```

## Formats

`TruthTableOptions.Format` selects one of five formats. The text has no trailing line feed. Lines end with a line feed in every format. The examples below show the classical table to keep them short.

### Markdown

The default. The result is a GitHub-flavored Markdown pipe table. The printer escapes the characters that Markdown reads as markup (`\`, `|`, `` ` ``, `*`, `_`, `<`, `>`, `[` and `]`) in headers and cells. The first example on this page shows the format.

### CSV

The first row holds the headers. A field that has a comma, a quote or a line break is in quotes, and a quote in it is doubled, as RFC 4180 says. Rows end with a line feed and not with a carriage return and a line feed.

<!-- doctest:truthtable dine classical csv -->
```text
isDineIn,isBanned,Result
F,F,F
F,T,F
T,F,T
T,T,F
```

### JSON

The result is one JSON object. `logic` is `K3` or `classical`. Each entry of `terms` has the `name` of the column and the full call of the `term`. Each entry of `rows` has the `values` of the terms in column order and the `result`. The cells use the constant style, so set the style `Word` when a program must read the values as `True`, `False` and `Unknown`.

<!-- doctest:truthtable dine classical json -->
```json
{
  "logic": "classical",
  "terms": [
    { "name": "isDineIn", "term": "isDineIn" },
    { "name": "isBanned", "term": "isBanned" }
  ],
  "rows": [
    { "values": ["F", "F"], "result": "F" },
    { "values": ["F", "T"], "result": "F" },
    { "values": ["T", "F"], "result": "T" },
    { "values": ["T", "T"], "result": "F" }
  ]
}
```

### HTML

The result is a `table` element with the class `tw-truth-table`. Each cell has the class `tw-true`, `tw-false` or `tw-unknown`. A result cell also has the class `tw-result`. With `SimpleVariables`, the `title` attribute of a header has the full call. The text in the table is escaped, as the output of [`PrintHtml`](html-and-dot.md) is. The host page sets the colors with CSS rules for the classes.

<!-- doctest:truthtable dine classical html -->
```html
<table class="tw-truth-table">
<thead>
<tr><th>isDineIn</th><th>isBanned</th><th>Result</th></tr>
</thead>
<tbody>
<tr><td class="tw-false">F</td><td class="tw-false">F</td><td class="tw-false tw-result">F</td></tr>
<tr><td class="tw-false">F</td><td class="tw-true">T</td><td class="tw-false tw-result">F</td></tr>
<tr><td class="tw-true">T</td><td class="tw-false">F</td><td class="tw-true tw-result">T</td></tr>
<tr><td class="tw-true">T</td><td class="tw-true">T</td><td class="tw-false tw-result">F</td></tr>
</tbody>
</table>
```

### LaTeX

The result is a `tabular` environment. The cells are in math mode, and the printer escapes the text of terms and constants. A vertical rule stands before the result column.

<!-- doctest:truthtable dine classical latex -->
```text
\begin{tabular}{cc|c}
$\text{isDineIn}$ & $\text{isBanned}$ & Result \\
\hline
$\text{F}$ & $\text{F}$ & $\text{F}$ \\
$\text{F}$ & $\text{T}$ & $\text{F}$ \\
$\text{T}$ & $\text{F}$ & $\text{T}$ \\
$\text{T}$ & $\text{T}$ & $\text{F}$ \\
\end{tabular}
```

## Size limits

A table grows fast: 8 terms give 6,561 rows and 12 terms give 531,441 rows. Two limits keep the output usable:

| Limit | Value | What happens |
| --- | --- | --- |
| Default cap | 8 terms (`TruthTableOptions.DefaultTermCap`) | The table is refused. Set `AllowLargerTable` to print it. |
| Term limit | 12 terms by default (`TruthTableOptions.TermLimit`) | The table is refused, whatever else the options say. |

`TermLimit` is yours to set. It has no ceiling: a table of n terms has 3^n rows (2^n when `Classical`), and you own that time and memory cost. Write to a `TextWriter` to keep memory flat. A value below 1 throws `ArgumentOutOfRangeException`. A `TermLimit` below the default cap lowers the cap too, so a table above it is refused even with `AllowLargerTable`. A table above the default cap still needs `AllowLargerTable`, however high `TermLimit` is.

```csharp
TruthTableOptions options = new() { AllowLargerTable = true, TermLimit = 14, Classical = true };
```

The limits count distinct terms. They apply to the classical table too, but a classical table is smaller: 12 terms give 4,096 rows.

A table above the cap with `AllowLargerTable` set is printed and carries a `TRE0034` warning (`TruthTableCapRaised`). A refused table has a `TRE0035` error (`TruthTableTooLarge`). The [diagnostic reference](diagnostics.md) lists both codes.

A table within both the cap and the term limit can still be refused: building the value table's shared decision diagram can need more nodes than its internal [node budget](rewriting-rules.md#the-node-budget) when the rule's term ordering is unlucky for its shape. This is rare and reports the same `TRE0035` error. Nothing is written either way.

## Writers and the Try form

| Member | Result |
| --- | --- |
| `PrintTruthTable(options)` | The table as a string. It throws `InvalidOperationException` when the table is refused. |
| `PrintTruthTable(writer, options)` | Writes the same text to a `TextWriter`. It does not flush or dispose the writer. Nothing is written when the table is refused. |
| `TryPrintTruthTable(options, out text, out diagnostics)` | Returns `false` for a refused table, with no text and the `TRE0035` error. A table that is printed can still have the `TRE0034` warning. |
| `TryPrintTruthTable(writer, options, out diagnostics)` | The same, for a `TextWriter`. |

```csharp
if (rule.TryPrintTruthTable(new TruthTableOptions { AllowLargerTable = true }, out string? text, out var diagnostics))
{
    Console.WriteLine(text);
}
else
{
    Console.WriteLine(diagnostics[0].Message);
}
```
