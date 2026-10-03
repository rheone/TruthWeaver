# 04: Collapse is a method on the result, not part of the rule

**What to build:** Collapse stops being a rule-language feature. The original three-valued Decision.Result is always the raw value; the developer chooses a policy at the call site by calling the Decision.Collapse method (UnknownAsFalse, UnknownAsTrue, UnknownIsError), which stays pure and never records a fault. Remove the declared Collapse from the DSL, JSON, YAML, builder, schema, compiler (the compiled rule's collapse policy), evaluated-tree root, descriptors, printers, rule diff, rewrites and the analyzer, retire the nested-collapse diagnostic (BRE0016), and remove Decision.Outcome. IsSatisfied stays Result == True (fail-closed). Amend ADR-0005 decision 14 and ADR-0001's text, README, CONTEXT.md and issues-log rows 22, 23. Owner decision recorded 2026-10-03 (see research item 1a for the rationale and the SQL WHERE/CHECK and XACML PDP/PEP precedents).

**Blocked by:** None (can start immediately)

**Status:** done

- [x] No rule text, JSON or YAML can declare Collapse (it is rejected with a clear diagnostic that points to Decision.Collapse)
- [x] Decision.Result is never altered by a collapse; a faulting predicate leaves IsSatisfied false regardless of policy
- [x] Decision.Collapse(policy) behaves identically to before for all three policies (oracle-checked over {T,F,U})
- [x] All layers and tests that referenced the declared collapse are removed or updated deliberately
- [x] Docs and ADR amendments written
- [x] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

Source: [research findings](../../k3-conformance/research-findings.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

- Done. A declared `Collapse` is removed from the DSL, JSON, YAML, builder, schema, compiler, evaluated tree, descriptors, printers, `RuleDiff` and rewrites; `CompiledRule.CollapsePolicy`, `Decision.Outcome`, `CollapsePolicyText`, `CollapseNode` and `NestedCollapse` (`BRE0016`, retired and not reused) are gone. `CollapsePolicy`, `CollapseOutcome` and `Decision.Collapse(policy)` are unchanged.
- Rejection diagnostic: one shared message and hint (`CollapseRejection`) pointing to `Decision.Collapse`, raised as `SyntaxError` (`BRE0001`, spanning the whole call) in DSL text and `MalformedTree` (`BRE0014`, at the node path) in JSON and YAML. `Collapse` stays a reserved DSL word. The DSL parser still consumes the call's arguments so later errors are reported in the same pass.
- Tests: `CollapseTests` rewritten around `Decision.Collapse` (oracle over {T,F,U}, faults kept, `IsSatisfied` fail-closed) and rejection; collapse-specific tests in the rewrite, diff, schema, tree-name and diagnostics suites were removed or converted to call-site collapse.
- Docs amended: ADR-0005 decision 14 (rewritten, with the dated amendment note and related mentions), ADR-0001 note, README, CONTEXT.md, issues-log rows 22 and 23 and the summary item.
