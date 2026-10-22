# 03: normalize-text as a file-based app

**What to build:** `scripts/normalize-text.cs` applies the same text rules as the `.ps1` (valid UTF-8, BOM removal, dash/ellipsis/quote replacement with the curly-quote allowlist, LF endings, trailing whitespace, one final newline, blocklist reporting). `-CheckOnly` becomes `--check-only`; file paths are positional arguments.

**Blocked by:** 01

**Status:** done

- [x] Run against the whole repo, the app reports and rewrites exactly what the `.ps1` does (compare on a scratch copy)
- [x] `--check-only` rewrites nothing and exits 1 on a file that needs a change
- [x] The `.ps1` BOM exception stays until ticket 08
- [x] All build and format gates pass for the new file
