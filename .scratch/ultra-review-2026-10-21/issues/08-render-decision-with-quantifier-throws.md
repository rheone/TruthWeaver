# 08 - Rendering a decision with an evaluated quantifier throws: the trace shape does not match the outline

Status: needs-triage
Severity: high
Lens: T2 Surfaces
Verdict: CONFIRMED
Blocked by: none

## Finding T2.5: Rendering a decision with any evaluated quantifier throws: trace shape does not match the outline

Location: `src/TruthWeaver/Printing/RuleRenderTree.cs:167` (CONFIRMED, high)

CompiledRule.OutlineOf gives a quantifier exactly one operand, its body (CompiledRule.Printing.cs:671). CollectionBinding gives the quantifier's TraceNode one child per element, each wrapping [body] (CollectionBinding.cs:206/221), and no children for an empty, null or faulted collection (lines 119/129/138). AssertOperandCountsAligned compares the two positionally and throws. Only the structural (no-trace) quantifier printing is tested (QuantifierFormsTests:151).

### Failure scenario

Evaluate `FORALL(x IN items, elementTrue)` with IncludeTrace over 0, 1 or 2 items, then call `rule.PrintMermaid(decision)`, PrintDot, PrintHtml or PrintPlainText(decision). Each throws InvalidOperationException 'OutlineNode/TraceNode operand-count mismatch', although the decision came from the same rule.

### Expected behavior

Make RuleRenderTree aware of quantifiers: map element children onto the body outline, or render per-element nodes. The evaluator trace and the printers must agree on shape.

### Verification

I traced the mismatch in code but did not run it. The printer entry points that take a decision are the traced overloads in PlainTextTreePrinter.cs:46, MermaidTreePrinter.cs:112, HtmlPrinter.cs:34 and DotPrinter.cs:31. Each calls RuleRenderTree.Build(root, traceTree, ...). Build (RuleRenderTree.cs:56-65) calls AssertOperandCountsAligned first. That check (line 167) throws InvalidOperationException when a TraceNode is evaluated and its Children.Count differs from OutlineNode.Operands.Count.

Outline side: CompiledRule.Printing.cs:661-675 gives a QuantifierExpression exactly one operand, `[OutlineOf(quantifier.Body)]`.

Trace side: CollectionBinding.cs builds the quantifier's TraceNode differently.
- Lines 119, 129 and 138 handle a faulted, null or empty collection. They return a TraceNode with zero children, so 0 != 1 and the check throws.
- Lines 165, 183 and 206 add one child per element, labelled `x[i]`. Each wraps the body trace as a single child.
- With 2 or more elements the quantifier's child count is not 1, so the check throws at the quantifier node.
- With exactly 1 element the counts match at the quantifier node. The recursion then pairs the body outline with the `x[0]` element TraceNode. Build lines 75-79 do this positionally, with no quantifier-aware mapping. The body outline is a term with 0 operands, but the element node has 1 child (the body trace), so the check throws one level down.
- A short-circuited ForAll or Exists can leave the same shape mismatch.

I found no quantifier special-casing in RuleRenderTree.Build. So rendering any traced decision that includes an evaluated quantifier throws, for 0, 1 and 2 elements as claimed. Structural (no-trace) printing does not call the check, which fits the claim that only that path is tested.

Severity: it is a deterministic exception from a public API on a valid decision from the same rule, in a feature added by ADR-0010. It does not affect evaluation or semantics. It only breaks the trace renderers for quantifier rules, so I rate it high, and medium would be defensible.

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.

## Oracle reproduction

In `tests/TruthWeaver.Tests/QuantifierLayerAgreementTests.cs`, the test failed for every quantifier rule and element count tried (0 to 3): `PrintPlainText`, `PrintHtml`, `PrintMermaid` and `PrintDot` of the decision threw.

The failing test is in the repo, skipped with this ticket number: `Print_DecisionOfAQuantifierRule_DoesNotThrowForAnyElementCount_Test`. Acceptance: remove its `Skip` and the test passes. While the `Skip` is there, `dotnet format --severity info` reports xUnit1004, so CI is red on the format step until this ticket is done.
