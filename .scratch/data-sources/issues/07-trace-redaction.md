# 07: Trace output and redaction

**What to build:** The trace and fault text name the variable reference and its outcome but never the resolved value, and `EvaluationOptions.IncludeResolvedValues` opts values into the trace only (decision 13).

**Blocked by:** 02

**Status:** done

- [x] Tests prove the default trace and every fault message contain no resolved value
- [x] With the opt-in, the trace shows values and faults still do not
- [x] Full validation set from CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0006](../../../docs/adr/0006-data-sources-for-expression-variables.md).

## Resolution notes

- **Public API.** `EvaluationOptions.IncludeResolvedValues` (new last record parameter, default `false`).
- With the opt-in a resolved term's trace text (flat `TraceEntry.Text` and `TraceNode.Text`, including memoized repeats) reads `v: from("user", "$.secret") = "hunter2"`; a term whose variable failed keeps the reference-only text. Fault messages never include values (unchanged; the source's own `ErrorMessage` is passed through as the source wrote it). Tests: `VariableTraceRedactionTests` in `TruthWeaver.Testing.Tests`.
