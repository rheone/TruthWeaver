# 04: Simple-variable mode with a legend

**What to build:** An optional mode that replaces each term with a letter and returns a legend that maps each letter to its full term. Letters follow first occurrence, depth first, left to right. Identical terms share a letter.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] The same rule always gets the same lettering across calls
- [ ] Identical terms share one letter
- [ ] The legend has one entry per distinct term, and the result exposes it as data as well as text
- [ ] Behaviour past 26 terms is decided and documented (for example `p₁`, `p₂` or a clear error)
- [ ] Works in every dialect from tickets 01 to 03
- [ ] Public API has XML docs and the full validation set from CLAUDE.md passes

See also [spec](../spec.md).
