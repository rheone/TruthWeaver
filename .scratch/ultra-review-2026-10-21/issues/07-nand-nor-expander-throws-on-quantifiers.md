# 07 - ExpandToNand and ExpandToNor throw on any rule that contains FORALL or EXISTS

Status: needs-triage
Severity: high
Lens: F5 Analysis+Rewriting
Verdict: CONFIRMED
Blocked by: none

## Finding F5.2: ExpandToNand / ExpandToNor throw on any rule that contains FORALL or EXISTS

Location: `src/TruthWeaver/Rewriting/NandNorExpander.cs:136` (CONFIRMED, high)

PrimitiveExpander.ExpandTop returns a QuantifierExpression unchanged, as ADR-0010 decision 9 specifies. NandNorExpander.Convert has no QuantifierExpression case, so it reaches the default arm and throws InvalidOperationException. ADR-0010 decision 9 and docs/rewriting-rules.md:11 say the quantifier stays an atom and its body is rewritten in place. The public ExpandToNand contract also says the method never throws for a normal rule. No test covers ExpandToNand or ExpandToNor with a quantifier.

### Failure scenario

`compiled("a AND FORALL(x IN items, elementTrue)").ExpandToNand()` passes the size and SubsetCost checks. Convert then hits the FORALL node and throws `InvalidOperationException: Unhandled expression type 'QuantifierExpression'`. The caller should get a NAND rule with `FORALL(x IN items, <NAND form of the body>)` kept as an atom.

### Expected behavior

Add a QuantifierExpression case that keeps the quantifier and converts its body: new QuantifierExpression(q.Kind, q.Variable, q.CollectionName, Convert(q.Body, conversion)).

### Verification

I traced the path and found no guard on it. CompiledRule.Rewrites.cs:68-71 sends ExpandToNand straight to NandNorExpander.ToNand(this.Root, cap), which calls Rewrite (NandNorExpander.cs:60). Rewrite calls PrimitiveExpander.Expand(node) at line 62. Expand maps the children first and then calls ExpandTop (PrimitiveExpander.cs:25). ExpandTop returns a QuantifierExpression unchanged (PrimitiveExpander.cs:69), and its body is already expanded because ExpressionTools.MapChildren handles quantifiers (ExpressionTools.cs:171). The size check and SubsetCost then run. Neither special-cases quantifiers, and SubsetCost goes through ExpressionTools.Children. Convert (NandNorExpander.cs:121-138) has cases only for Constant/Term, Not, And, Or, Coalesce and Threshold. A QuantifierExpression reaches the default arm at line 136 and throws InvalidOperationException("Unhandled expression type 'QuantifierExpression'"). The comment there, "PrimitiveExpander has already removed every other node type", is wrong for quantifiers. The same Convert serves ExpandToNor. I did not run the code, but the trace is complete. The impact is an unhandled exception from a public API, which the docs say never throws for a normal rule. The fix is the one proposed: add a QuantifierExpression case that rebuilds the node with a converted body. Severity is high rather than critical because it only affects rules that contain FORALL or EXISTS, and it fails loudly instead of returning a wrong result.

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.

## Oracle reproduction

The oracle in `tests/TruthWeaver.Tests/QuantifierLayerAgreementTests.cs` runs every rewrite over 12 quantifier rules, every K3 value of two terms and every element list of up to three elements. Seven of the nine rewrites (all but these two) pass. `ExpandToNand` and `ExpandToNor` throw `InvalidOperationException: Unhandled expression type QuantifierExpression` from `NandNorExpander.Convert` (`NandNorExpander.cs:136`). The two tests are not in the repo, because a skipped test fails the `dotnet format --severity info` gate (xUnit1004).

Acceptance test to add with the fix: in that class, add `("ExpandToNand", r => r.ExpandToNand())` and `("ExpandToNor", r => r.ExpandToNor())` to the `Rewrites` table, and `[InlineData("ExpandToNand")]` and `[InlineData("ExpandToNor")]` to `Rewrite_QuantifierRules_KeepTheOracleValueForEveryContext_Test`.
