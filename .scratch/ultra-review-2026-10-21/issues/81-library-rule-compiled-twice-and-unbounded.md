# 81 - A library rule is compiled twice per reference and is not bounded by MaxNodeCount

Status: ready-for-agent
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
- [ ] A test shows a library rule referenced from many sites is parsed once per compile (assert through a counting library or a public timing-free signal such as a node count), and the existing `RuleLibraryTests` pass unchanged.
- [ ] A test fails before and passes after: a library rule referenced enough times to exceed `MaxNodeCount` gives the node-budget diagnostic, not an unbounded expansion.
- [ ] No second raw-tree walk per recursive quantifier-body compile for a rule with no library reference (or a measured justification in the Resolution).
- [ ] The validation list in CLAUDE.md passes.

**Out of scope:** Source spans inside a library rule (ticket 83); library rules inside quantifier bodies (ticket 79).
