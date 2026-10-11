# 10: Width-aware pretty rule text

**What to build:** Add a width option to `PrintRuleText` (for example `PrintRuleText(RuleTextOptions)`) that breaks long rules across lines with indentation, within a target width, and still compiles back to an equal tree. Define the breaking rule in the docs (break after the operator inside the outermost group that does not fit; two-space indent per level). The default stays the current one-line output.

**Blocked by:** 06

**Status:** done

- [x] A property test shows pretty output compiles to an equal tree for generated rules at several widths
- [x] A width too small to fit the longest unbreakable token does not loop or throw
- [x] `docs/rule-text.md` states the rule with one example
- [x] The page that describes the changed behavior is updated in the same change (no "changed from" text), `CHANGELOG.md` records each break with a migration step, and `K3ReferenceSyncChecker` and the doc example tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
