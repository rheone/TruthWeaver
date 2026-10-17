# 10: Ticket and .scratch references in XML docs

**Parent:** [main review](../spec.md)

**Blocked by:** 09 (both edit src comments in the same files)

**Status:** ready-for-agent

**What to build:** Remove references to ticket numbers and work items from XML docs and comments, and state the rule in the doc text instead. Keep an ADR reference only where it pins a normative rule.

**Evidence:** Examples: YamlTreePrinter.cs:9, YamlTreeParser.cs:12, BddManager.cs:11 and :158, Analyzer.cs:287 and :792, Expression.cs:82, RuleSwapLog.cs:6, RuleCompilerLog.cs:7, AssemblyInfo.cs:3 and :12 in two projects. The documentation standard says docs describe current behavior, and ticket numbers go stale when .scratch ages out.

- [ ] No src doc names a ticket or work-item number
- [ ] Docs for any changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
