# E: Fix the CHANGELOG encoding and guard it

**What to build:** `CHANGELOG.md` has a stray byte 0x85 (a Windows-1252 ellipsis) near `HasCount…`, so the file is not valid UTF-8 and the Edit tool refuses it. Replace the byte with the intended character, save as UTF-8 with LF, and add a test that every tracked text file under the repository (markdown, props, csproj, cs, json, yaml) is valid UTF-8 and has no byte order mark, so it cannot happen again.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `CHANGELOG.md` is valid UTF-8 and its text is otherwise unchanged
- [x] A test fails for an invalid-UTF-8 text file (prove it with a fixture in the test, not a bad file in the repo)
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

2026-10-11, dispatch run: The byte 0x85 occurred twice in `CHANGELOG.md` (both after `HasCount`) and once in `docs/predicate-conventions.md` (after `NotHasCount`); all three became U+2026. The new `TextFileEncodingTests` walks the repository (skipping bin, obj, .git, .tmp, artifacts, TestResults) and proves the failure cases with in-test byte fixtures.
