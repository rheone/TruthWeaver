# Repo hygiene: line endings, file-type config and commit hook
n**Status:** done

Decided in the 2026-10-03 owner grilling session (Q17, Q13-14).

- Line endings: LF by default (`* text=auto eol=lf`), with best-practice exceptions per file type (CRLF only for `*.bat`, `*.cmd` and `*.sln`). `.editorconfig` mirrors this per glob and also sets charset, final newline, trailing whitespace and indentation. `.gitignore` gains the missing .NET and tool outputs.
- The commit hook fixes formatting (CSharpier and dotnet format) and re-stages instead of only checking. Build, test and Roslynator stay as checks because they cannot auto-fix.
- With `eol=lf` the line-endings guard (script, test, hook task and the CLAUDE.md note) is redundant and is retired.

Tickets are in `issues/`.

## Follow-up: convention enforcement (2026-10-10)

**Status:** ready for implementation (tickets 03 to 06). Tickets 01 and 02 stay done.

A scan on branch `roadmap-tickets` found four places where a written rule has no check, or a disabled check has no review date.
The scan used grep only. Each ticket starts by confirming its numbers.

| Ticket | Concern |
| --- | --- |
| [03](issues/03-enforce-test-name-convention.md) | CLAUDE.md requires `{Member}_{Scenario}_{Expectation}_Test`. No test checks it. A grep found about 439 candidate methods without the suffix, some of them helpers. |
| [04](issues/04-sweep-stale-ticket-checkboxes.md) | 49 tickets marked `done` still have unchecked items, mostly the "carries XML docs, tests and validation" item. |
| [05](issues/05-recheck-disabled-analyzer-rules.md) | `.editorconfig` disables eight rules, each with a written reason and no review trigger. IDE0028 waits on an upstream fix. |
| [06](issues/06-audit-inline-suppressions.md) | Eight `#pragma warning disable` lines in `src/`. Two have no visible reason. |

Decisions:

- Ticket 03 uses the pattern of `DocumentationLintBaseline`: a list of exempt names that may only shrink, and the check fails on a clean entry.
- Ticket 04 is a one-time sweep with a script that lists candidates. It does not change any ticket status.
- Ticket 05 adds a review trigger, not a change of severity. Do not weaken analyzer severity without documenting why (CLAUDE.md).
- Ticket 06 prefers fixing the code over suppressing. Every remaining pragma carries a reason.
