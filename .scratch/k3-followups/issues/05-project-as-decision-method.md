# 05: Project is a method on the result, not an operator

**What to build:** Project stops being an in-tree operator. Add Decision.Project(unknownAs) returning a definite value (True and False pass through, Unknown becomes the chosen value), and remove the Project node from the DSL, JSON, YAML, builder, schema, compiler, evaluator, analyzer, node-shape and operator-info tables, printers, rule diff, and the expand, compress, canonicalise and simplify rewrites (the expansion and compression patterns that produced or consumed Project now use COALESCE with a constant, which remains a rule-level operator). A rule that needs the effect inside an expression uses COALESCE(x, True|False). Owner decision recorded 2026-10-03. Amend ADR-0005 decision 12, README and CONTEXT.md.

**Blocked by:** 04

**Status:** done

- [x] `Project(...)` in rule text, JSON or YAML is rejected with a diagnostic suggesting COALESCE or Decision.Project
- [x] Decision.Project matches the oracle for all inputs and never changes Result
- [x] Rewrites no longer emit or recognise a Project node and still preserve evaluation over all assignments (property tests pass)
- [x] All parity and exhaustiveness tests updated deliberately
- [x] Docs and ADR amendments written
- [x] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

Source: [research findings](../../k3-conformance/research-findings.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

- Done. Project is removed from the DSL, JSON, YAML, builder, schema, compiler, evaluator, analyzer, node-shape and operator-info tables, printers, `RuleDiff` and the rewrites; `ProjectExpression`, `ProjectNode`, `NodeShape.UnknownAs`, `RuleBuilder.Project` and `TruthValueText.Canonical(bool)` are gone. `Decision.Project(bool unknownAs)` is new (pure; `True`/`False` pass through, `Unknown` becomes the chosen value; never changes `Result`, `Faults` or `IsSatisfied`).
- Rejection diagnostic: one shared message and hint (`ProjectRejection`) pointing to `COALESCE` and `Decision.Project`, raised as `SyntaxError` (`BRE0001`, spanning the whole call) in DSL text and `MalformedTree` (`BRE0014`, at the node path) in JSON and YAML. `Project` stays a reserved DSL word, mirroring `Collapse` (ticket 04).
- Rewrites: `ExpandToPrimitives` has nothing to expand for Project; `CompressToDerived` no longer turns `COALESCE(x, True|False)` into a Project node and leaves it as written (only `COALESCE(NOT x, False)` still compresses, to `IsFalse`); `Simplify` already treats a `COALESCE` with a definite operand as definite. The property tests (`K3RuleGenerator`) now generate `COALESCE(x, True|False)` where they generated Project and still pass against the oracle.
- Tests: `ProjectTests` rewritten around `Decision.Project` (oracle over {T,F,U}, agreement with in-rule `COALESCE`, faults kept, `IsSatisfied` fail-closed) and rejection; project-specific cases in the analyzer, canonical printer, compress, expand, simplify, diff, schema, shape, operator-info, tree-name and diagnostics suites were removed or converted to `COALESCE`.
- Docs amended: ADR-0005 decisions 1, 3, 10 and 12 (decision 12 rewritten with the dated amendment note; the old k3-conformance 18 paragraph is removed), ADR-0003 superseded note, README, CONTEXT.md and issues-log rows 21, 29, 30 and 31.
