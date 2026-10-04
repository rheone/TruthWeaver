# 07: Trace output and redaction

**What to build:** The trace and fault text name the variable reference and its outcome but never the resolved value, and `EvaluationOptions.IncludeResolvedValues` opts values into the trace only (decision 13).

**Blocked by:** 02

**Status:** ready-for-agent

- [ ] Tests prove the default trace and every fault message contain no resolved value
- [ ] With the opt-in, the trace shows values and faults still do not
- [ ] Full validation set from CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0006](../../../docs/adr/0006-data-sources-for-expression-variables.md).
