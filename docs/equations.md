# Equations

How to print a compiled rule as a flat, single-line infix equation. Back to the [README](../README.md).

`CompiledRule<TContext>.PrintEquation` writes the rule the way a math text writes a formula, for example `a ∧ (b ∨ c)`. The [tree printers](rulebuilder.md#rendering-a-rule-as-a-diagram) draw a diagram. The equation is a line of text that you can put in a report, a log, a chat message or a Markdown page. It is a read view. The canonical rule text does not change.

```csharp
string equation = rule.PrintEquation();
string latex = rule.PrintEquation(new EquationOptions { Dialect = EquationDialect.LaTeX });
```

## Operators

The connectives print as infix symbols. Every other operator prints in function-call form. The call uses the DSL spelling and the DSL argument order, with the count arguments first, for example `AtLeast(2, a, b, c)`.

| Operator | Unicode | LaTeX | AsciiMath | CStyle |
| --- | --- | --- | --- | --- |
| `AND` | `∧` | `\land` | `^^` | `&&` |
| `OR` | `∨` | `\lor` | `vv` | `\|\|` |
| `NOT` | `¬` | `\lnot` | `not` | `!` |
| `XOR` | `⊕` | `\oplus` | `oplus` | `^` |
| `EQUIVALENT` | `↔` | `\leftrightarrow` | `<=>` | `==` |
| `IMPLIES` | `→` | `\rightarrow` | `=>` | `IMPLIES` (no C-family spelling) |
| `NAND` | `↑` | `\uparrow` | `uarr` | `NAND` (no C-family spelling) |
| `NOR` | `↓` | `\downarrow` | `darr` | `NOR` (no C-family spelling) |

| Operator | MathML | Typst | Ascii |
| --- | --- | --- | --- |
| `AND` | `<mo>∧</mo>` | `and` | `/\` |
| `OR` | `<mo>∨</mo>` | `or` | `\/` |
| `NOT` | `<mo>¬</mo>` | `not` | `~` |
| `XOR` | `<mo>⊕</mo>` | `xor` | `(+)` |
| `EQUIVALENT` | `<mo>↔</mo>` | `<=>` | `<->` |
| `IMPLIES` | `<mo>→</mo>` | `=>` | `->` |
| `NAND` | `<mo>↑</mo>` | `arrow.t` | `NAND` (no ASCII symbol) |
| `NOR` | `<mo>↓</mo>` | `arrow.b` | `NOR` (no ASCII symbol) |

`ExactlyOne`, the threshold family (`AtLeast`, `AtMost`, `Exactly`, `GreaterThan`, `LessThan`), `BETWEEN`, `PARITY`, `ANY`, `ALL`, `NONE`, `COALESCE` and the four inspections have no standard infix symbol. They print in function-call form in every dialect. `If` follows the same rule in every dialect but `CStyle`, which renders it as the ternary `c ? t : f` instead of the call `If(c, t, f)` (see [Dialects](#dialects)).

`CStyle` is a rendering aid, not C# semantics: `==` for `EQUIVALENT` and `?:` for `If` follow Strong K3, not C#'s `null`-handling `==`.

Parentheses appear only around a connective that is an operand of another connective. The root has no outer pair. A negation and a function call never get parentheses.

## Terms

A term prints as its full call by default, for example `hasCrust(crust: "thin")`. Set `ShowArgumentValues` to `false` to print the predicate name only, for example `hasCrust`.

## Constants

`EquationOptions.Constants` takes a `ConstantFormat`. It sets how the constants `True`, `False` and `Unknown` print. It has a `Style` and an optional override string for each constant: `TrueText`, `FalseText` and `UnknownText`.

| Style | `True` | `False` | `Unknown` |
| --- | --- | --- | --- |
| `Word` | `True` | `False` | `Unknown` |
| `Letters` (default) | `T` | `F` | `U` |
| `LogicSymbols` | `⊤` | `⊥` | `U` |

An override wins over the style for its own constant. An override is literal text. The dialect escapes it as it escapes a term, so `yes_1` is safe in LaTeX, and a non-ASCII override is safe in the `Ascii` dialect. An override cannot be empty or hold a line break.

The `LogicSymbols` style writes `⊤` and `⊥` in Unicode and in `CStyle`. LaTeX gets the commands `\top` and `\bot`. AsciiMath gets the keywords `top` and `bot`. MathML gets the elements `<mo>⊤</mo>` and `<mo>⊥</mo>`. Typst gets the symbols `top` and `bot`. The `Ascii` dialect gets the words `top` and `bot`, because the symbols are not ASCII. `Unknown` has no standard symbol, so it prints `U` in every dialect (in a `\text` group in LaTeX, in quotes in AsciiMath and Typst, and in an `mtext` element in MathML).

```text
isDineIn AND (False OR Unknown)
```

```text
isDineIn ∧ (F ∨ U)
```

```text
isDineIn ∧ (False ∨ Unknown)
```

```text
isDineIn ∧ (⊥ ∨ U)
```

```text
\text{isDineIn} \land (\bot \lor \text{U})
```

```text
`"isDineIn" ^^ ("F" vv "U")`
```

```csharp
string letters = rule.PrintEquation();   // isDineIn ∧ (F ∨ U)

EquationOptions options = new() { Constants = new ConstantFormat { Style = ConstantStyle.Word } };
string words = rule.PrintEquation(options);   // isDineIn ∧ (False ∨ Unknown)
```

`Letters` is the default for every equation dialect and for `PrintEquationWithLegend`. Set `Style` to `Word` to print `True`, `False` and `Unknown`. The rule text, plain text, Mermaid, HTML and DOT printers keep `Word` as their default.

### Why letters, and why not `⊥`

In classical logic, `⊥` means false. In partial logic and in domain theory, `⊥` often means "no information", and that is the meaning of `Unknown` here. A reader who knows that convention can read `⊥` as `Unknown`, and then read `A ∧ ⊥` the wrong way. The `LogicSymbols` style keeps `⊥` for `False`, as classical logic does, but the symbol is not safe for a mixed audience. The letters `T`, `F` and `U` have one meaning each, so `Letters` is the default style.

There is no `1`, `0` or `½` style. The order `False < Unknown < True` is an aid for the connectives. It is not a number scale, and a numeric form invites arithmetic on it. See [Truth order](glossary.md).

## Dialects

Set `EquationOptions.Dialect` to choose the notation.

| Dialect | Output |
| --- | --- |
| `Unicode` (default) | Plain text with the symbols in the table above. No delimiters and no escaping. |
| `LaTeX` | LaTeX math. A term, a constant and a function name are `\text` groups. |
| `AsciiMath` | AsciiMath between backticks. A term, a constant and a function name are quoted runs. |
| `CStyle` | Plain text with the C-family symbols in the table above. No delimiters and no escaping. `If` renders as the ternary `c ? t : f`. |
| `MathML` | Presentation MathML in one `math` element. A term, a constant and a function name are `mtext` elements. |
| `Typst` | Typst math between `$` signs. A term, a constant and a function name are quoted strings. |
| `Ascii` | Plain text with only the characters from space to tilde. The printer escapes every other character. |

For the rule `a AND If(hasCrust(crust: "thin"), b, c)`, `CStyle` writes:

```text
a && (hasCrust(crust: "thin") ? b : c)
```

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

## AsciiMath

The `AsciiMath` dialect always writes the equation between backticks, the AsciiMath delimiter. It has no wrap modes. A term, a constant and a function name are text between double quotes. A call keeps the function-call form of the other dialects.

```text
`"a" ^^ ("b" vv "c")`
`"AtLeast"(2, "a", "b", "c")`
```

AsciiMath reads every character between double quotes as literal text, so the characters `_ % & # { } \ ^ ~ $` need no escape. Two characters need work:

- AsciiMath has no escape for a double quote. The printer replaces each straight quote in a term with the right double quotation mark (U+201D). The term `hasCrust(crust: "thin")` prints as `"hasCrust(crust: ”thin”)"`.
- A backtick would end the envelope. The printer replaces each backtick in a term with the reversed prime (U+2035).

## MathML

The `MathML` dialect always writes one `math` element that declares the MathML namespace. An operator is an `mo` element. A term, a constant and a function name are `mtext` elements. A group is a pair of `mo` parentheses, and a count is an `mn` element. A call keeps the function-call form of the other dialects.

```text
<math xmlns="http://www.w3.org/1998/Math/MathML"><mtext>a</mtext> <mo>∧</mo> <mo>(</mo><mtext>b</mtext> <mo>∨</mo> <mtext>c</mtext><mo>)</mo></math>
<math xmlns="http://www.w3.org/1998/Math/MathML"><mtext>AtLeast</mtext><mo>(</mo><mn>2</mn> <mo>,</mo> <mtext>a</mtext> <mo>,</mo> <mtext>b</mtext><mo>)</mo></math>
```

The output is well-formed XML for any rule. The printer writes these replacements in a term, a constant and a function name:

| Character | MathML text |
| --- | --- |
| `&` | `&amp;` |
| `<` | `&lt;` |
| `>` | `&gt;` |
| Tab, line feed, carriage return | `&#x9;`, `&#xA;`, `&#xD;` |
| A character that XML 1.0 cannot hold: a control character other than tab, line feed and carriage return, a lone surrogate, U+FFFE and U+FFFF | U+FFFD, the replacement character |

A browser or an e-book reader chooses the spacing, and it joins runs of white space in an `mtext` element. The equation has no `mrow` element, so a reader that needs the grouping must use the `mo` parentheses.

## Typst

The `Typst` dialect writes the equation between `$` signs, the Typst inline math delimiters. A term, a constant and a function name are quoted strings, and the operators are Typst symbols. A call keeps the function-call form of the other dialects.

```text
$"a" and ("b" or "c")$
$"AtLeast"(2, "a", "b")$
```

A Typst string reads every character literally except the backslash and the double quote. The printer escapes these characters in a term, a constant and a function name:

| Character | Typst text |
| --- | --- |
| `"` | `\"` |
| `\` | `\\` |
| Line feed, carriage return, tab | `\n`, `\r`, `\t` |
| Any other control character | `\u{X}`, with the code point in hexadecimal |

Typst has no standard symbol for `NAND` and `NOR`. The dialect uses the arrows `arrow.t` and `arrow.b`, the same arrows as the Unicode dialect.

## Ascii

The `Ascii` dialect writes only the characters from U+0020 to U+007E, and a line feed in a legend. It is for a log, a terminal or a system that accepts only 7-bit text. A term is not quoted. `NAND` and `NOR` have no ASCII symbol and keep their word form.

```text
a /\ (b \/ c) /\ ~d
AtLeast(2, a, b)
```

A term, a constant and a function name can hold any character. The printer escapes them with these rules:

| Character | Ascii text |
| --- | --- |
| `\` | `\\` |
| U+0020 to U+007E, except `\` | The character itself. |
| Every other character, including a control character, a line break and any non-ASCII character | `\uXXXX`, where `XXXX` is the UTF-16 code unit in four uppercase hexadecimal digits. A character outside the Basic Multilingual Plane is two escapes, one for each surrogate. |

The term `café` prints as `caf\u00E9`. Because the printer doubles each backslash, `\u` in a term always starts an escape. An unquoted term can hold spaces and parentheses, so the `Ascii` dialect is for reading and logging. Do not parse it.

## Simple variables and the legend

Set `EquationOptions.SimpleVariables` to `true` to print each term as a letter. A rule with many predicate calls then reads like a textbook formula, for example `p ∧ (q ∨ r)`. All dialects support the mode.

`PrintEquationWithLegend` returns the equation and a legend that maps each letter to its term. It always uses simple-variable mode.

```csharp
EquationWithLegend result = rule.PrintEquationWithLegend();

string equation = result.Equation;     // p ∧ (q ∨ p)
string legend = result.LegendText;     // p = a  /  q = hasCrust(crust: "thin")
foreach (EquationLegendEntry entry in result.Legend)
{
    Console.WriteLine($"{entry.Variable}: {entry.TermText}");
}
```

| Member | Content |
| --- | --- |
| `Equation` | The equation with letters, wrapped for the dialect. |
| `Legend` | One `EquationLegendEntry` for each distinct term, in letter order. |
| `LegendText` | One `letter = term` line for each entry, joined with a line feed. Each line has the envelope of the dialect, so a `$$` equation gets a `$$` legend line. The text is empty for a rule with no term. |

An `EquationLegendEntry` has the `Variable` text, the `Term` identity (predicate name and arguments) and the `TermText`. `TermText` is the full term call in the notation of the dialect. It shows the argument values even when `ShowArgumentValues` is `false`.

`PrintEquationWithLegend(writer)` writes the equation, a line feed and the legend text to a `TextWriter`. It writes only the equation when the rule has no term. See [Write a printer to a TextWriter](examples.md#write-a-printer-to-a-textwriter).

### Lettering

- Letters follow the first occurrence of a term, in a depth-first walk from left to right. The same rule always gets the same letters.
- Identical terms share one letter. Terms of one predicate with different argument values are different terms.
- Constants and function names get no letter.
- The letters are `p q r s t u v w x y z`. After the eleventh distinct term, the letters repeat with a numeric subscript: `p₁ q₁ ... z₁`, then `p₂`. The subscript is written `p₁` in Unicode and in `CStyle`, `p_{1}` in LaTeX, `p_1` in AsciiMath, Typst and `Ascii`, and `<msub><mi>p</mi><mn>1</mn></msub>` in MathML. A MathML letter is an `mi` element, and a MathML legend line is `<mi>p</mi> <mo>=</mo> <mtext>a</mtext>` inside its own `math` element.

`PrintEquation` with `SimpleVariables` set returns the equation only. Use `PrintEquationWithLegend` when the reader needs the legend.
