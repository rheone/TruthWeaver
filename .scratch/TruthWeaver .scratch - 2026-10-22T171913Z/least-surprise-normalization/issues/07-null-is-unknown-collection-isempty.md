# 07: A null collection is Unknown for IsEmpty and IsNotEmpty

**What to build:** Collection `IsEmpty` and `IsNotEmpty` take a `NullBehavior` that defaults to `Unknown`, as the string `IsEmpty` and the count predicates do. `IsNullOrEmpty` remains the definite test for "null counts as empty". The predicate pages state the rule: a missing value is Unknown unless the predicate is a null test.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [ ] Failing tests first: `IsEmpty` and `IsNotEmpty` on a null collection answer `Unknown` by default and honour `NullBehavior.False`
- [ ] The K3 reference pages and the NotX twin table are updated, and `dotnet test tests/TruthWeaver.Tests --filter-class "*K3Reference*"` passes
- [ ] The null-handling sentence is added to the predicate conventions page if it exists
- [ ] The breaking change is recorded in `CHANGELOG.md` with a migration step
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
