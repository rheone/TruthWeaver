# 22b-4: Quantifier bodies that read the element through data sources

**Parent:** [22b](22b-quantifiers-build.md)

**What to build:** A quantifier body can read the current element with untyped data-source nodes (`from("line", "...")`) as well as typed element predicates, per the owner's decision that both are in slice 1. Follow ADR-0006 and note section 8. Do not add a new `IDataSource` member without asking the owner (data-source ranges are 22c).

**Blocked by:** 22b-3

**Status:** done

- [x] A body using `from(...)` on the element evaluates correctly, including `Unknown` and fault cases
- [x] If the work needs an ADR-0006 amendment or a new `IDataSource` member, stop and report it as a blocker
- [x] The pages that describe the behavior are written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
