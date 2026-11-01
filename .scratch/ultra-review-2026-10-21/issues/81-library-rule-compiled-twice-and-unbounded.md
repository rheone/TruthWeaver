# 81 - A library rule is compiled twice per reference and is not bounded by MaxNodeCount

Status: done
Category: refactor
Severity: low
Lens: Code review of ticket 11b-2
Blocked by: none

## Finding

The library expander builds each library rule fully once to check its names, and the caller's compile builds the same nodes again. The cache is keyed by rule name and reference span, so a rule referenced from 50 sites is parsed, expanded and compiled 50 times plus the caller's build. The per-reference work is not counted against `CompilerOptions.MaxNodeCount`, so a large library rule referenced many times does unbounded work before the caller's limit is checked. Separately, `RuleNodeCompiler` has two parallel branches (definitions and library references) that wrap a non-document root, and it walks the raw tree on each recursive compile, including each quantifier body, just to detect a library reference. (From the review of 11b-2; not measured.)

## Agent Brief

**Category:** refactor
**Summary:** Build each library rule once per compile, count its nodes against the node budget, and use one wrapping path.

**Current behavior:** See above.

**Desired behavior:** The parse of a library rule is shared across every reference to it within one compile. The nodes a reference adds count against `MaxNodeCount`, so the compile fails with the existing too-large diagnostic instead of doing unbounded work. One code path wraps a non-document root for definitions and library references. Compiled trees, diagnostics and spans are unchanged.

**Acceptance criteria:**
- [x] A test shows a library rule referenced from many sites is parsed once per compile (assert through a counting library or a public timing-free signal such as a node count), and the existing `RuleLibraryTests` pass unchanged.
- [x] A test fails before and passes after: a library rule referenced enough times to exceed `MaxNodeCount` gives the node-budget diagnostic, not an unbounded expansion.
- [x] No second raw-tree walk per recursive quantifier-body compile for a rule with no library reference (or a measured justification in the Resolution).
- [x] The validation list in CLAUDE.md passes.

**Out of scope:** Source spans inside a library rule (ticket 83); library rules inside quantifier bodies (ticket 79).

## Resolution (2026-11-01)

- `LibraryRuleExpander` keeps, for one compile, the parse of each library rule (by rule name) and the node count of
  each library rule that expanded and built without an error. Each site still runs the cheap expansion (it puts the
  site span on every node), but the parse is shared and the check build runs once per rule. A failed rule is not kept,
  so each site reports its own errors (the cycle path is seen from that site, as before).
- `Expand` returns the expansion and its node count. `DefinitionExpander.Expander.ExpandReference` adds that count
  (less the reference node already counted) to its `NodeCountGuard` (new `TryCount(int nodes, ...)` overload), so many
  references stop at `MaxNodeCount` with the existing `TRE0010` diagnostic. `RuleNodeCompiler.Compile` presets
  `nodeLimitReported` when the expansion already reported `TRE0010`, so the build of the cut-short tree does not report
  it twice.
- One path in `RuleNodeCompiler.Compile` wraps a non-document root and expands. The library-reference walk runs only
  when the new `expandLibraryReferences` argument is true, which only `RuleCompiler` passes. A quantifier body compile
  and a library rule check build do not walk the tree again.

Tests (`tests/TruthWeaver.Tests/RuleLibraryTests.cs`), both red first. The query validator of the `user` data source
counts how often the library rule is built:

- `Compile_LibraryRuleReferencedFromManySites_IsCheckedOncePerCompile_Test`: three references, expected 4 builds
  (three in the compiled rule, one check). Red: `Expected: 4, Actual: 6`.
- `Compile_LibraryRuleReferencedPastTheNodeLimit_StopsExpandingAtTheLimit_Test`: 20 references to a 6-node rule with
  `MaxNodeCount` 30 give one `TRE0010` and fewer builds than references. Red: the build count was not below 20 (each
  reference was check-built before the caller counted). After the budget change, the first green run showed a second
  `TRE0010` from the build, fixed by the preset above.

The parse count is not visible through the public API, so the tests observe the build count; a shared parse follows
from the same per-compile cache. Existing `RuleLibraryTests` and the whole `TruthWeaver.Tests` project pass unchanged.
No CHANGELOG entry: compiled trees, diagnostic codes and messages do not change; only the work done does.
