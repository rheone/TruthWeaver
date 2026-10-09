# 03: Palettes

**What to build:** A caller picks a preset palette or supplies their own.

**Blocked by:** 01 (MermaidOptions with direction and node shapes)

**Status:** ready-for-agent

- [ ] Presets `Light` (today's colors, the default), `ColorblindSafe`, `Monochrome` and `Dark` define the true, false, unknown and skipped classes plus highlight and mute
- [ ] `Monochrome` separates states by stroke style so it prints in grayscale
- [ ] A caller-supplied palette record is accepted
- [ ] `Decision` coloring uses the chosen palette
- [ ] Carries XML docs on all public API, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes.

See also [spec](../spec.md).
