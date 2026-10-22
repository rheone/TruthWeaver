# 04: Guard skill provenance against drift

**What to build:** A test or CI check that fails when a skill directory has no provenance row, when a row names a skill that no longer exists, or when a locked skill's contents no longer match its lock entry. It follows the repository's existing pattern for repository-wide guards. This ticket is optional: the owner may drop it if the table and lock stay small enough to review by eye.

**Blocked by:** 02, 03

**Status:** done

- [x] A test fails first for each of the three drift cases
- [x] The check passes on the repository as it stands
- [x] The failure message names the skill and the fix
- [x] The full validation from CLAUDE.md passes

Source: owner request, 2026-10-04.

## Comments

2026-10-04: Added `SkillProvenanceChecker` and `SkillProvenanceTests` under `tests/TruthWeaver.Tests/ReferenceDocs`, in the same shape as `K3ReferenceSyncChecker` (a `Check` on in-memory data proven by fixtures, a `CheckTree` on the real repository). It fails when a skill directory has no table row, a row or lock entry names a missing skill, a vendored skill has no lock entry, or a locked skill's hash differs; each message names the skill and the fix. It computes the folder hash itself, so the guard also verifies the documented hash method. The tool sorts paths with `localeCompare`, so the sort is culture-aware (ordinal gives a different hash); the README now says so. Authored skills are not locked and are covered by the row check only.
