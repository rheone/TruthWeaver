# 04 - RuleNodeCompiler.Build's own MaxDepthExceeded diagnostic path has no test

Status: needs-triage
Severity: critical
Slice: Compilation (src/TruthWeaver/Compilation/RuleNodeCompiler.cs)
Lens: boundary
Signal: line

## Gap

`RuleNodeCompiler<TContext>.Build`'s `depth > this.options.MaxDepth` check and its `MaxDepthExceeded`
diagnostic (`src/TruthWeaver/Compilation/RuleNodeCompiler.cs:239-251`) is entirely uncovered. Ticket 03 of
this same branch added a parse-time depth guard (DSL + YAML), and ticket 03's own handoff notes
(`.scratch/ultra-review-2026-10-21/handoff-2026-10-24.md`) explicitly flag that this compiler-level check
"is still load-bearing for one case ticket 03's parse-time guard does not reach: a `let` chain that is
shallow as written but deep after `DefinitionExpander` inlines uses" - which ticket 04 (also already
committed on this branch) was meant to cover via a separate node-count guard. Whichever guard is meant to
catch a post-expansion deep tree, `RuleNodeCompiler.Build`'s own depth check and its specific diagnostic
(message, `expected`/`found` text, `node.Path`) have zero test coverage proving they fire correctly when
actually reached.

## Missing scenario

A compiled tree (constructed directly via `RuleNode`/`RuleNodeCompiler`, bypassing the parser's own depth
guard if necessary, or via a `let`-expansion deep enough after ticket 04's expansion to reach this specific
check) that trips `depth > options.MaxDepth` inside `Build`, asserting the resulting `CompilationResult`
has a `MaxDepthExceeded` diagnostic with the expected message, `expected`/`found` text and `node.Path`, and
that the tree compiles to `FailedNode.Placeholder` rather than throwing or stack-overflowing.

## Acceptance criteria

- Add the test first (`/tdd`): it exercises the missing scenario, which currently has no coverage for it
- The line coverage signal clears for `RuleNodeCompiler.cs:241-251`
- Test named `{Member}_{Scenario}_{Expectation}_Test`, one behavior per test, `<summary>` added
- Validation list from CLAUDE.md passes
