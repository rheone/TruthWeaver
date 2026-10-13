# 03: HasCount predicates

**What to build:** Rename the collection count predicates: `CountEqual`, `CountGreaterThan`, `CountGreaterThanOrEqual`, `CountLessThan`, `CountLessThanOrEqual` become `HasCountEqualTo`, `HasCountGreaterThan`, `HasCountGreaterThanOrEqualTo`, `HasCountLessThan`, `HasCountLessThanOrEqualTo`, and their `NotCount…` twins become `NotHasCount…`. Fix the doc pages that say the family is `StringPredicates` for collection predicates if they are wrong. The packages are unpublished, so the break is allowed; there is no deprecation shim.

**Blocked by:** 02

**Status:** done

- [x] The ten predicates have the new names, with their doc pages renamed and the index updated
- [x] Twin tests cover each pair
- [x] The page that describes the changed behavior is updated in the same change (no "changed from" text), `CHANGELOG.md` records each break with a migration step, and `K3ReferenceSyncChecker` and the doc example tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
