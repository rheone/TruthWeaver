# 03: One `EvaluateAsync` overload with nullable, defaulted parameters

**What to build:** Replace the two `CompiledRule<TContext>.EvaluateAsync` overloads (`(context, services, options, ct)` and `(context, services, dataSources, options, ct)`) with one whose trailing parameters are nullable and defaulted, so a caller supplies only what the rule needs. A null `services` means an empty provider: a predicate that requires a service faults as it does for any missing registration.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

Open point the implementer settles and records: parameter order. Keeping `services` second preserves existing positional callers; passing `dataSources` by name is then the norm. Do not leave two overloads that make a call ambiguous.

- [ ] A test fails first: evaluating with neither services nor data sources compiles and runs
- [ ] A rule that needs a service and gets none yields `Unknown` plus a fault, not an exception
- [ ] Existing call sites (tests, README, docs, benchmarks) compile; the public change is in the CHANGELOG
- [ ] XML docs describe each parameter's null meaning
- [ ] The full validation from CLAUDE.md passes

Source: owner review, 2026-10-04 (question 3).
