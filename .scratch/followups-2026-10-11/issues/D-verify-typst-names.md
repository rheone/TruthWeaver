# D: Verify the Typst symbol names

**What to build:** The Typst dialect maps NAND and NOR to `arrow.t` and `arrow.b` and XOR to `xor`, and these were never checked. Read the official Typst reference (symbols and math functions) and confirm every name, quoting, escape and operator the dialect writes. Correct any wrong name, cite the reference page in a code comment and in `docs/equations.md`, and update tests and examples.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Every symbol name, escape and quoting rule in the Typst dialect is confirmed against the official reference, or corrected
- [ ] The reference URL is cited in `docs/equations.md` and in a code comment
- [ ] Tests and examples match the corrections
- [ ] The page that describes the changed behavior is updated in the same change (no "changed from" text), `CHANGELOG.md` records each break with a migration step, and `K3ReferenceSyncChecker` and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
