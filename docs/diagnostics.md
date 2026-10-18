# Reading diagnostics

A rule that does not compile never throws. `Compile` returns a `CompilationResult<TContext>`, and its `Diagnostics` explain what is wrong. Each `Diagnostic` is structured data first and text second. An editor can lay it out itself, and a log can print it as it is. Back to the [README](../README.md).

For the meaning, cause and fix of each code, see the [diagnostic code catalog](strong-k3/specification/diagnostics.md). The catalog is one table. It lists every code with its severity, its phase and whether the compiler reports it by default or only on request. One mistake has one code in rule text, JSON, YAML and `RuleBuilder`.

## Diagnostic members

| Member               | Meaning                                                                                                       |
| -------------------- | ------------------------------------------------------------------------------------------------------------- |
| `Code`               | Stable identifier such as `TRE0001` (see `DiagnosticCodes`).                                                  |
| `Severity`           | `Error` blocks compilation. `Warning` and `Info` do not.                                                      |
| `Message`            | A plain-language explanation of the problem.                                                                  |
| `Span`               | Where the problem is in the rule text (0-based offset and length). `Span.GetLocation(source)` gives line and column. |
| `Path`               | Where the problem is in a JSON or YAML rule, or in a `RuleBuilder` tree, for example `$.operands[1].op`. It is `null` for rule text. A `RuleBuilder` diagnostic carries a path but `Span.None`: see [Compiling a builder](rulebuilder.md#compiling-a-builder). |
| `Expected` / `Found` | What the compiler needed and what it saw (`')'` and `']'`, `2 operands` and `3 operands`), when that applies. |
| `Suggestion`         | A `DiagnosticSuggestion`: a `Replacement` ("did you mean `AND`?") or a `Hint` (advice such as adding parentheses). |

## Reading a diagnostic in code

```csharp
const string source = "a ANDD b";
CompilationResult<MyContext> result = compiler.Compile(source);

foreach (Diagnostic d in result.Diagnostics)
{
    SourceLocation at = d.Span.GetLocation(source);          // line 1, column 3
    Console.WriteLine($"{d.Code} {at.Line}:{at.Column} {d.Message}");
    Console.WriteLine($"expected {d.Expected}, found {d.Found}, try {d.Suggestion?.Text}");
}

// Or render everything as plain text for a log or an editor panel.
Console.WriteLine(result.FormatDiagnostics(source));
```

<!-- doctest:diagnostics-dsl a ANDD b -->
```text
TRE0001 error at line 1, column 3: Unexpected token 'ANDD' after end of expression.
  a ANDD b
    ^^^^
  Expected: an operator or the end of the rule
  Found: 'ANDD'
  Did you mean: AND
```

## Getting the rule out of a result

Every method that returns a `CompilationResult<TContext>` has a `Try` form with the same inputs plus two `out` parameters:
`TryCompile`, `TryCompileText`, `TryCompileJson`, `TryCompileYaml`, `RuleBuilder.TryCompile`, `TrySimplify`, `TryCanonicalize`,
`TryCompressToDerived`, `TryExpandToPrimitives`, `TryExpandToNand`, `TryExpandToNor`, `TryToNnf`, `TryToCnf` and `TryToDnf`.
The method returns `true` exactly when `rule` is not `null`, so no `!` is needed. A result that has only warnings returns
`true`, and `diagnostics` holds the warnings. `diagnostics` is set in both cases.

```csharp
if (!compiler.TryCompile(source, out CompiledRule<MyContext>? rule, out EquatableArray<Diagnostic> diagnostics))
{
    return;
}
```

`CompilationResult<TContext>.TryGetRule` does the same for a result you already hold. `CompilationResult<TContext>` itself
holds the compiled rule only when compilation succeeded. After a `Succeeded` check, `Rule` is not `null`:

```csharp
CompilationResult<MyContext> result = compiler.Compile(source);
if (!result.Succeeded)
{
    return;
}

CompiledRule<MyContext> rule = result.Rule;
```

Where a rule that does not compile is a bug, such as at startup or in a test, `GetRuleOrThrow()` returns the rule or throws an
`InvalidOperationException`. The message lists each `Error` diagnostic with its code and text. It does not list warnings or
info findings, and it does not quote the rule text.

```csharp
CompiledRule<MyContext> rule = compiler.Compile("isAdmin").GetRuleOrThrow();
```

`DiagnosticFormatter.Format(diagnostic, source)` renders a single diagnostic. Without the source text, the header shows `at offset 2` and the source line is left out.

## "Did you mean" suggestions

A suggestion comes from a small, deterministic edit distance. The comparison ignores case and counts a swapped pair of letters as one edit. The cut-off grows with the length of the word. The candidates are the operators, aliases, reserved words and the predicate names in your registry.

When several candidates are equally close, the ordinally first one wins, so the same typo always gets the same answer. A word that is far from everything known gets no suggestion.

Suggestions cover these cases:

- unknown predicate and operator names
- undeclared predicate argument names
- a lone `&` or `|`

## Lint rules (opt-in)

The compiler can flag constructs that are redundant under Strong K3 and say what to write instead. This is in addition to the tautology and contradiction warnings. The lints are off by default, so a rule that compiled clean before keeps compiling clean. Switch them on with `CompilerOptions.Lints`:

```csharp
RuleCompiler<MyContext> compiler = new(registry, new CompilerOptions(Lints: LintRules.All));
CompilationResult<MyContext> result = compiler.Compile("NOT NOT isAdmin");
// TRE0023 info: a negation of a negation cancels out ... Did you mean: isAdmin
```

`LintRules.All` is a quiet first run: a rule that is valid and not redundant, such as `b AND a`, gets no finding. Add the `Style` flag (`LintRules.All | LintRules.Style`) to also hear about a rule that `Canonicalize()` would change.

Each finding is an `Info` diagnostic, so it never blocks compilation. It has a `Replacement` suggestion that holds the simpler rule text. The message gives the Strong K3 reason why the replacement means the same.

A finding has no source span, because the compiled tree does not keep the place where a node was written. The message and `Found` show the construct instead.

Every suggestion is the same rule as the original for every `True`, `False` and `Unknown` input, as `RuleEquivalence` confirms. A lint does not fire on a two-valued intuition that Strong K3 does not share. It leaves `a XOR a`, `a AND NOT a` and a repeated `PARITY` operand alone.

| `LintRules` flag      | Code      | Flags                                                                                                                         | Suggests                                         |
| --------------------- | --------- | ----------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------ |
| `RedundantInspection` | `TRE0017` | `IsTrue`, `IsFalse`, `IsUnknown` or `IsKnown` over an operand that can never be `Unknown`, or never be known (`IsKnown(IsTrue(a))`) | `True`, `False`, the operand, or its negation    |
| `RedundantCoalesce`   | `TRE0018` | a `COALESCE` operand that can never be `Unknown`, so the operands after it are unreachable                                    | the operands up to and including it              |
| `ConstantIfCondition` | `TRE0019` | an `If` whose condition is always `True` or always `False`                                                                    | the branch that is always chosen                 |
| `IdenticalIfBranches` | `TRE0020` | `If(c, t, t)`                                                                                                                 | `t`                                              |
| `VacuousCardinality`  | `TRE0021` | a threshold or `BETWEEN` whose constant operands already fix the result (`AtLeast(1, a, TRUE)`)                               | the constant                                     |
| `DuplicateOperands`   | `TRE0022` | a structurally identical operand repeated inside `AND`, `OR`, `ANY`, `ALL` or `COALESCE`                                      | the operator with each operand once              |
| `DoubleNegation`      | `TRE0023` | `NOT NOT x`                                                                                                                   | `x`                                              |
| `DeepNesting`         | `TRE0028` | a rule whose depth reaches `DeepNestingFraction` of `MaxDepth`                                                                | none                                             |
| `WideChain`           | `TRE0029` | an `AND` or `OR` chain with more than `WideChainOperandLimit` operands                                                        | none                                             |
| `Style`               | `TRE0030` | a rule that `Canonicalize()` would change (`NotCanonical`)                                                                  | the canonical rule text                          |

`LintRules` is a flags enum. Combine the flags you want (`LintRules.DuplicateOperands | LintRules.DoubleNegation`). `LintRules.All` switches on every flag except `Style`, so a first run reports likely mistakes and stays quiet about a rule that is valid but written in its own order. `LintRules.All | LintRules.Style` switches on every lint.

`DeepNesting`, `WideChain` and `Style` do not propose an equivalent shorter rule by logic. `DeepNesting` and `WideChain` give no suggestion. `Style` suggests the output of [`Canonicalize()`](rewriting-rules.md#canonical-form). It does not run on a rule larger than `CompilerOptions.MaxRewriteNodeCount`. `Canonicalize()` itself has no size limit, so no finding for a large rule does not show that the rule is canonical.

Two `CompilerOptions` values set the thresholds:

| Option | Default | Effect |
| --- | --- | --- |
| `DeepNestingFraction` | `0.75` | `DeepNesting` reports a rule whose depth is at least this share of `MaxDepth`. With the default `MaxDepth` of 32, that is 24 levels. |
| `WideChainOperandLimit` | `16` | `WideChain` reports an `AND` or `OR` with more operands than this. A chain of 17 operands is reported. |

A rule under both thresholds produces no `DeepNesting` or `WideChain` finding.

### Related findings

One redundant construct can produce several findings. For example, an `If` whose condition holds a redundant inspection gets a finding for the `If` and a finding for the inspection. Every finding is reported. `Diagnostic.EnclosedBy` links a finding to the nearest finding whose construct contains it, so a UI can group the inner findings under the outermost one. It is `null` for a finding that stands alone, such as a finding in a separate branch of the rule. The findings keep the order "outermost construct first". `DeepNesting` and `Style` describe the whole rule, so they are never linked.

The semantic lints (`TRE0017` to `TRE0019` and `TRE0021`) use the analyzer's BDD. They are skipped for a sub-expression with more than `CompilerOptions.MaxAnalysisTerms` distinct terms. The structural lints (`TRE0020`, `TRE0022` and `TRE0023`) always run. The [diagnostic code catalog](strong-k3/specification/diagnostics.md) lists each code.

## Term independence

The analyzer treats each distinct term as an independent variable. Two terms are the same variable only when their predicate name and arguments match. It cannot know that two different predicates read the same value, or that `ageAtLeast(min: 21)` being `True` makes `ageAtLeast(min: 18)` `True`.

The effect on the results:

- **Tautology and contradiction warnings stay sound, and some are missed.** A sub-expression is reported only when it is `True` (or `False`) for every assignment, including the assignments that cannot occur. A rule that is constant only because two terms are related is not reported.
- **A counter-example, an overlap pair or a lint finding can name an assignment that cannot occur.** Two terms that read the same missing value are both `Unknown` at run time, but the analyzer also allows `A = True` with `B = Unknown`. A pair that is not reachable is a proof. A reachable pair is a candidate only. See [Overlap and conflict](rewriting-rules.md#overlap-and-conflict) and [Counterfactuals](counterfactuals.md).
- **A term that never answers `Unknown` counts as possibly `Unknown`.** `IsNull`, and any predicate under `NullBehavior.False`, cannot answer `Unknown`, but the analyzer does not know that. `IsUnknown(IsNull(x))` is therefore not linted.

A host can reduce the noise in three ways. Use one predicate for a condition that two terms would express, so the analyzer sees one variable. Compare rules only when their terms are unrelated, or treat a reachable pair as a question for a person. Test the cases that matter with [rule tests](rule-tests.md), which evaluate real assignments.

## Limits

Each limit has one setting and one outcome. The compile limits are `CompilerOptions` values. The evaluation limits are `EvaluationOptions` values.

| Limit | Default | When it is exceeded |
| --- | --- | --- |
| `CompilerOptions.MaxDepth` | 32 | `Compile` returns a `TRE0009` error and no rule. |
| `CompilerOptions.MaxNodeCount` | 512 | `Compile` returns a `TRE0010` error and no rule. |
| `CompilerOptions.MaxAnalysisTerms` | 20 | The analysis is skipped and `Compile` adds a `TRE0011` info diagnostic. The rule compiles. The semantic lints skip the sub-expression. `RuleEquivalence.Compare` and `RuleDiff.Compare` return an undecided result. `AssertEquivalent` and `AssertSound` fail as inconclusive. |
| `CompilerOptions.MaxRewriteNodeCount` | 100,000 | An expanding rewrite or normal form returns a failed `CompilationResult` with a `TRE0016` error and no rule. A result under this cap can still be over `MaxNodeCount`, so its text compiles back only when `MaxNodeCount` is raised. See [A large result may not recompile](rewriting-rules.md#a-large-result-may-not-recompile). |
| Rewrite pass limit (16 for `CompressToDerived()`, 8 for `Canonicalize()`, 16 for `Simplify()`) | fixed | The rewrite returns its equivalent rule and a `TRE0033` warning. See [Pass limits](rewriting-rules.md#pass-limits). |
| `TruthTableOptions.DefaultTermCap` and `TruthTableOptions.HardTermLimit` | 8 and 12 | `PrintTruthTable` refuses a table over the cap with a `TRE0035` error, unless `AllowLargerTable` is set, and then it adds a `TRE0034` warning. Over the hard limit it always refuses. See [Size limits](truth-tables.md#size-limits). |
| `EvaluationOptions.FaultBudget` | none | The evaluation stops when the faults reach the budget. The terms not yet run are `Unknown` in the trace, so the result is usually `Unknown`. |
| `CompilerOptions.MaxQuantifierDepth` | 2 | `Compile` returns a `TRE0043` error and no rule. |
| `EvaluationOptions.MaxElementEvaluations` | 10,000 | The quantifier being evaluated is `Unknown`, one fault is recorded and the evaluation stops. The count covers every `FORALL` and `EXISTS` body run, so nested quantifiers multiply. |
| `EvaluationOptions.Timeout` | none | `EvaluateAsync` throws `OperationCanceledException`. It records no fault and gives no `Unknown`. |

The two caps on rewrites are independent: `MaxRewriteNodeCount` limits what a rewrite builds, and `MaxNodeCount` limits what the compiler reads. The [diagnostic code catalog](strong-k3/specification/diagnostics.md) lists each code.

## JSON and YAML rules

A malformed JSON or YAML rule is located by `Path` instead of by line and column. The path is the route from the document root to the offending key. It is written the same way for both formats: `$` is the root, `.name` is a key and `[n]` is a 0-based sequence item.

A YAML diagnostic also carries the `Span` of the offending node, so `FormatDiagnostics(yaml)` adds the line, the column and the source line. `CompileJson(string)` carries the span of the offending node in the same way; the compiler finds it by reading the text again, because `System.Text.Json` keeps no positions. `CompileJson(JsonElement)` carries no span, because its caller already holds the parsed element and the text it came from, if there ever was text, may be gone. `RuleBuilder.Compile` compiles through this element overload, so a builder diagnostic carries only its `Path` into the tree you built. Invalid JSON syntax carries the position that the parser reports.

```csharp
const string json = """{"op":"and","operands":[{"const":true},{"op":"orr","operands":[]}]}""";
Console.WriteLine(compiler.CompileJson(json).FormatDiagnostics(json));
```

<!-- doctest:diagnostics-json {"op":"and","operands":[{"const":true},{"op":"orr","operands":[]}]} -->
```text
TRE0002 error at $.operands[1].op (line 1, column 46): Unknown operator 'orr'.
  {"op":"and","operands":[{"const":true},{"op":"orr","operands":[]}]}
                                               ^^^^^
  Expected: a known operator
  Found: 'orr'
  Did you mean: or
```

An unknown `op` name gets the nearest operator in the spelling of the tree (`atLeast`, not `AtLeast`). An unknown predicate name gets the nearest registered predicate.

Where a field is wrong, the path points at the field (`$.k`, `$.policy`, `$.unknownAs`, `$.min`, `$.args.role`, `$.predicate`, `$.op` or `$.const`). A wrong operand count points at `.operands`. A missing key is reported at the node that should hold it.

Invalid JSON or YAML syntax is reported at the nearest valid ancestor, which is the innermost object or array that is still open. The diagnostic also has the position that the parser reports:

<!-- doctest:diagnostics-json {"op":"and","operands":[{"const":true}, -->
```text
TRE0014 error at $.operands (line 1, column 39): Malformed JSON: Expected start of a property name or value, but instead reached end of data. LineNumber: 0 | BytePositionInLine: 38.
  {"op":"and","operands":[{"const":true},
                                        ^
  Expected: well-formed JSON
  Found: Expected start of a property name or value, but instead reached end of data. LineNumber: 0 | BytePositionInLine: 38.
```

## Classes of malformed rule text

This table shows how each class of malformed rule text is reported.

| Problem                                                    | Code                                       | Expected / found                                                                       | Suggestion                                                      |
| ---------------------------------------------------------- | ------------------------------------------ | -------------------------------------------------------------------------------------- | --------------------------------------------------------------- |
| Unknown predicate or operator name                         | `TRE0002`                                  | a registered name or an operator / the name                                            | nearest known name                                              |
| Misspelt operator between operands, trailing tokens        | `TRE0001`                                  | an operator or the end of the rule / the token                                         | nearest word operator                                           |
| Missing operand or literal                                 | `TRE0001`                                  | a term, constant or `(` (or a literal) / the token or end of rule                      | none                                                            |
| Mismatched, unclosed or unmatched delimiter                | `TRE0001`                                  | the closer / the token or end of rule (an unclosed group is reported at its opener)    | none                                                            |
| Unterminated string, bad escape                            | `TRE0001`, `TRE0015`                       | a closing `"`, or the supported escapes / end of rule or the escape                    | none                                                            |
| Wrong operand count, for every operator                    | `TRE0006`                                  | `2 operands` / `3 operands`                                                            | `PARITY` or `ExactlyOne` for `XOR`, parentheses for the others  |
| Ambiguous mixing without parentheses                       | `TRE0007`                                  | parentheses around one of the groups / the operators that share a level                | hint that shows the parenthesised text                          |
| Threshold or `BETWEEN` bounds, non-integer bound           | `TRE0008`                                  | the valid range, or an integer / the value                                             | none                                                            |
| Declared `Collapse`                                        | `TRE0002`                                  | a rule without `Collapse` / `Collapse`                                            | hint to call `Decision.Collapse(policy)` on the result          |
| Declared `Project`                                         | `TRE0002`                                  | a rule without `Project` / `Project`                                              | hint to use `COALESCE(x, True)` or `COALESCE(x, False)`, or `Decision.Project(unknownAs)` |
| Missing, unknown or mistyped predicate argument            | `TRE0003`, `TRE0005`, `TRE0004`            | the argument or kind / what was written                                                | nearest declared argument name                                  |
| Predicate argument named twice                             | `TRE0032`                                  | each argument once / the repeated name                                                 | hint to remove one                                              |
| JSON or YAML key that the node does not define, a `predicate` next to an `op`, or a value of the wrong kind | `TRE0014` | only the keys of that node kind / the key written                                      | nearest valid key, or a hint to split the node                  |
| Variable reference that names an undeclared data source    | `TRE0024`                                  | a declared data source name / the name written                                         | nearest declared source name, or a hint to declare it           |
| Variable reference whose query fails the validator of its source | `TRE0025`                            | a query valid for the data source / the query written                                  | none                                                            |
| Literal argument values that the predicate's argument validator rejects, such as reversed `IsBetween` or `IsNotBetween` bounds | `TRE0026` | the rule the values break / the values written | the fix the validator gives, such as swapping the bounds |
| Use of a predicate whose schema is marked deprecated (a warning, one per use; the rule still compiles) | `TRE0027` | none / the predicate name | the replacement predicate, when the schema names one |
| `CompressToDerived()`, `Canonicalize()` or `Simplify()` stopped at its pass limit (a warning on the result) | `TRE0033` | none / the rewrite and its pass limit | hint to call the rewrite on the result again |
| `PrintTruthTable` printed a table over the default cap of 8 terms (a warning on the result) | `TRE0034` | none / the term count and the row count | none |
| `PrintTruthTable` refused a table: over the cap without `AllowLargerTable`, or over the hard limit of 12 terms | `TRE0035` | none / the term count and the limit | hint to set `AllowLargerTable`, when the table is under the hard limit |
| A normal-form rewrite kept a threshold as an atom (a warning on the result of `ToNnf()`, `ToCnf()` or `ToDnf()`) | `TRE0031` | none / the threshold and the estimated node count of its expansion | hint to pass `NormalFormOptions` with `ExpandThresholds` set to `true` |
