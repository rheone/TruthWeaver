# 25: Amend historical `Project` and `Collapse` wording in ADR-0001, ADR-0002 and ADR-0004

**What to build:** ADR-0001, ADR-0002 and ADR-0004 still describe `Project` and `Collapse` as rule-level features, which the owner decisions of 2026-10-03 replaced with methods on the result (`Decision.Project` and `Decision.Collapse`, ADR-0005 decisions 12 and 14 as amended). Add a dated amendment note at each affected passage, in the style used by the ADR-0005 amendments, pointing at ADR-0005. Do not rewrite history: the original text stays, struck through or annotated, so the decision trail remains readable. Also check these ADRs for any remaining `NXOR` mention and note the `PARITY` rename.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Every passage in ADR-0001, ADR-0002 and ADR-0004 that presents `Project` or `Collapse` as a rule-language feature carries a dated amendment note linking ADR-0005
- [x] Any remaining `NXOR` mention in those ADRs carries a note naming `PARITY`
- [x] No original decision text is deleted
- [x] `dotnet build` and `dotnet csharpier check .` pass

Source: k3-followups ticket 15 report (historical mentions left untouched); [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

- 2026-10-03 (implementation): verified, no amendment needed beyond what exists. ADR-0001 already carries the dated note (amended 2026-10-03) linking ADR-0005 decision 14 at the `Decision` record; ADR-0002 and ADR-0004 never presented `Project` or `Collapse` as rule-language features (their only hits are the unrelated words "project" and "collapsible"), and none of the three ADRs mentions `NXOR` (ADR-0001 already names `PARITY`). Nothing was rewritten.
