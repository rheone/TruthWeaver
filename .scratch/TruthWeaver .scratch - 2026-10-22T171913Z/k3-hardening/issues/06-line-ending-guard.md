# 06: Line-ending guard

**What to build:** The repository is eol=crlf, and two separate scripted edits wrote LF into the working tree and had to be repaired by hand. Add a cheap guard (a .gitattributes/.editorconfig check, a Husky pre-commit step or a CI step) that fails when a tracked text file has the wrong line endings, and document the rule for scripted edits.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] A file with LF line endings in a CRLF-configured path fails the check
- [x] The check is part of the local pre-commit tasks and CI, or the ticket records why one is enough
- [x] CLAUDE.md or the contributing notes mention the rule
- [x] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

See also [spec](../spec.md).

## Comments

- Guard is `scripts/check-line-endings.ps1` (fails on any bare LF in the files it is given), tested by `tests/TruthWeaver.Architecture.Tests/LineEndingGuardTests.cs` (built red-first), wired as the `line-endings` Husky pre-commit task on staged files. No CI step: CI checks out with `eol=crlf`, and Git normalises LF on commit, so CI can never observe the problem. Rule documented in CLAUDE.md under Git.
- Open: 216 tracked files (mostly `.scratch/`, `.claude/`, `.husky/`, `.gitattributes`) currently sit as LF in the working tree, so a whole-tree run of the script would fail on them. The hook only checks staged files, so it flags them when next touched. I did not mass-convert them (out of scope).
