# 10 - MaxNodeCount restarts at 0 inside every quantifier body, so the total node limit is not enforced

Status: needs-triage
Severity: medium
Lens: F3 Compilation
Verdict: CONFIRMED
Blocked by: none

## Finding F3.2: MaxNodeCount restarts at 0 inside every quantifier body, so the total node limit is not enforced

Location: `src/TruthWeaver/Compilation/RuleNodeCompiler.cs:425` (CONFIRMED, medium)

BuildQuantifier compiles the body through binding.CompileBody, which creates a new RuleNodeCompiler (CollectionBinding.cs:50-52) whose nodeCount starts at 0. Only depth, quantifier depth and the active variables are passed across the boundary. ADR-0010 (line 175) says 'a quantifier counts as one node plus its body, once', and CompilerOptions documents MaxNodeCount as the maximum total node count. In the code, each body gets its own full MaxNodeCount allowance, and the outer tree counts the whole quantifier as 1 node.

### Failure scenario

With the default MaxNodeCount 512: `FORALL(a IN items, <body of 500 nodes>) AND FORALL(b IN items, <500 nodes>) AND ...` with 20 such quantifiers compiles with no TRE0010, which gives a rule of about 10,000 nodes. A single quantifier with a 511-node body plus 511 outer nodes also passes, which is about 1,022 nodes.

### Expected behavior

Pass the running node count (or the remaining budget) into CompileBody the same way startDepth and startQuantifierDepth are passed, and add the body's count back to the outer total, so that TRE0010 fires when the combined tree exceeds MaxNodeCount.

### Verification

I traced the path. RuleNodeCompiler.cs:425-432 (BuildQuantifier) calls binding.CompileBody and passes only this.options, depth + 1, this.quantifierDepth and this.activeVariables. It does not pass the running nodeCount or the remaining budget. CollectionBinding.cs:50-52 builds a fresh RuleNodeCompiler through the static RuleNodeCompiler<Element<...>>.Compile(body, elementRegistry, bodyOptions, depth, quantifierDepth, enclosingVariables). The nodeCount field (RuleNodeCompiler.cs:27) therefore starts at 0 in that new compiler. Build (RuleNodeCompiler.cs:205-206) increments the count and compares it to options.MaxNodeCount, and the body compiler's options still carry the full 512. After CompileBody returns, only bodyDiagnostics are merged (line 436) and the body's count is never added back to the outer total. The outer compiler counts the quantifier as one node. TRE0010 is raised only in Build, so no later check can catch the combined tree. ADR-0010 line 175 says a quantifier counts as one node plus its body, once, and CompilerOptions documents MaxNodeCount as the maximum total node count. The code gives each body its own full allowance, so the scenario works as described. For example, 20 quantifiers with 500-node bodies each never hit the limit, and a 511-node body plus 511 outer nodes passes. I did not run a test. The behavior is a bypass of a compile-time structural limit. It is not a correctness bug in evaluation. The limit is also not the only protection, because MaxDepth and MaxQuantifierDepth are propagated. Nested quantifiers still give each level a fresh 512, so growth is bounded only by depth. Severity is medium: a resource-limit bypass that matters mainly for untrusted rule authors.

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
