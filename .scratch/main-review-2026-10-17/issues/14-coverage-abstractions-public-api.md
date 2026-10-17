# 14: Coverage of the Abstractions public API

**Parent:** [main review](../spec.md)

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

**What to build:** Add focused tests for the untested public members of TruthWeaver.Abstractions.

**Evidence:** Line coverage: PredicateArguments.cs 45.9% (lines 170-333), LiteralValue.cs 76.1% (251-299, 329-340), Fault.cs 73.1% (48-67), Decision.cs 60.7% (83-95), DataSources.cs 56.2% (24, 48-56), EquatableArray.cs 73.5% (34-59). Assembly total 80.3%. Member names are inferred from line ranges, so open the lines first. Measure with: dotnet test --project tests/<X> --coverlet --coverlet-output-format cobertura.

- [ ] Each listed file reaches 90% line coverage with behavior tests, not call-through tests
- [ ] Docs for any changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
