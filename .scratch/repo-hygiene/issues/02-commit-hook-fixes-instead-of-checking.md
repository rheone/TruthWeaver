# 02: Commit hook fixes formatting instead of rejecting it

**What to build:** A commit formats staged files rather than failing on them. The pre-commit hook runs `csharpier format` and `dotnet format` instead of the check variants and re-stages the files they change, so a developer or agent never has to run a formatter by hand. Build, test and `roslynator analyze` stay as checks because they cannot auto-fix. The line-endings guard (its script, its test, its hook task and the line-endings note in CLAUDE.md) is retired, because `eol=lf` and `.editorconfig` now cover it. CLAUDE.md's Required validation and Git sections describe the new behaviour.

**Blocked by:** 01

**Status:** done

- [ ] Staging a deliberately mis-formatted C# file and committing produces a formatted commit, not a failure
- [ ] Only staged files are re-staged; unrelated unstaged edits are never swept into the commit
- [ ] The line-endings script, its test, its hook task and the CLAUDE.md note are removed and nothing still references them
- [ ] Build, test and Roslynator still run in the hook and still fail the commit on a real error
- [ ] The full validation from CLAUDE.md passes

Source: owner grilling session, 2026-10-03 (decisions Q1-Q24).

## Comments

- 2026-10-04: The line-endings guard (script, test, hook task, CLAUDE.md note) was retired with ticket 01. Still open here: the hook runs `csharpier format` and `dotnet format` and re-stages instead of the check variants.
