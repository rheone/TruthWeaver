# 02: Regex IsMatch with flags

**What to build:** Replace `Matches`, `MatchesIgnoreCase`, `NotMatches` and `NotMatchesIgnoreCase` with `IsMatch` and `IsNotMatch`. Add an optional `flags` string argument of the single letters `i m s n x` (ignore case, multiline, single line, explicit capture, ignore pattern whitespace). A literal pattern or literal flags that are invalid is a compile diagnostic (reuse `TRE0026`). Keep the bounded cache from least-surprise ticket 26 and key it on pattern and options. The packages are unpublished, so the break is allowed; there is no deprecation shim.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] `IsMatch` and `IsNotMatch` exist; the four old predicates and their docs pages are gone
- [ ] An unknown flag letter, a repeated flag and an invalid literal pattern each give a compile diagnostic, pinned by tests
- [ ] Case-insensitive matching uses `RegexOptions.IgnoreCase | CultureInvariant`, as the removed twin did
- [ ] The page that describes the changed behavior is updated in the same change (no "changed from" text), `CHANGELOG.md` records each break with a migration step, and `K3ReferenceSyncChecker` and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
