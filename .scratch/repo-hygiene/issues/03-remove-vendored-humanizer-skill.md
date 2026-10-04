# 03: Remove the vendored humanizer skill from the repo

**What to build:** The third-party humanizer agent skill that was committed into the repository (nine tracked files) is deleted in a new commit, with no history rewrite, because it is unrelated to the library and shows up in every branch diff. Before deleting, copy the skill to the user-level skills folder so it keeps working in every project and survives a fresh clone. The git-ignored project-level copy already in the working tree is left alone.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] The tracked humanizer skill files are deleted and no tracked file references them
- [ ] A working copy of the skill exists at user level and the skill still loads
- [ ] k3-hardening 16 is marked resolved by this ticket
- [ ] No history is rewritten and nothing is pushed

Source: owner grilling session, 2026-10-03 (decisions Q1-Q24).
