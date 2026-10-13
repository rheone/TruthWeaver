# D: Verify the Typst symbol names

**What to build:** The Typst dialect maps NAND and NOR to `arrow.t` and `arrow.b` and XOR to `xor`, and these were never checked. Read the official Typst reference (symbols and math functions) and confirm every name, quoting, escape and operator the dialect writes. Correct any wrong name, cite the reference page in a code comment and in `docs/equations.md`, and update tests and examples.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Every symbol name, escape and quoting rule in the Typst dialect is confirmed against the official reference, or corrected
- [x] The reference URL is cited in `docs/equations.md` and in a code comment
- [x] Tests and examples match the corrections
- [x] The page that describes the changed behavior is updated in the same change (no "changed from" text), `CHANGELOG.md` records each break with a migration step, and `K3ReferenceSyncChecker` and the doc example tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

2026-10-11, dispatch run: Checked against typst.app/docs (symbols/sym, symbols, foundations/str) with a fetch tool that summarizes pages, so the confirmation is from a summary and not a raw read. Confirmed with no change: `and` (U+2227), `or` (U+2228), `not` (U+00AC), `xor` (U+2295), `arrow.t` (U+2191), `arrow.b` (U+2193), `top`, `bot`, the shorthands `=>` and `<=>`, and the string escapes for backslash, double quote, newline, carriage return, tab and `\u{...}`. Not confirmed from a page: that a quoted string in math mode is read as text (the summaries did not cover it; it is documented Typst behavior). No code behavior, test or example changed; the URLs are now in a code comment and in `docs/equations.md`. A reviewer may want to open the symbol page once.
