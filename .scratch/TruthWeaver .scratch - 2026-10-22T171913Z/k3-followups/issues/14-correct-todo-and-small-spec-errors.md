# 14: Correct the TODO and small spec errors

**What to build:** Fix the smaller inconsistencies in the source requirements and reference notes: 'six primitive operations' versus seven listed, the arity table that marks XOR as ternary and n-ary versus the binary-only decision, NAND/NOR written n-ary but listed binary, 'trinary' replaced by 'three-valued' or 'ternary', the typos (Kleen, Conical, Oder), the vacuous parity wording, and the 'definite whenever information is sufficient' sentence (true per connective, not per formula). Add a note at the top of the TODO that it is the historical requirements document and where decisions were finalised (ADR-0005, the follow-up decisions). Spec audit section B8.

**Blocked by:** 06

**Status:** done

- [x] Each B8 row is fixed or explicitly left with a reason
- [x] The TODO records that NXOR became PARITY and Project/Collapse became methods on the result

Source: [spec audit](../../k3-conformance/spec-audit.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

Fixed in `.scratch/2026-10-02-TODO.md` (historical note, trinary to three-valued/ternary, typos, XOR binary and PARITY row, NAND/NOR binary-only, Project/Collapse and NXOR-to-PARITY decisions) and, on disk only (`.tmp` is gitignored), in `Strong Kleene K3 Logic.md` (seven primitives, per-connective wording, parity rule, arity table). The `Expression specification.md` tautology row is left as is: the statement is correct, only vacuous for constant-free formulas, and needs no text change.
