# 12: TextWriter overloads on every printer

**What to build:** Add a `TextWriter` overload to every printer on `CompiledRule` (`PrintRuleText`, `PrintJson`, `PrintPlainText`, `PrintMermaid`, `PrintEquation`, `PrintEquationWithLegend`, `PrintHtml`, `PrintDot`, the `Decision` forms and the rewrite or diff printers if they exist) so large output goes to a file, response or log without one large string. The overload writes exactly the text the string form returns. Prefer writing in pieces through the writer over building a string and writing it once, where the printer builds text incrementally.

**Blocked by:** 06, 08, 09, 10

**Status:** ready-for-agent

- [ ] Each overload has a test that its output equals the string form
- [ ] A test with a failing writer shows the exception reaches the caller unchanged
- [ ] Docs list the overloads next to each printer
- [ ] The page that describes the changed behavior is updated in the same change (no "changed from" text), `CHANGELOG.md` records each break with a migration step, and `K3ReferenceSyncChecker` and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
