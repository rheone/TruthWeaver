# 12 - Quantifier trace label uses the registered collection name; canonical text uses the name as written

Status: needs-triage
Severity: medium
Lens: L2 Cardinality+quantifiers
Verdict: CONFIRMED
Blocked by: none

## Finding L2.1: Quantifier trace label uses the registered collection name while the rule's canonical/trace text uses the name as written, so a case-different collection name breaks coverage pairing

Location: `src/TruthWeaver/Registry/CollectionBinding.cs:72` (CONFIRMED, medium)

`CollectionBinding.EvaluateQuantifierAsync` labels the quantifier's trace node and flat entries with the binding's registered `name` (`$"{label}({variable} IN {name})"`), but `RuleNodeCompiler.BuildQuantifier` (RuleNodeCompiler.cs:442) stores `node.CollectionName` exactly as the author wrote it, and `TraceText.Of` (TraceText.cs:29) and `CanonicalPrinter` print that written name. Collection lookup is case-insensitive (`PredicateRegistry.TryGetCollection` upper-cases the key), so the two texts differ whenever the rule spells the collection with different casing than the registration. `RuleCoverageAccumulator.Pair` (RuleCoverageAccumulator.cs:313) compares `traceNode.Text == this.texts[index]` for non-term nodes and throws `Mismatch` on any difference; the comment on TraceText.cs:25 says the two must match exactly. Terms do not have this problem because the compiler resolves a term to its schema's registered name. The same unnormalised name also makes `FORALL(x IN Lines, b)` and `FORALL(x IN lines, b)` two different analyzer atoms (ADR-0010 decision 8) even though they denote the same collection. The compiler side looks wrong: it should carry `binding.Name` into the `QuantifierExpression` (or the binding should use the written name), as it already does for predicates.

### Failure scenario

Register `AddCollection("lines", ...)`, compile `FORALL(line IN Lines, inStock)` (compiles cleanly), evaluate with `IncludeTrace`, then `new RuleCoverage(rule).Add(decision)`: the quantifier node's trace text is `FORALL(line IN lines)` while the rule's text is `FORALL(line IN Lines)`, so coverage throws `ArgumentException: The trace tree of the decision does not match the rule at '$'` for a decision of the very same rule.

### Expected behavior

Per ADR-0010 decision 7 and docs/strong-k3/quantifiers/*.md, coverage folds element children onto the body node for any decision of the rule; the trace label and the rule text for a quantifier must be identical regardless of how the author cased the collection name, exactly as a term's label is the registered predicate name.

### Verification

I traced the path through the code and did not run it.

1. Lookup is case-insensitive. `PredicateRegistry.TryGetCollection` (PredicateRegistry.cs:84-87) upper-cases the key. `RuleNodeCompiler.BuildQuantifier` (RuleNodeCompiler.cs:372, 442) resolves the binding, then builds `new QuantifierExpression(node.Kind, node.Variable, node.CollectionName, bodyTree)`. `node.CollectionName` is the text as written, and nothing in the compiler replaces it with `binding.Name`. So `FORALL(line IN Lines, ...)` compiles cleanly against a collection registered as `lines`.

2. The rule's text uses the written name. `TraceText.Of` (TraceText.cs:29) prints `{label}({q.Variable} IN {q.CollectionName})`. `CanonicalPrinter` (CanonicalPrinter.cs:276) does the same. `Evaluator.EvalQuantifierAsync` (Evaluator.cs:432) builds its description from `node.CollectionName` too, but only uses it on the defensive unregistered-collection branch.

3. The runtime trace uses the registered name. On the normal path, `Evaluator.cs:448-464` calls `binding.EvaluateQuantifierAsync` and returns that `TraceNode` unchanged. `CollectionBinding.cs:72` builds `description = $"{label}({variable} IN {name})"`, where `name` is the registered name. That string is the root `TraceNode` text, at lines 119, 129, 138 and 221, and the flat `TraceEntry`, at line 77.

4. Coverage compares the two texts. `RuleCoverageAccumulator.Pair` (RuleCoverageAccumulator.cs:311-317) compares non-term nodes with `traceNode.Text == this.texts[index]` and throws `Mismatch` on any difference. The quantifier node is not a `TermExpression`, so it takes the strict equality branch. With `lines` registered and `Lines` written, the trace text is `FORALL(line IN lines)` and the rule text is `FORALL(line IN Lines)`. `RuleCoverage.Add` then throws `ArgumentException` for a decision of the same rule.

The trigger needs a trace-bearing decision, a `RuleCoverage` consumer, and a collection name whose casing differs from its registration. Quantifiers written with exactly the registered casing are unaffected.

I did not trace the secondary claim that `Lines` and `lines` become two separate analyzer atoms. It is plausible, because the expression stores the name unnormalised.

Severity: a real crash in a documented feature, but it needs a casing mismatch, and the fix is small. The trace text, the flat entries and the memo keys all have to switch to the same name. I rate it medium rather than high.

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
