# Diagnostics

The compile-time diagnostics that the Operations produce. A rule that does not compile never throws. The compiler returns a result that carries its diagnostics. Each diagnostic has a stable code, a severity, a message, and a span in rule text or a path in JSON and YAML. Back to the [specification index](README.md).

A diagnostic with the severity Error blocks compilation. A Warning or an Info does not.

One mistake has one code in every format. A rule written as rule text, JSON, YAML or with `RuleBuilder` gets the same code for the same mistake. `TRE0014` is only for the shape of a JSON or YAML tree. The table below lists every code once. The message text is not part of the contract: a message may add detail such as the offending value.

## Code table

The Phase column names the stage of the compile pipeline that reports the code: Parse, Validate, Analyze or Build. Rewrite is a rewrite of a compiled rule, and Lint is an opt-in lint that runs after the analysis. The Default column says whether the compiler reports the code without any option or only when the matching `LintRules` flag is set in the compiler options.

| Code | Name | Severity | Phase | Default | Cause and fix |
| --- | --- | --- | --- | --- | --- |
| `TRE0001` | `SyntaxError` | Error | Parse | On | The rule text does not parse. Examples: a call form for an infix Operation (`XOR(a, b)`), a lone `&` or `\|`, an unclosed or mismatched delimiter, a missing operand. Write the form that [syntax](syntax.md) shows for the Operation. |
| `TRE0002` | `UnknownPredicate` | Error | Parse, Validate | On | A term names a predicate that is not registered, or a JSON or YAML `op` or a call names an Operation that does not exist. `Project`, `Collapse` and `NXOR` are not Operations, so they get this code too, with a hint to the replacement. |
| `TRE0003` | `MissingArgument` | Error | Validate | On | A term omits a required argument of its predicate. |
| `TRE0004` | `ArgumentTypeMismatch` | Error | Validate | On | An argument value has the wrong kind for the predicate. |
| `TRE0005` | `UnknownArgument` | Error | Validate | On | A term names an argument that the predicate does not declare. |
| `TRE0006` | `InfixArityViolation` | Error | Validate | On | An Operation has an operand count it does not accept. `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND` and `NOR` take two (a chain such as `a XOR b XOR c` is the usual cause), `NOT` and the inspections take one, `If` takes three, and the n-ary Operations and the threshold family need their minimum. Use the operand count that the page of the Operation gives, add parentheses, or use `PARITY` or `ExactlyOne` for more than two. The name is historical. |
| `TRE0007` | `AmbiguousOperatorMixing` | Error | Parse | On | An infix Operation other than `NOT`, `AND` and `OR`, or a `??` or ternary form, shares a level with `AND`, `OR` or another such form without parentheses. Add parentheses around the expression that applies first. |
| `TRE0008` | `InvalidThresholdValue` | Error | Parse, Validate | On | A threshold `k`, or the bounds of `BETWEEN`, are not whole numbers, or make the result a constant for the operand count. Examples: `AtLeast(3, a, b)`, `AtMost(1, a)`, `AtLeast(1.5, a, b)` and `BETWEEN(0, 2, a, b)`. Use a whole-number `k` in the valid range for the operand count. The range is on the page of each Operation. |
| `TRE0009` | `MaxDepthExceeded` | Error | Build | On | The expression tree is deeper than the compiler option `MaxDepth` allows. |
| `TRE0010` | `MaxNodeCountExceeded` | Error | Build | On | The expression tree has more nodes than the compiler option `MaxNodeCount` allows. |
| `TRE0011` | `AnalysisSkippedTooManyTerms` | Info | Analyze | On | The analysis was skipped because the rule has more terms than the compiler option `MaxAnalysisTerms` allows. The rule still compiles. |
| `TRE0012` | `StructuralTautology` | Warning | Analyze | On | A sub-expression is `True` for every assignment of `True`, `False` and `Unknown` to its terms. `a OR NOT a` is not one, because it is `Unknown` when `a` is. |
| `TRE0013` | `StructuralContradiction` | Warning | Analyze | On | A sub-expression is `False` for every assignment. `a AND NOT a` is not one, because it is `Unknown` when `a` is. |
| `TRE0014` | `MalformedTree` | Error | Parse | On | The shape of a JSON or YAML tree is wrong: a missing key (`const`, `predicate` or `op`, or `operands`), a key that the node does not define (such as a `k` on `AND`, or a `predicate` next to an `op`), a value of the wrong kind, or text that is not well-formed JSON or YAML. Give the node the keys of its kind and remove any other key. |
| `TRE0015` | `InvalidEscapeSequence` | Error | Parse | On | A string literal contains a backslash that is not `\"`, `\\`, `\n` or `\t`. |
| `TRE0016` | `RewriteTooLarge` | Error | Rewrite | On | An expanding rewrite would produce more nodes than the compiler option `MaxRewriteNodeCount` allows, so the compiler does not perform it. |
| `TRE0017` | `RedundantInspection` | Info | Lint | Opt-in (`LintRules.RedundantInspection`) | An inspection (`IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown`) has an operand that can never be `Unknown`, or never be known. |
| `TRE0018` | `RedundantCoalesce` | Info | Lint | Opt-in (`LintRules.RedundantCoalesce`) | A `COALESCE` operand can never be `Unknown`, so the operands after it are never reached. |
| `TRE0019` | `ConstantIfCondition` | Info | Lint | Opt-in (`LintRules.ConstantIfCondition`) | The condition of an `If` is always `True` or always `False`, so one branch is never chosen. |
| `TRE0020` | `IdenticalIfBranches` | Info | Lint | Opt-in (`LintRules.IdenticalIfBranches`) | An `If` has two equal branches, so its condition does not affect the result. |
| `TRE0021` | `VacuousCardinality` | Info | Lint | Opt-in (`LintRules.VacuousCardinality`) | The constant operands of a threshold or `BETWEEN` fix its value whatever the other operands are. |
| `TRE0022` | `DuplicateOperands` | Info | Lint | Opt-in (`LintRules.DuplicateOperands`) | An operand repeats inside `AND`, `OR`, `ANY`, `ALL` or `COALESCE`, and the repeat adds nothing. |
| `TRE0023` | `DoubleNegation` | Info | Lint | Opt-in (`LintRules.DoubleNegation`) | `NOT (NOT x)`, which is `x`. |
| `TRE0024` | `UndeclaredDataSource` | Error | Validate | On | A variable reference names a data source that is not declared in the compiler options. |
| `TRE0025` | `MalformedDataQuery` | Error | Validate | On | The query of a variable reference is not valid for the dialect of its data source. |
| `TRE0026` | `InvalidArgumentValue` | Error | Validate | On | The literal argument values of a predicate call break a rule that the predicate checks at compile time, for example reversed bounds on `Between` or `Outside`. |
| `TRE0027` | `DeprecatedPredicate` | Warning | Validate | On | A rule uses a predicate whose schema is marked deprecated. This is a warning, one per use, and the rule still compiles. |
| `TRE0028` | `DeepNesting` | Info | Lint | Opt-in (`LintRules.DeepNesting`) | The rule is at least `DeepNestingFraction` of `MaxDepth` levels deep. This finding has no suggestion. |
| `TRE0029` | `WideChain` | Info | Lint | Opt-in (`LintRules.WideChain`) | An `AND` or `OR` has more operands than `WideChainOperandLimit`. This finding has no suggestion. |
| `TRE0030` | `NotCanonical` | Info | Lint | Opt-in (`LintRules.NotCanonical`) | `Canonicalize()` would change the rule. The suggestion is the canonical rule text. |
| `TRE0031` | `ThresholdKeptAsAtom` | Warning | Rewrite | On | `ToNnf()`, `ToCnf()` or `ToDnf()` kept a threshold as an atom, because expanding it can grow the rule a lot. The message gives the growth estimate. Pass `NormalFormOptions` with `ExpandThresholds` set to `true` to expand it. |
| `TRE0032` | `DuplicateArgument` | Error | Validate | On | A term names the same argument more than once, in rule text, JSON, YAML or `RuleBuilder`. No occurrence wins. |

A lint finding carries a suggested replacement.

## Related

- [syntax](syntax.md) states the grammar rules that these diagnostics enforce.
- [operations](operations.md) lists the operand counts of every Operation.
