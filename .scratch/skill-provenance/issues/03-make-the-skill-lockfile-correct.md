# 03: Make the skill lockfile correct

**What to build:** `skills-lock.json` lists every externally sourced skill (at least `humanizer`, and `github-markdown` if ticket 01 classes it as external) with a hash that matches the committed files. The current entry was computed before the skills were moved and converted to LF, so it is probably stale. Find out what the lock tool hashes. If it hashes raw bytes, add `.gitattributes` rules so line-ending normalization can never change a locked skill's hash. Refresh the lock with the tool chosen in ticket 01, not by hand.

**Blocked by:** 01, 02

**Status:** ready-for-agent

- [ ] Each externally sourced skill has a lock entry whose source agrees with the provenance table
- [ ] The tool reports every entry as up to date (or the hash method is documented and the entries are verified by hand)
- [ ] A fresh clone with `core.autocrlf` set either way keeps the hashes valid
- [ ] No lock entry exists for an authored skill, unless ticket 01 decided otherwise

Source: owner request, 2026-10-04.
