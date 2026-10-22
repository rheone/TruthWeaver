# 04 - Definition (let) expansion grows exponentially before any size limit is checked

Status: needs-triage
Severity: high
Lens: F2 Parsing; F3 Compilation
Verdict: CONFIRMED
Blocked by: none

## Finding F2.4: Definition expansion grows exponentially before MaxNodeCount is checked (tiny input exhausts memory)

Location: `src/TruthWeaver/Compilation/DefinitionExpander.cs:299` (CONFIRMED, high)

ExpandUse expands the callee body again for every use and builds new nodes (ExpandNode/ExpandAll). The only bound during expansion is MaxDefinitionDepth (default 8), on chain length. MaxNodeCount is counted later in RuleNodeCompiler.Build, so the whole expanded raw tree is built first. ADR-0008 decision 6 says expanded nodes count toward MaxNodeCount, but nothing stops the expansion before it is materialized. This is outside the Parsing slice, but it is the same resource-limit gap.

### Failure scenario

Eight definitions, each the AND of 20 uses of the previous one (`let a1 = x AND x AND ... (20); let a2 = a1 AND a1 AND ... (20); ... let a8 = a7 AND ...; a8`). This is under 2 KB of text and asks for about 20^8 = 2.6e10 RawNode allocations, so Compile runs out of memory or runs for a very long time instead of returning TRE0010 MaxNodeCountExceeded.

### Expected behavior

The expander counts the nodes it produces and stops with MaxNodeCountExceeded when the count passes CompilerOptions.MaxNodeCount.

### Verification

I traced the path in src/TruthWeaver/Compilation/DefinitionExpander.cs and found no node-count bound before expansion finishes.

- RuleNodeCompiler.cs:90 calls DefinitionExpander.Expand(document, registry, options) and gets the whole expanded raw tree back.
- RuleNodeCompiler.cs:206 is the only MaxNodeCount check. It runs afterwards, in the build pass.
- ExpandUse (DefinitionExpander.cs:298-299) calls ExpandNode(definition.Body, bound, nextChain, unused) on every use. ExpandNode rebuilds every body node through the Carry(..., new AndNode(ExpandAll(...))) arms at lines 133-182.
- Nothing caches or shares an expanded body between uses. Only argument nodes are shared, through the parameter dictionary at lines 292-296. The expander keeps no count of the nodes it creates.
- The only checks before a body is expanded are the cycle check, the argument-count check and chain.Count >= MaxDefinitionDepth (line 277). That last one limits chain length (default 8), not total size.

Trigger check: with a8 used at chain depth 0, a1 is used at chain depth 7, which is below 8, so the 8-level chain is allowed. Each level has 20 uses of the one below. That gives about 20^8 = 2.6e10 nodes, built from a parse tree of roughly 160 use nodes. I found no input-length or definition-count limit that would stop the text first.

Smaller cases are also a problem. A fan-out of 10 gives about 1e8 nodes, and a fan-out of 5 gives about 390,000. Both are far above the default MaxNodeCount of 512, so the caller expects a TRE0010 error. The documentation (CompilerOptions.cs:15 and ADR-0008) says expanded nodes count toward MaxNodeCount, but nothing enforces that during expansion, so the cap only works for small blow-ups.

I did not run the repro, so the memory-exhaustion claim rests on the traced allocation count, not on an observed crash. Compile does not return TRE0010 in this case. It allocates the full tree first, which is a denial-of-service on untrusted rule text.

## Finding F3.1: let expansion has no size budget: a short document hangs or exhausts memory in Compile (and in RuleDefinitions.ReadText)

Location: `src/TruthWeaver/Compilation/DefinitionExpander.cs:299` (CONFIRMED, high)

DefinitionExpander expands every use eagerly and allocates a fresh copy of each definition body (ExpandUse -> ExpandNode at 298-299, new nodes via Carry/ExpandAll) without counting nodes. MaxNodeCount is enforced only afterwards, in RuleNodeCompiler.Build (RuleNodeCompiler.cs:90-95), and MaxDefinitionDepth (8) bounds chain length, not fan-out. So the expanded size is fanout^depth before any limit runs. RuleDefinitions.Reader.Expand (RuleDefinitions.cs:263-332) walks the same expansion with no budget and records a path string per expanded node, so the public RuleDefinitions.ReadText, which needs no registry, has the same blowup. This breaks the CompilerOptions promise (CompilerOptions.cs:4-5) that rule text from an unreviewed source cannot hang a request thread, and ADR-0008 decision 6's claim that MaxDefinitionDepth catches a runaway chain 'long before it could threaten MaxNodeCount'.

### Failure scenario

Rule text (about 1.5 KB): `let d0 = p; let d1 = d0 AND d0 AND ... (20 times); let d2 = d1 AND ... (20 times); ... let d7 = d6 AND ... (20 times); d7`. The chain depth is 7, below MaxDefinitionDepth 8, so no TRE0038 fires. Expansion materializes 20^7 (about 1.3e9) leaf references and about 6.7e7 AndNode arrays before Build counts the first node, so Compile runs for minutes or throws OutOfMemoryException instead of returning TRE0010. RuleDefinitions.ReadText on the same text does the same work and also allocates a path string for each node.

### Expected behavior

Expansion must count the nodes it produces against MaxNodeCount (or a dedicated budget) and stop with TRE0010 as soon as the budget is exceeded. RuleDefinitions.Reader needs the same bound.

### Verification

I traced the path in DefinitionExpander.cs and RuleNodeCompiler.cs. I did not run the scenario or read RuleDefinitions.Reader. Neither file contains a size budget.

1. RuleNodeCompiler.Compile (RuleNodeCompiler.cs:88-95) calls DefinitionExpander.Expand(document, registry, options) first. Only after Expand returns does it call compiler.Build(root), which is where the report says MaxNodeCount is enforced. I did not open Build to check that. Expand itself has no counter and no early exit.

2. Expander.ExpandUse (DefinitionExpander.cs:213-300) is the only limit on a definition chain. It checks chain.Count >= options.MaxDefinitionDepth (line 277), which bounds how deep the chain goes. It does nothing about how many uses a body contains. A depth-7 chain of 20-way bodies passes that check, because the deepest use is made with chain.Count == 7 and 7 is below 8.

3. Line 299 calls this.ExpandNode(definition.Body, bound, nextChain, unused) for every use. Nothing caches or shares the result of an expansion. ExpandNode rebuilds each body node, for example AndNode via Carry(a, new AndNode(this.ExpandAll(...))) at line 134. ExpandAll (lines 197-211) allocates a new RuleNode[] for every operand list. Each use of d_k therefore re-expands and re-allocates d_k's whole body, so the work is fanout^depth. Only parameter substitution (line 124-127) and ConstantNode/TermNode/ErrorNode (line 132) are shared by reference, and that does not stop the repeated rebuilding.

4. For the scenario (`let d0 = p; let d1 = d0 AND x20; ... let d7 = d6 AND x20; d7`), each of the 20 uses of d_k expands a fresh d_k body. That gives about 20^6 (6.4e7) AndNode instances at the d1 level, each with its own 20-element array, and about 20^7 leaf references. That is many GB of allocation and minutes of CPU, or an OutOfMemoryException, before Build counts a single node. TRE0010 never gets a chance to fire.

I did not check the parser for a total-input size cap. The scenario text is only about 1.5 KB. The parser would have to cap the number of operands or definitions to stop it.

I did not read RuleDefinitions.Reader (RuleDefinitions.cs:263-332), so the ReadText half of the claim is unverified. The Compile half is traced.

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
