# 08: NullBehavior.Unknown is the zero value

**What to build:** A forgotten or defaulted `NullBehavior` means `Unknown`, the documented default. `Unknown` becomes `0` and `False` becomes `1`.

**Blocked by:** 07 (both edit the predicate null handling)

**Status:** resolved

- [ ] A test pins `default(NullBehavior) == NullBehavior.Unknown`
- [ ] No code or test depends on the old numeric values
- [ ] The breaking change is recorded in `CHANGELOG.md` with a migration step
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
