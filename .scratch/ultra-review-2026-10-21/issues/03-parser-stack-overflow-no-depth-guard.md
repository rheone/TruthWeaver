# 03 - Deep nesting (DSL) and self-referencing aliases or deep nesting (YAML) overflow the stack and crash the process

Status: wontfix
Category: bug
Severity: high
Lens: F2 Parsing; T2 Surfaces
Verdict: CONFIRMED
Blocked by: none

## Finding F2.2: Parsing recursion has no depth guard: deep nesting crashes the process with StackOverflowException before MaxDepth applies

Location: `src/TruthWeaver/Parsing/DslParser.cs:517` (CONFIRMED, high)

MaxDepth is only enforced in RuleNodeCompiler.Build (RuleNodeCompiler.cs:226), after the full raw tree exists. The DSL parser recurses with no limit: about 9 frames per group level (ParsePrimary 538-548 -> ParseExpression -> ParseOrExpression -> ParseAndOrChain -> ... -> ParseNotExpression), and one frame per NOT (517-527, not a tail call because it builds the NotNode afterwards). NoteAttacher.Measure (NoteAttacher.cs:115) and TreeFormatReader.ReadNode/ReadOperator (149, 436) also recurse with no limit. The Lexer recurses once per invalid character (`return this.NextToken();`, Lexer.cs:132), so it depends on a JIT tail call, and it adds one diagnostic per character with no cap. A StackOverflowException cannot be caught, so this breaks 'Compile never throws' and kills the host.

### Failure scenario

`RuleCompiler.Compile(new string('(', 200_000) + "a" + new string(')', 200_000))`, or `string.Concat(Enumerable.Repeat("NOT ", 500_000)) + "a"`, terminates the process instead of returning TRE0009 MaxDepthExceeded. A deeply nested YAML document through CompileYaml, or a JsonElement parsed with a raised JsonDocumentOptions.MaxDepth through CompileJson(JsonElement), takes the same path through TreeFormatReader.

### Expected behavior

The parser and the tree reader stop at a nesting bound tied to CompilerOptions.MaxDepth (or call RuntimeHelpers.TryEnsureSufficientExecutionStack) and report a diagnostic.

### Verification

I traced the DSL parser path. I did not run it. A repo-wide grep of src/ for TryEnsureSufficientExecutionStack, EnsureSufficientExecutionStack, InsufficientExecutionStack, maxStackSize, new Thread, MaxLength, MaxSource and MaxInput found nothing. There is no stack guard and no input-size cap. In Parsing/DslParser.cs, the group case of ParsePrimary (lines 536-548) calls ParseExpression (281) -> ParseOrExpression (397) -> ParseAndOrChain (413) -> parseOperand delegate (ParseAndExpression 402) -> ParseAndOrChain -> ParseInfixChain -> ... -> ParseNotExpression -> ParsePrimary. Every '(' therefore adds a fixed stack of frames, and none of them checks depth. ParseNotExpression (517-527) calls itself once per NOT, then builds NotNode(operand, ...) afterward, so it is not a tail call. The only depth counters in Parsing/ are in a skip-ahead helper (lines 147-159) and a bracket-matching helper (379-388), and neither limits recursion. NoteAttacher.Measure (NoteAttacher.cs:117-133) recurses over operands with no guard. The MaxDepth check lives in RuleNodeCompiler.Build, which runs only after Parse has produced the whole tree. A StackOverflowException cannot be caught in .NET, so an input such as 200k '(' or 500k 'NOT ' kills the process before TRE0009 can be reported. I did not read the Lexer or TreeFormatReader lines the finding cites. The DSL parser path is enough to confirm the finding. Practical exposure is limited to untrusted rule text, since it takes a very large input (hundreds of thousands of nesting levels, or tens of thousands with the default 1 MB stack). It is still a denial-of-service against a host that compiles user-supplied rules. I assign medium-high severity, and I keep high because the failure is unrecoverable.

## Finding T2.1: YAML self-referencing alias or very deep nesting overflows the stack while parsing a rule

Location: `src/TruthWeaver.Yaml/YamlTreeParser.cs:57` (CONFIRMED, high)

YamlStream.Load resolves aliases into shared node references, so an anchor that is aliased inside its own body gives a cyclic YamlNode graph. YamlNodeCursor (lines 170-189) hands child nodes straight to TreeFormatReader.ReadNode, which recurses with no cycle check and no depth limit. JSON is capped at depth 64 by JsonDocument, but nothing bounds YAML nesting. The repository already knows the cycle exists: YamlDataSource.ToJson keeps an ancestor set for exactly this case, and YamlDataSourceTests pins `a: &x [*x]`.

### Failure scenario

`compiler.CompileYaml("&r\nop: not\noperands: [*r]\n")`, or a 50,000-level `{op: not, operands: [...]}` chain, recurses until a StackOverflowException. That exception cannot be caught, so one untrusted rule document kills the host process. The documented contract is a MalformedTree diagnostic.

### Expected behavior

Detect alias cycles by reference (as YamlDataSource does) and enforce a nesting limit in line with JSON's, so both produce a diagnostic.

### Verification

The reader has no cycle or depth guard.

Trace for `&r\nop: not\noperands: [*r]`:
- YamlTreeParser.Parse (src/TruthWeaver.Yaml/YamlTreeParser.cs:31, 57) calls `stream.Load` and passes `Documents[0].RootNode` to `TreeFormatReader.Read(new YamlNodeCursor(node), ...)`.
- `Read` (src/TruthWeaver/Parsing/TreeFormatReader.cs:68) calls `ReadNode` (:149), which calls `ReadNodeCore`, then `ReadOperator`, then `ReadNode` on each operand. This is plain recursion.
- A grep of src for `EnsureSufficientExecutionStack`, `TryEnsureSufficientExecutionStack` and `MaxNesting` returns nothing.
- The only depth limit is `CompilerOptions.MaxDepth` (default 32), checked in RuleNodeCompiler.cs:226. That runs after the raw tree is built, so it cannot stop the read.

Cycle:
- YamlDotNet registers an anchor before it loads the node's children, so `*r` inside `&r` resolves to the same node object. This is based on my recollection of YamlDotNet, not a trace of its source.
- The repo's own tests agree: YamlDataSourceTests.cs:179 and :189 pin `a: &x [*x]` as a self-referencing alias that must be rejected, and the cursor code has a branch for "an alias" nodes.
- I did not run the sample. The reader recurses forever on it, so it would end in a StackOverflowException, which cannot be caught.

Deep nesting without aliases:
- There is no limit on YAML depth, and the JSON path is bounded by `JsonDocument`.
- Very deep YAML recurses without bound in `ReadNode`, and probably also inside YamlDotNet's own loader. I did not trace the second part.

Impact: a single untrusted YAML rule document kills the host process. The documented contract is a MalformedTree diagnostic.

Severity: critical is defensible because the process crashes, but it needs the attacker to supply the YAML rule text. I rate it high.

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.

## Comments

> *This was generated by AI during triage.*

Already implemented, closing as `wontfix`. `DslParser.cs` now has a `TryEnterNesting` guard shared by `ParseOrExpression` and `ParseNotExpression`, reporting `MaxDepthExceeded` instead of recursing unbounded. `TreeFormatReader.ReadNode` (shared by the JSON and YAML readers) now carries its own depth guard. `YamlNodeCursor` detects a self-referencing alias by reference, and `YamlTreeParser.Parse` catches `YamlException` and reports `MalformedTree` instead of crashing. Covered by `DslParserDiagnosticTests.cs`.
