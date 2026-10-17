# 08: Shipped surface with no ADR

**Parent:** [main review](../spec.md)

**Blocked by:** None (can start immediately)

**Status:** needs-owner-decision

**What to build:** Write short ADRs or ADR amendments, one at a time, for: OrderOptimizer and PredicateSchema.Cost (soundness, fault order and trace order against ADR-0002 left-to-right); BatchEvaluator/EvaluateMany and maxDegreeOfParallelism (against ADR-0002 caller-loops and no implicit concurrency); the packages TruthWeaver.AspNetCore, TruthWeaver.Tool and TruthWeaver.Tool.Core (ADR-0004 package set); TruthWeaverMetrics (ADR-0004 defers observability); RuleCounterfactual, RuleRelation, ChangeSetSearch, Linter, RuleCoverage and RuleDiff; the chosen elementSource registration shape (ADR-0010).

**Evidence:** ADR conformance review of main at ef26bd5, section (c). A search of docs/adr found no coverage.

- [ ] Each item has an accepted ADR or amendment, or the owner records that none is needed
- [ ] Docs for any changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
