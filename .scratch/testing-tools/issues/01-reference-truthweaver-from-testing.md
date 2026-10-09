# 01: Testing references TruthWeaver

**What to build:** `TruthWeaver.Testing` can use the compiler and analysis types, so rule-level testing tools can live beside `DecisionAssertions`. The package-boundary rule that `Testing` depends on `Abstractions` alone is replaced by one that allows the reference.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `TruthWeaver.Testing` has a project reference to `TruthWeaver`
- [x] The package-boundary architecture test no longer requires `Abstractions` alone and still forbids the dependencies the new rule does not allow
- [x] ADR-0004 is amended in place with the reason and the date
- [x] `docs/packages.md` states the new dependency
- [ ] Carries XML docs on all public API, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes.

See also [spec](../spec.md).
