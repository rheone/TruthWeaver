# 05: Recheck disabled analyzer rules

**What to build:** Each analyzer rule that `.editorconfig` turns off has a stated trigger for a recheck, so a fixed upstream bug or a changed formatter does not leave the rule off by accident.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] List every `severity = none` entry in `.editorconfig` and `src/Directory.Build.props`: IDE0028, CA1859, SA1009, SA1111, SA1500, SA1502, SA1600, SA1633
- [x] Next to each entry, add the recheck trigger (for example "retest on each .NET SDK bump", "retest on each CSharpier upgrade")
- [x] Retest IDE0028 on the pinned SDK (`11.0.100-rc.1.26425.128`) in a scratch copy; if the code fix no longer corrupts `new List<T>(capacity)`, re-enable the rule
- [x] Retest the SA formatting rules against the current CSharpier; re-enable any that no longer conflict
- [x] No severity is weakened, and every change is documented in the file
- [x] The full validation set from CLAUDE.md passes

See also [spec](../spec.md).
