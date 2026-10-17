# 11-2: Definitions and parameters in rule text

**Parent:** [11](11-named-sub-rules-and-parameters.md)

**What to build:** An author can write `let adult = age >= 18; let atLeastAge(n) = age >= n; adult AND atLeastAge(21)` in rule text and get a compiled rule that is a plain immutable tree: each use is expanded at compile time, so the evaluator, BDD analyzer, rewrites and printers need no new node kind, and a term used in several expansions keeps one identity. Includes `CompilerOptions.MaxDefinitionDepth`, expanded nodes counting toward `MaxNodeCount` and `MaxDepth`, and distinct diagnostic codes (in the one code table) for recursion (naming the cycle), duplicate names, reserved or predicate names or `T`/`F`/`U`, wrong argument count, unknown definition and limit overruns. Spans point at the use and name the definition; a diagnostic inside an expansion also names the definition's span. An unused definition is an opt-in lint. A compiled rule prints in expanded form.

**Blocked by:** 11-1

**Status:** done

- [ ] Rule text with definitions and parameters compiles, evaluates and prints (expanded) as the parent ticket describes
- [ ] Each guardrail has its own diagnostic code and a test
- [ ] The BDD analysis, equivalence check, rewrites and every printer work on a rule that used definitions without change (tests)
- [ ] The TextMate grammar ticket (17a) is checked: if it has shipped, add `let` to its keywords in this change
- [ ] The pages that describe the behavior are written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
