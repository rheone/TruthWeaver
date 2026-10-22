# 03: Make the skill lockfile correct

**What to build:** `skills-lock.json` lists every externally sourced skill (at least `humanizer`, and `github-markdown` if ticket 01 classes it as external) with a hash that matches the committed files. The current entry was computed before the skills were moved and converted to LF, so it is probably stale. Find out what the lock tool hashes. If it hashes raw bytes, add `.gitattributes` rules so line-ending normalization can never change a locked skill's hash. Refresh the lock with the tool chosen in ticket 01, not by hand.

**Blocked by:** 01, 02

**Status:** done

- [x] Each externally sourced skill has a lock entry whose source agrees with the provenance table
- [x] The tool reports every entry as up to date (or the hash method is documented and the entries are verified by hand)
- [x] A fresh clone with `core.autocrlf` set either way keeps the hashes valid
- [x] No lock entry exists for an authored skill, unless ticket 01 decided otherwise

Source: owner request, 2026-10-04.

## Comments

2026-10-04: `npx skills` (1.7.0) hashes every file of the skill folder (sorted relative path plus raw bytes, `.git` and `node_modules` excluded), so line endings matter. A fresh `npx skills add blader/humanizer` on Windows wrote CRLF files plus plugin and CI files that the committed copy omits (`.claude-plugin/`, `.cursor-plugin/`, `.github/`), so the tool cannot reproduce the committed hash. The old hash was stale. The `humanizer` entry now holds the hash of the committed (LF) folder, computed with the tool's own algorithm (the same script reproduced the tool's hash on its fresh install). The method is documented in `.claude/skills/README.md`. `.gitattributes` pins `.claude/skills/**` to `text eol=lf`. No authored skill is locked. Verified by hand, not with a tool report; the tool has no check command for a project lock.
