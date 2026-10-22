# 07: Constant style and override strings

**What to build:** Add a constant style setting to the printers that write constants (`PrintRuleText`, `PrintEquation`, `PrintPlainText`, `PrintMermaid`): `Word` (`True`, `False`, `Unknown`; today's default), `Letters` (`T`, `F`, `U`; the default for the strict-logic Unicode variant), `LogicSymbols` (`⊤`, `⊥`, `U`). Each constant also accepts an override string (for example `TrueText`, `FalseText`, `UnknownText`) that wins over the style. `PrintRuleText` accepts only styles the parser reads back. Do not offer `1`/`0`/`½`: the ordering `False < Unknown < True` must not read as numeric (`CONTEXT.md`).

**Blocked by:** 06

**Status:** done

- [x] The three styles and the overrides are tested for each printer that supports them
- [x] An override that would break `PrintRuleText` round-trip is rejected with a clear message
- [x] Docs explain why `⊥` can read as Unknown in partial logic and why letters are the default
- [x] The page that describes the changed behavior is updated in the same change (no "changed from" text), `CHANGELOG.md` records each break with a migration step, and `K3ReferenceSyncChecker` and the doc example tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
