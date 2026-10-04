# Syntax

How a rule author writes an Operation. This page states the grammar rules that apply to every Operation. The Syntax section of each Operation document lists the spellings of that Operation. Back to the [specification index](README.md).

## Formats

A rule is written as rule text (the DSL), as JSON, as YAML or with the `RuleBuilder` API. All four compile to the same immutable expression tree. The meaning of a rule does not depend on the format.

| Format | Shape of an Operation |
| --- | --- |
| Rule text | A word, a symbol or a call: `a AND b`, `a && b`, `AtLeast(2, a, b, c)` |
| JSON | `{"op": "and", "operands": [ ... ]}`. Parameters are members of the same object: `"k"`, `"min"`, `"max"` |
| YAML | The same keys as JSON: `op: and` with an `operands:` list |
| `RuleBuilder` | One static member per Operation, such as `RuleBuilder.And` |

A constant is `True`, `False` or `Unknown` in rule text, and `{"const": true}`, `{"const": false}` or `{"const": "unknown"}` in JSON. YAML uses `const: unknown`. A predicate reference is `{"predicate": "name", "args": { ... }}`.

## Spellings

Every Operation has one canonical word. The canonical printer writes the word and never a symbol. A symbol is an alias: it compiles to the same node as the word.

| Operation | Words | Symbols |
| --- | --- | --- |
| `NOT` | `NOT` | `!`, `¬` |
| `AND` | `AND` | `&&`, `∧` |
| `OR` | `OR` | `\|\|`, `∨` |
| `XOR` | `XOR` | `⊕`, `⊻` |
| `EQUIVALENT` | `EQUIVALENT`, `IFF`, `XNOR` | `↔`, `⇔` |
| `IMPLIES` | `IMPLIES` | `→`, `⇒` |
| `NAND` | `NAND` | `↑`, `⊼` |
| `NOR` | `NOR` | `↓`, `⊽` |
| `COALESCE` | `COALESCE(...)` as a call | `??` as an infix chain |
| `If` | `If(c, t, f)` as a call | `c ? t : f` as a ternary |

A lone `&` or `|` is a syntax error. A lone `?` is valid only as the `?` of a ternary. Words, symbols and constants mix freely, and `a && b OR c` is valid.

Operator words and the constants `True`, `False` and `Unknown` are case-insensitive on input. In JSON and YAML the `op` value is case-insensitive on read. The printer writes `AND`, `AtLeast`, `If` and `True`.

## Forms

An Operation has one of three forms.

| Form | Operations | Rule |
| --- | --- | --- |
| Prefix | `NOT` | `NOT a`. The operator binds to the operand on its right. `NOT NOT a` is valid. |
| Infix | `AND`, `OR`, `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND`, `NOR`, `??`, `c ? t : f` | The operator sits between its operands. There is no call form: `AND(a, b)` and `XOR(a, b)` are syntax errors. |
| Call | `PARITY`, `ANY`, `ALL`, `NONE`, `COALESCE`, `ExactlyOne`, the threshold family, `BETWEEN`, `If`, the four inspections | The word, then the operands in parentheses. A call has no precedence. |

A threshold call writes its integer `k` first: `AtLeast(2, a, b, c)`. `BETWEEN` writes its two integers first: `BETWEEN(1, 2, a, b, c)`.

## Operand counts

| Operand count | Operations |
| --- | --- |
| One | `NOT`, `IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown` |
| Exactly two | `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND`, `NOR` |
| Exactly three | `If` |
| Two or more | `AND`, `OR`, `PARITY`, `ANY`, `ALL`, `NONE`, `BETWEEN`, `COALESCE`, `ExactlyOne` |
| One or more, after the integer `k` | `AtLeast`, `AtMost`, `GreaterThan`, `LessThan`, `Exactly` |

`AND` and `OR` are flat chains. `a AND b AND c` is one three-operand node, not two nested nodes. A chain of a binary-only Operation, such as `a IMPLIES b IMPLIES c`, is the diagnostic `InfixArityViolation`. `a XOR b XOR c` is the same diagnostic, and its message names `PARITY` and `ExactlyOne`. A wrong operand count for any other Operation is `MalformedTree`. Both codes are in [diagnostics](diagnostics.md).

## Precedence and grouping

From tightest to loosest, the rule text binds in this order:

1. Grouping.
2. `NOT`.
3. `AND`.
4. `OR`.

So `a OR b AND c` is `a OR (b AND c)`, and `NOT a AND b` is `(NOT a) AND b`. Calls are self-delimiting and do not take part in precedence. Precedence applies only to parsing. The canonical printer adds explicit parentheses, so a printed rule never depends on the reader knowing the order.

`()`, `[]` and `{}` all group a sub-expression and mean the same. `a AND (b OR c)`, `a AND [b OR c]` and `a AND {b OR c}` compile to the same tree. The tree does not record which delimiter was written, and the canonical text uses parentheses. Delimiters nest, and each closer matches its opener. A mismatched, unclosed or unmatched delimiter is a `SyntaxError`. The parentheses of a call are call syntax, not grouping.

## The mixing rule

Only `NOT`, `AND` and `OR` form a precedence chain. Every other infix form needs parentheses to combine with another operator at the same level. This avoids an implicit choice of precedence. A violation is the diagnostic `AmbiguousOperatorMixing`, and its message names the operator and says where to add parentheses.

| Rule text | Result |
| --- | --- |
| `a XOR b AND c` | `AmbiguousOperatorMixing` |
| `(a XOR b) AND c` | Valid |
| `a XOR b EQUIVALENT c` | `AmbiguousOperatorMixing` |
| `a ?? b AND c` | `AmbiguousOperatorMixing` |
| `(a ?? b) AND c` | Valid |
| `a ?? b ?? c` | Valid. One three-operand `COALESCE`, because coalescing is associative |
| `NOT a ?? b` | Valid. It is `COALESCE(NOT a, b)` |
| `a AND b ? c : d` | `AmbiguousOperatorMixing` |
| `(a AND b) ? c : d` | Valid |
| `a ? b : c ? d : e` | `AmbiguousOperatorMixing` |
| `a ? b : (c ? d : e)` | Valid |
| `ANY(a ? b : c, d)` | Valid |

The condition and each branch of a ternary must be a single operand or a parenthesized group. A ternary may appear without extra parentheses wherever a full expression is allowed: the root, parentheses and call arguments.

## Operations outside the rule language

`Project` and `Collapse` are methods on a `Decision`. They are not rule operators. A rule that writes either one is a syntax error in rule text and `MalformedTree` in JSON and YAML. The message points to `Decision.Project` or `Decision.Collapse`. `NXOR` is not an Operation. The compiler rejects it and names `PARITY`.

## Related

- [operations](operations.md) lists every Operation with its category and operand counts.
- [diagnostics](diagnostics.md) lists the diagnostics that this page names.
- [notation](notation.md) lists the symbols that the reference uses in formulas.
