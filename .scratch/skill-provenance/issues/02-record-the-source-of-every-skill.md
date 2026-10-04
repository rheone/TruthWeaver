# 02: Record the source of every skill

**What to build:** One provenance table with a row per project skill: its source (repository and path, or "authored here"), its class from ticket 01, its license and its version. A skill with no upstream says "authored here". Skills that name an author but no license get the license the owner chooses. The table lives where the owner chose in ticket 01 and is the single place to read where a skill came from.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] `csharp-builder-pattern` and `csharp-system-attributes` match the bootstrapping repository (the whole skill directory, not only SKILL.md), and every other skill is compared the same way and any difference is reported
- [ ] Every directory under the project skills folder has exactly one row, and no row names a missing skill
- [ ] Each row has a source, a class, a license and a version
- [ ] Skills that lack a license field in their metadata have one added, or the table says why not
- [ ] The documentation lint still passes

Source: owner request, 2026-10-04.
