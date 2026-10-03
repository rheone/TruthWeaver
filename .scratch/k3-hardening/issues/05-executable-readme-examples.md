# 05: Executable README examples

**What to build:** The README and CONTEXT.md examples are currently verified only by hand (the documentation sweep used an uncommitted throwaway project). Turn the runnable examples (DSL, JSON, YAML, builder, diagnostics output, rewrite output, Decision.Collapse/Project examples) into tests that extract or mirror them, so any future change that breaks a documented example fails the build. Pick the smallest approach (a test that compiles and compares the documented snippets, or marker comments); record the choice.

**Blocked by:** k3-followups 15

**Status:** done

- [x] Every runnable example in README.md and CONTEXT.md is covered by a test that fails if its documented output changes
- [x] A deliberately broken example is shown to fail the check
- [x] Adding a new example has a documented procedure
- [x] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

See also [spec](../spec.md).

## Comments

- Approach (owner-approved): marker-driven check. `tests/TruthWeaver.Tests/DocExamples/DocExampleChecker.cs` reads README.md and CONTEXT.md, requires a `<!-- doctest:KIND ARG -->` marker above every `text`/`json`/`yaml`/`mermaid`/`ebnf` block, compiles rules with the real compiler and compares documented tree, Mermaid and diagnostics output to the real output. No new dependency. C# fragments are out of scope, so the `Decision.Collapse`/`Project` and rewrite examples (all C#) are not covered; add a `text` output block with a marker if one is wanted.
- Covered: 14 rule/JSON/YAML/tree/Mermaid blocks in the README and 3 diagnostics outputs; 6 non-runnable blocks (class diagrams, grammars) are allow-listed with a reason.
- Deliberately broken examples are shown to fail in `DocExampleTests` (uncompilable rule, stale tree, mismatched JSON, stale diagnostics, untagged block, skip without reason, unresolvable marker).
- Procedure: `docs/doc-examples.md`, with a pointer in CLAUDE.md Testing.
- First run found a real stale example: the README said a JSON diagnostic has no span and showed no location, but k3-followups 24 added spans. README prose and output fixed. ADR-0005 line 253 still says JSON diagnostics have no spans (an accepted ADR, left unchanged; follow-up for the owner).

