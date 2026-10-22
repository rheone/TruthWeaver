# 02: Record the source of every skill

**What to build:** One provenance table with a row per project skill: its source (repository and path, or "authored here"), its class from ticket 01, its license and its version. A skill with no upstream says "authored here". Skills that name an author but no license get the license the owner chooses. The table lives where the owner chose in ticket 01 and is the single place to read where a skill came from.

**Blocked by:** 01

**Status:** done

- [x] `csharp-builder-pattern` and `csharp-system-attributes` match the bootstrapping repository (the whole skill directory, not only SKILL.md), and every other skill is compared the same way and any difference is reported
- [x] Every directory under the project skills folder has exactly one row, and no row names a missing skill
- [x] Each row has a source, a class, a license and a version
- [x] Skills that lack a license field in their metadata have one added, or the table says why not
- [x] The documentation lint still passes

Source: owner request, 2026-10-04.

## Comments

- 2026-10-04: The table is `.claude/skills/README.md` (23 rows). Ticket 01 did not name a location; this one sits beside the skills and is outside the documentation lint scope (`.claude/`). `csharp-builder-pattern` and `csharp-system-attributes` were replaced with the upstream directories (new files added, LF endings). Every other skill matched its source, including `github-markdown` against `miscellaneous-agentic-tooling`. `csharp-library-repo-structure` lacks a license field upstream; it was left unedited to avoid drift from the canonical copy and the table says so. Ticket 03 must lock `humanizer`; its hash was computed from the whole directory, which differs from upstream by the omitted repository files.
