# Rule text

How to write a rule as text (the DSL), how to print it, and how to tidy it. The grammar is here. The meaning of each operator is in the [Strong Kleene (K3) reference](strong-k3/README.md). Back to the [README](../README.md).

## Where each rule is stated

This page does not repeat the K3 reference. These topics are in the specification:

| Topic | Page |
| --- | --- |
| Words and symbols for each operator | [Spellings](strong-k3/specification/syntax.md#spellings) |
| The three forms: prefix, infix and call | [Forms](strong-k3/specification/syntax.md#forms) |
| Operand counts and the errors for a wrong count | [Operand counts](strong-k3/specification/syntax.md#operand-counts) |
| Precedence and grouping | [Precedence and grouping](strong-k3/specification/syntax.md#precedence-and-grouping) |
| Which operators need parentheses next to each other | [The mixing rule](strong-k3/specification/syntax.md#the-mixing-rule) |
| `Project`, `Collapse` and `NXOR` | [Operations outside the rule language](strong-k3/specification/syntax.md#operations-outside-the-rule-language) |
| Every operator, its category and its operand counts | [Operations](strong-k3/specification/operations.md) |
| Diagnostic codes | [Diagnostics](strong-k3/specification/diagnostics.md) |

## Grammar

The whole DSL in EBNF. `{ x }` is zero or more, `[ x ]` is optional and `|` is a choice. A term name cannot be a reserved word. The reserved words are the operator and constant keywords: `AND`, `OR`, `NOT`, `XOR`, `EQUIVALENT`, `IFF`, `XNOR`, `IMPLIES`, `NAND`, `NOR`, `TRUE`, `FALSE`, `UNKNOWN`, `PARITY`, `NXOR`, `ANY`, `ALL`, `NONE`, `BETWEEN`, `COALESCE`, `IF`, `ISTRUE`, `ISFALSE`, `ISUNKNOWN`, `ISKNOWN`, `PROJECT`, `COLLAPSE`, `EXACTLYONE`, `ATLEAST`, `ATMOST`, `GREATERTHAN`, `LESSTHAN` and `EXACTLY`, in any case. `PredicateRegistryBuilder<TContext>` throws an `ArgumentException` that names the word when you register a predicate with one of these names, and the source generator reports `TWG007` for a method with one.

<!-- doctest:skip grammar notation, not a rule -->
```ebnf
rule        = expression ;

expression  = or_expr [ "?" or_expr ":" or_expr ] ;          (* ternary = If *)
or_expr     = and_expr { ( "OR" | "||" | "∨" ) and_expr } ;
and_expr    = infix_expr { ( "AND" | "&&" | "∧" ) infix_expr } ;
infix_expr  = not_expr [ infix_op not_expr ]                 (* at most one *)
            | not_expr { "??" not_expr } ;                   (* COALESCE chain *)
infix_op    = "XOR" | "⊕" | "⊻" | "EQUIVALENT" | "IFF" | "XNOR" | "↔" | "⇔"
            | "IMPLIES" | "→" | "⇒" | "NAND" | "↑" | "⊼" | "NOR" | "↓" | "⊽" ;
not_expr    = ( "NOT" | "!" | "¬" ) not_expr | primary ;

primary     = "(" expression ")" | "[" expression "]" | "{" expression "}"
            | constant | call | term ;
constant    = "True" | "False" | "Unknown" ;
call        = list_op "(" expression { "," expression } ")"
            | threshold "(" integer "," expression { "," expression } ")"
            | "BETWEEN" "(" integer "," integer "," expression { "," expression } ")"
            | "If" "(" expression "," expression "," expression ")"
            | inspection "(" expression ")" ;
list_op     = "PARITY" | "ANY" | "ALL" | "NONE" | "COALESCE" | "ExactlyOne" ;
threshold   = "AtLeast" | "AtMost" | "GreaterThan" | "LessThan" | "Exactly" ;
inspection  = "IsTrue" | "IsFalse" | "IsUnknown" | "IsKnown" ;

term        = identifier [ "(" [ argument { "," argument } ] ")" ] ;
argument    = identifier ":" literal ;
literal     = string | number | "true" | "false" | "[" [ literal { "," literal } ] "]" ;
```

Some rules are context rules, not syntax, so the grammar does not show them:

- **No implicit mixing.** An `infix_op` expression, a `??` chain or a ternary cannot sit next to `AND`, `OR`, another infix operator or a nested ternary at the same level without parentheses. The compiler reports `AmbiguousOperatorMixing`. See [The mixing rule](strong-k3/specification/syntax.md#the-mixing-rule).
- **Checks after parsing.** The compiler checks operand counts, threshold bounds and argument schemas after it parses the rule. A failure is a diagnostic, not a syntax error.
- **Rejected spellings.** `NXOR`, `Project` and `Collapse` are not in the language. The compiler rejects them in rule text, JSON and YAML, and the diagnostic names the replacement. See [Operations outside the rule language](strong-k3/specification/syntax.md#operations-outside-the-rule-language).

## Case

Operator words and the constants `True`, `False` and `Unknown` are case-insensitive on input, in every format. `and`, `And` and `AND` compile to the same node. The printer writes one spelling: operator words in upper case (`AND`, `XOR`), threshold and function names in upper camel case (`AtLeast`, `ExactlyOne`, `If`, `IsTrue`) and the constants as `True`, `False` and `Unknown`. The printer writes the word form of an operator and never a symbol.

Predicate names and argument names are case-insensitive too. `hasCrust(Crust: "thin")` and `hasCrust(crust: "thin")` compile to the same term, and the printer writes the name as the predicate's schema spells it. Argument values are case-sensitive.

The [Spellings](strong-k3/specification/syntax.md#spellings) table lists the accepted words and symbols.

## Grouping delimiters

`()`, `[]` and `{}` group a sub-expression and mean the same. The tree does not record which delimiter you wrote. `CanonicalText` always uses parentheses. For the rule, see [Precedence and grouping](strong-k3/specification/syntax.md#precedence-and-grouping).

These details apply to the parser:

- Each closer must match its opener. `(a AND b]` is an error.
- A function call keeps `(` as its own argument-list syntax. Only a grouped sub-expression can use `[` or `{`.
- Brackets and braces inside a quoted string are ordinary text.

A delimiter mistake is a `SyntaxError` diagnostic with the exact span:

| Mistake | Example | Message (span) |
| ------- | ------- | -------------- |
| Mismatched closer | `a AND (b OR c]` | `Expected ')' to close '(' at offset 6 but found ']'.` (the `]`) |
| Unclosed group | `a AND (b OR c` | `Unclosed '(' at offset 6: expected ')' before the end of the rule.` (the `(`) |
| Closer with no opener | `a AND b)` | `Unexpected closing ')' with no matching opener.` (the `)`) |

To print a rule with delimiters that change with the nesting depth, pass a `GroupingStyle` to `CompiledRule.PrintRuleText`:

```csharp
CompiledRule<MyContext> rule = compiler.Compile("a AND (b OR (c AND (d OR (e AND (f OR g)))))").CompiledRule!;

rule.CanonicalText;                                  // a AND (b OR (c AND (d OR (e AND (f OR g)))))  (parentheses only)
rule.PrintRuleText(GroupingStyle.Parentheses);           // same as CanonicalText
rule.PrintRuleText(GroupingStyle.DepthCycling);          // a AND (b OR [c AND {d OR (e AND [f OR g])}])
```

`DepthCycling` is opt-in and deterministic. The delimiter depends only on how many groups enclose it, and it cycles through `(`, `[` and `{`. It helps people read a deeply nested rule. The output always parses back to a tree equal to the original, so `CanonicalText` stays the form to persist. Function-call argument lists keep `(`.

## Whitespace

Whitespace between tokens never matters, so you can lay out rule text freely. `CanonicalText` prints one space around each infix operator and after each comma, with no leading or trailing whitespace, whatever spacing the rule had when you wrote it.

To tidy text as written, use `RuleText.NormalizeWhitespace`. It keeps your operators, letter case and delimiters. It does not compile the text and does not need a predicate registry.

```csharp
RuleText.NormalizeWhitespace("  a&&b ||\n  !c  ");          // "a && b || !c"
RuleText.NormalizeWhitespace("ANY( a ,b,	c )");            // "ANY(a, b, c)"
RuleText.NormalizeWhitespace("named( value :\"x  y\" )");   // "named(value: \"x  y\")" (string contents untouched)
```

The result is the same for any input spacing, and a second call changes nothing. The result compiles to a tree equal to the tree of the input. The prefix `!` and `¬` touch their operand. The argument list of a call or term touches its name. Nothing pads the inside of `()`, `[]` or `{}`. The function keeps characters that the DSL does not recognize in place, so the text of a rule that does not compile yet is never lost.

## Printer spelling of the ternary

`If(c, t, f)` and `c ? t : f` compile to the same node. The `CStyle` operator style of the tree printers labels the node `?:`. The other styles label it `If`.
