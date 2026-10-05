# Diagnostics

The compile-time diagnostics that the Operations produce. A rule that does not compile never throws. The compiler returns a result that carries its diagnostics. Each diagnostic has a stable code, a severity, a message, and a span in rule text or a path in JSON and YAML. Back to the [specification index](README.md).

A diagnostic with the severity Error blocks compilation. A Warning or an Info does not. The tables name each code and its cause. The message text is not part of the contract: a message may add detail such as the offending value.

## Errors in an Operation

| Code | Name | Cause | Fix |
| --- | --- | --- | --- |
| `TRE0001` | `SyntaxError` | The rule text does not parse. Examples: a call form for an infix Operation (`XOR(a, b)`), a threshold call without an integer first argument, a lone `&` or `\|`, an unclosed or mismatched delimiter, `Project` or `Collapse` in a rule, `NXOR`. | Write the form that [syntax](syntax.md) shows for the Operation. |
| `TRE0006` | `InfixArityViolation` | `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND` or `NOR` has other than two operands, usually a chain such as `a XOR b XOR c`. | Use two operands, add parentheses, or use `PARITY` or `ExactlyOne` for more than two. |
| `TRE0007` | `AmbiguousOperatorMixing` | An infix Operation other than `NOT`, `AND` and `OR`, or a `??` or ternary form, shares a level with `AND`, `OR` or another such form without parentheses. | Add parentheses around the expression that applies first. |
| `TRE0008` | `InvalidThresholdValue` | A threshold `k`, or the bounds of `BETWEEN`, make the result a constant for the operand count. For example `AtLeast(3, a, b)`, `AtMost(1, a)` and `BETWEEN(0, 2, a, b)`. | Use a `k` in the valid range for the operand count. The range is on the page of each Operation. |
| `TRE0014` | `MalformedTree` | The operand count is wrong for an Operation other than the binary-only ones, a JSON or YAML rule has an unknown `op` or a missing key, or `Project` or `Collapse` appears in a JSON or YAML rule. | Give the Operation its operand count and its required keys. |

## Analysis findings

The analyzer reports `TRE0012` and `TRE0013` as warnings and `TRE0011` as information.

| Code | Name | Cause |
| --- | --- | --- |
| `TRE0011` | `AnalysisSkippedTooManyTerms` | The analysis was skipped because the rule has more terms than the compiler option `MaxAnalysisTerms` allows. |
| `TRE0012` | `StructuralTautology` | A sub-expression is `True` for every assignment of `True`, `False` and `Unknown` to its terms. `a OR NOT a` is not one, because it is `Unknown` when `a` is. |
| `TRE0013` | `StructuralContradiction` | A sub-expression is `False` for every assignment. `a AND NOT a` is not one, because it is `Unknown` when `a` is. |

## Lint findings

The compiler reports these as information when the matching lint rule is enabled in the compiler options. Each finding carries a suggested replacement.

| Code | Name | Cause |
| --- | --- | --- |
| `TRE0017` | `RedundantInspection` | An inspection (`IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown`) has an operand that can never be `Unknown`, or never be known. |
| `TRE0018` | `RedundantCoalesce` | A `COALESCE` operand can never be `Unknown`, so the operands after it are never reached. |
| `TRE0019` | `ConstantIfCondition` | The condition of an `If` is always `True` or always `False`, so one branch is never chosen. |
| `TRE0020` | `IdenticalIfBranches` | An `If` has two equal branches, so its condition does not affect the result. |
| `TRE0021` | `VacuousCardinality` | The constant operands of a threshold or `BETWEEN` fix its value whatever the other operands are. |
| `TRE0022` | `DuplicateOperands` | An operand repeats inside `AND`, `OR`, `ANY`, `ALL` or `COALESCE`, and the repeat adds nothing. |
| `TRE0023` | `DoubleNegation` | `NOT (NOT x)`, which is `x`. |

## Codes outside the Operations

These codes do not belong to one Operation.

| Code | Name | Cause |
| --- | --- | --- |
| `TRE0002` | `UnknownPredicate` | A term names a predicate that is not registered. |
| `TRE0003` | `MissingArgument` | A term omits a required argument of its predicate. |
| `TRE0004` | `ArgumentTypeMismatch` | An argument value has the wrong kind for the predicate. |
| `TRE0005` | `UnknownArgument` | A term names an argument that the predicate does not declare. |
| `TRE0009` | `MaxDepthExceeded` | The expression tree is deeper than the compiler option `MaxDepth` allows. |
| `TRE0010` | `MaxNodeCountExceeded` | The expression tree has more nodes than the compiler option `MaxNodeCount` allows. |
| `TRE0015` | `InvalidEscapeSequence` | A string literal contains a backslash that is not `\"`, `\\`, `\n` or `\t`. |
| `TRE0016` | `RewriteTooLarge` | An expanding rewrite would produce more nodes than the compiler option `MaxRewriteNodeCount` allows, so the compiler does not perform it. |
| `TRE0024` | `UndeclaredDataSource` | A variable reference names a data source that is not declared in the compiler options. |
| `TRE0025` | `MalformedDataQuery` | The query of a variable reference is not valid for the dialect of its data source. |
| `TRE0026` | `InvalidArgumentValue` | The literal argument values of a predicate call break a rule that the predicate checks at compile time, for example reversed bounds on `Between` or `Outside`. |

## Related

- [syntax](syntax.md) states the grammar rules that these diagnostics enforce.
- [operations](operations.md) lists the operand counts of every Operation.
