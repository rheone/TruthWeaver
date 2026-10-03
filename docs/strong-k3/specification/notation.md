# Notation

The conventions used in every formula of the reference. Back to the [specification index](README.md).

## Rendering

Formulas are LaTeX in GitHub Flavored Markdown: inline as `$...$` and displayed as `$$...$$`. Rules used throughout:

- A value is written with `\mathsf`, as $\mathsf{T}$, $\mathsf{F}$, $\mathsf{U}$ in formulas and plain `T`, `F`, `U` in tables.
- Operation names inside a formula use `\operatorname`, for example $\operatorname{AtLeast}_k$.
- Anything a reader might type or search for (an Operation's name, a canonical form) is also given as plain text in a code span or fenced block. The plain text is what the test suite verifies; the LaTeX restates it.
- A formula never sits inside a table cell with a literal pipe character.

## Values and variables

| Symbol | LaTeX | Meaning |
| --- | --- | --- |
| $\mathsf{T}$, $\mathsf{F}$, $\mathsf{U}$ | `\mathsf{T}`, `\mathsf{F}`, `\mathsf{U}` | True, False, Unknown |
| $a, b, c$ | `a, b, c` | Operands of a fixed-arity Operation, each a value |
| $x_1, \dots, x_n$ | `x_1, \dots, x_n` | The $n$ operands of a variadic Operation |
| $n$ | `n` | The operand count |
| $k$, $\min$, $\max$ | `k`, `\min`, `\max` | Integer parameters of the threshold and `BETWEEN` operations. $\min(a, b)$ and $\max(a, b)$ also denote the minimum and maximum under the truth order; the context decides |
| $c, t, f$ | `c, t, f` | Condition, then-branch and else-branch of `If` |
| $\{\mathsf{T}, \mathsf{F}, \mathsf{U}\}$ | `\{\mathsf{T}, \mathsf{F}, \mathsf{U}\}` | The value set |

## Connectives

| Operation | Symbol | LaTeX | DSL symbol spelling |
| --- | --- | --- | --- |
| `NOT` | $\neg a$ | `\neg a` | `!` `¬` |
| `AND` | $a \land b$ | `a \land b` | `&&` `∧` |
| `OR` | $a \lor b$ | `a \lor b` | `\|\|` `∨` |
| `IMPLIES` | $a \to b$ | `a \to b` | `→` (input aliases `⇒`) |
| `EQUIVALENT` | $a \leftrightarrow b$ | `a \leftrightarrow b` | `↔` (input aliases `⇔`) |
| `XOR` | $a \oplus b$ | `a \oplus b` | `⊕` (input alias `⊻`) |
| `NAND` | $a \uparrow b$ | `a \uparrow b` | `↑` (input alias `⊼`) |
| `NOR` | $a \downarrow b$ | `a \downarrow b` | `↓` (input alias `⊽`) |
| `COALESCE` | $\operatorname{COALESCE}(a, b)$ | `\operatorname{COALESCE}(a, b)` | `??` |
| `If` | $\operatorname{If}(c, t, f)$ | `\operatorname{If}(c, t, f)` | `c ? t : f` |

Operations without a symbol (`PARITY`, the cardinality operations, the inspections) are written as $\operatorname{Name}(\dots)$, with parameters as a subscript where that reads better: $\operatorname{AtLeast}_k(x_1, \dots, x_n)$, $\operatorname{Between}_{m,M}(x_1, \dots, x_n)$. The subscript is the Operation's own parameter, not a different Operation. Operation names are case-insensitive on input; the canonical spelling is shown in documents.

The symbol column is the accepted spelling on input. The canonical printer writes words only, so a symbol is never a persisted form.

## Orders

| Symbol | LaTeX | Meaning |
| --- | --- | --- |
| $\mathsf{F} < \mathsf{U} < \mathsf{T}$ | `\mathsf{F} < \mathsf{U} < \mathsf{T}` | The truth order. An implementation aid, not a numeric ordering |
| $\sqsubseteq$ | `\sqsubseteq` | The information order: $\mathsf{U} \sqsubseteq \mathsf{T}$ and $\mathsf{U} \sqsubseteq \mathsf{F}$. Applied to a tuple, it means position by position |
| $x \sqsubseteq y$ | `x \sqsubseteq y` | $y$ refines $x$: it replaces some `Unknown` values by `True` or `False` |

## Counts and intervals

| Symbol | LaTeX | Meaning |
| --- | --- | --- |
| $d$ | `d` | Definitely true count: the number of operands equal to $\mathsf{T}$ |
| $p$ | `p` | Possibly true count: the number of operands equal to $\mathsf{T}$ or $\mathsf{U}$ |
| $[d, p]$ | `[d, p]` | The interval of possible true counts, with $0 \le d \le p \le n$ |
| $d \bmod 2$ | `d \bmod 2` | Parity of the definite count, used by `PARITY` |

## Equality, equivalence and extensions

| Symbol | LaTeX | Meaning |
| --- | --- | --- |
| $=$ | `=` | Two expressions have the same value under every assignment of $\mathsf{T}$, $\mathsf{F}$, $\mathsf{U}$ to their variables. Every law in the reference is stated this way |
| $\ne$ | `\ne` | At least one assignment gives different values; a counterexample is shown |
| $g^{*}$ | `g^{*}` | The strongest extension of a Boolean function $g$ (see [semantics](semantics.md#truth-functional-evaluation-and-the-strongest-extension)) |
| $\mapsto$ | `\mapsto` | "Is sent to", used when a single input is shown |
| $\Rightarrow$ | `\Rightarrow` | Meta-level "if ... then", used in statements about functions such as monotonicity. It is not the object-language `IMPLIES` |
| $\text{cases}$ | `\begin{cases} ... \end{cases}` | A definition by cases |

> [!NOTE]
> The quantifiers $\exists$ and $\forall$ are not used for `ANY` and `ALL`. Those are cardinality Operations over a fixed list of operands, not quantifiers over a domain.

## Result transformations

| Symbol | LaTeX | Meaning |
| --- | --- | --- |
| $\operatorname{Project}_{v}(a)$ | `\operatorname{Project}_{v}(a)` | Keeps $\mathsf{T}$ and $\mathsf{F}$ and replaces $\mathsf{U}$ with the definite value $v$ |
| $\operatorname{Collapse}_{\pi}(a)$ | `\operatorname{Collapse}_{\pi}(a)` | Turns a value into a collapse outcome under the policy $\pi$ (`UnknownAsFalse`, `UnknownAsTrue`, `UnknownIsError`) |

## Code conventions

- Plain-text forms use the function-call spelling, never infix: `OR(NOT(a), b)`, `ATLEAST(k + 1, ...)`. A canonical-form block names its operands (`a`, `b`, ...) and the Operation's parameters (`k`, `min`, `max`); `...` stands for every operand.
- Parameters come before operands in a call, as in the DSL: `AtLeast(2, a, b, c)`.
- Tables list values as `T`, `F`, `U` and results in the same alphabet. Evaluation tables for parameterised and variadic Operations list the definitely true count, the possibly true count and the result.
- Rows of a truth table follow the order `T`, `U`, `F` for each operand, so a table reads from the most true assignment to the least.

## Related

- [values](values.md), [semantics](semantics.md) and [terminology](terminology.md).
- [docs/doc-examples.md](../../doc-examples.md) describes the checked markers.
