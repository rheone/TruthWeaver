# 04: Guard skill provenance against drift

**What to build:** A test or CI check that fails when a skill directory has no provenance row, when a row names a skill that no longer exists, or when a locked skill's contents no longer match its lock entry. It follows the repository's existing pattern for repository-wide guards. This ticket is optional: the owner may drop it if the table and lock stay small enough to review by eye.

**Blocked by:** 02, 03

**Status:** ready-for-agent

- [ ] A test fails first for each of the three drift cases
- [ ] The check passes on the repository as it stands
- [ ] The failure message names the skill and the fix
- [ ] The full validation from CLAUDE.md passes

Source: owner request, 2026-10-04.
