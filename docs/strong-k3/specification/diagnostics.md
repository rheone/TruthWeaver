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
| `TRE0009` | `MaxDepthExceeded` | Error | Parse, Build | On | The expression tree is deeper than the compiler option `MaxDepth` allows. Rule text and a JSON or YAML tree report it at Parse, before the tree is fully read, once the nesting written in the source (parenthesized groups, a `NOT` chain, nested calls) is already deep enough that reading the rest would recurse too far. `RuleNodeCompiler.Build` reports it again at Build for a tree that only becomes this deep after expansion, such as a chain of `let` uses (see the known limitation below). Either way, no rule is produced. |
| `TRE0010` | `MaxNodeCountExceeded` | Error | Parse, Build | On | The expression tree has more nodes than the compiler option `MaxNodeCount` allows. `let` expansion counts the nodes it produces against the same option and reports this code as soon as the count is exceeded, rather than finishing a high-fanout expansion first: a chain of definitions well within `MaxDefinitionDepth`, each one `AND`-ing many uses of the one below, grows the same way whether or not the finished tree would ever reach `RuleNodeCompiler.Build`'s own count. Reading a JSON or YAML tree counts nodes the same way: a YAML anchor aliased several times is walked again in full on every reference, so a document only a few levels deep can still expand past `MaxNodeCount`, and this is reported at Parse rather than waiting for `RuleNodeCompiler.Build`. |
| `TRE0011` | `AnalysisSkippedTooManyTerms` | Info | Analyze | On | The analysis was skipped because the rule has more terms than the compiler option `MaxAnalysisTerms` allows. The rule still compiles. |
| `TRE0012` | `StructuralTautology` | Warning | Analyze | On | A sub-expression is `True` for every assignment of `True`, `False` and `Unknown` to its terms. `a OR NOT a` is not one, because it is `Unknown` when `a` is. |
| `TRE0013` | `StructuralContradiction` | Warning | Analyze | On | A sub-expression is `False` for every assignment. `a AND NOT a` is not one, because it is `Unknown` when `a` is. |
| `TRE0014` | `MalformedTree` | Error | Parse | On | The shape of a JSON or YAML tree is wrong: a missing key (`const`, `predicate` or `op`, or `operands`), a key that the node does not define (such as a `k` on `AND`, or a `predicate` next to an `op`), a key written twice in one node (such as two `op` keys), a value of the wrong kind, text that is not well-formed JSON or YAML, JSON text that is not valid UTF-16 (a lone surrogate), YAML text with more than one document, or a YAML alias that refers to one of its own ancestors (so the document is cyclic rather than a tree, such as `a: &x [*x]`). Give the node the keys of its kind, each once, remove any other key, and remove the cyclic alias. |
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
| `TRE0026` | `InvalidArgumentValue` | Error | Validate | On | The literal argument values of a predicate call break a rule that the predicate checks at compile time, for example reversed bounds on `IsBetween` or `IsNotBetween`. |
| `TRE0027` | `DeprecatedPredicate` | Warning | Validate | On | A rule uses a predicate whose schema is marked deprecated. This is a warning, one per use, and the rule still compiles. |
| `TRE0028` | `DeepNesting` | Info | Lint | Opt-in (`LintRules.DeepNesting`) | The rule is at least `DeepNestingFraction` of `MaxDepth` levels deep. This finding has no suggestion. |
| `TRE0029` | `WideChain` | Info | Lint | Opt-in (`LintRules.WideChain`) | An `AND` or `OR` has more operands than `WideChainOperandLimit`. This finding has no suggestion. |
| `TRE0030` | `NotCanonical` | Info | Lint | Opt-in (`LintRules.Style`) | `Canonicalize()` would change the rule. The suggestion is the canonical rule text. |
| `TRE0031` | `ThresholdKeptAsAtom` | Warning | Rewrite | On | `ToNnf()`, `ToCnf()` or `ToDnf()` kept a threshold as an atom, because expanding it can grow the rule a lot. The message gives the growth estimate. Pass `NormalFormOptions` with `ExpandThresholds` set to `true` to expand it. |
| `TRE0032` | `DuplicateArgument` | Error | Validate | On | A term names the same argument more than once, in rule text, JSON, YAML or `RuleBuilder`. No occurrence wins. |
| `TRE0033` | `RewriteNotConverged` | Warning | Rewrite | On | `CompressToDerived()`, `Canonicalize()` or `Simplify()` stopped at its pass limit before the rule stopped changing. The rule is equivalent to the input, but rewriting it again can still change it. |
| `TRE0034` | `TruthTableCapRaised` | Warning | Print | On | `PrintTruthTable` printed a table with more terms than the default cap of 8, because the options set `AllowLargerTable`. The message gives the row count. |
| `TRE0035` | `TruthTableTooLarge` | Error | Print | On | `PrintTruthTable` refused to print a table: the rule has more terms than the default cap of 8 and `AllowLargerTable` is not set, or it has more terms than `TruthTableOptions.TermLimit` (12 by default). Nothing is written. |
| `TRE0036` | `DefinitionCycleDetected` | Error | Validate | On | A `let` definition references itself, directly or through a chain of other definitions' uses. The message names every definition in the cycle. Remove the self- or mutual reference. |
| `TRE0037` | `DefinitionNameCollision` | Error | Validate | On | A `let` definition's name clashes with another definition already declared in the same document, a reserved word, or a predicate name registered with the compiler, or one of its parameters is a reserved word. Rename the definition or the parameter. |
| `TRE0038` | `MaxDefinitionDepthExceeded` | Error | Validate | On | A use would expand through more `let` definitions than the compiler option `MaxDefinitionDepth` allows, independent of `MaxDepthExceeded` and `DefinitionCycleDetected`. Shorten the chain of definitions or raise the option. |
| `TRE0039` | `DefinitionArgumentCountMismatch` | Error | Validate | On | A use of a `let` definition supplies a different number of arguments than the definition declares parameters. Match the use to the definition's parameter list. |
| `TRE0040` | `UnknownDefinition` | Error | Validate | On | A use names a `let` definition that was never declared (or was declared but discarded because its name collided; see `DefinitionNameCollision`). Declare the definition, or fix its name. |
| `TRE0041` | `UnusedDefinition` | Info | Lint | Opt-in (`LintRules.UnusedDefinition`) | A `let` definition is declared but never used anywhere in the document. |
| `TRE0042` | `UnknownCollection` | Error | Validate | On | A `FORALL`/`EXISTS` head names a collection that no `PredicateRegistryBuilder.AddCollection` call registered. The message suggests the nearest registered collection name. |
| `TRE0043` | `MaxQuantifierDepthExceeded` | Error | Validate | On | A rule nests `FORALL`/`EXISTS` quantifiers deeper than the compiler option `MaxQuantifierDepth` allows. |
| `TRE0044` | `ShadowedVariable` | Error | Validate | On | A nested `FORALL`/`EXISTS` reuses the bound variable name of an enclosing quantifier. Rename the inner quantifier's variable. |
| `TRE0045` | `AnalysisSkippedNodeBudgetExceeded` | Info | Analyze | On | The analysis was skipped because building its decision diagram would exceed the analyzer's internal node budget, even though the rule's term count is within `MaxAnalysisTerms`. The rule still compiles. |
| `TRE0046` | `DuplicateDefinitionParameter` | Error | Validate | On | A `let` definition's own parameter list names the same parameter twice (compared case-insensitively), in rule text, JSON, YAML or `RuleBuilder`. Binding a use's arguments by parameter name would otherwise let a later duplicate silently overwrite an earlier one. Rename one of the repeated parameters. |

A lint finding carries a suggested replacement.

**Known limitation: `MaxDepthExceeded`/`MaxNodeCountExceeded` inside a `let` expansion.** A diagnostic raised
inside a `let` definition's expansion should point at the use site and additionally name the definition and its
own declaration span. `TRE0036`-`TRE0040` (the definition guardrails themselves) already do this, located at the
use's own name. `TRE0009` `MaxDepthExceeded` and `TRE0010` `MaxNodeCountExceeded`, when the node that tripped the
limit came from inside a definition's body, do not: the span is the node's position inside the `let` declaration
that wrote it, not the use site that expanded it there, and the message does not separately name the definition.
A node that came from an argument at the use site, rather than from the body's own text, already carries that
argument's real span, which happens to be at or near the use; this limitation is specific to nodes written inside
the definition's own body. Closing it needs the expander to carry each expanded node's originating use site
alongside its existing span, threaded through every diagnostic `RuleNodeCompiler` can raise on an expanded node,
not only these two, so it stays a documented limitation for now rather than a partial fix. No `Diagnostic` public
API change is needed to close it later: the use site and definition identity can go in the message text, as the
rest of this page's diagnostics already do for detail that is not part of the stable code.

## Related

- [syntax](syntax.md) states the grammar rules that these diagnostics enforce.
- [operations](operations.md) lists the operand counts of every Operation.
