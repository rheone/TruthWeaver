# 06: Rename NXOR to PARITY

**What to build:** The n-ary parity operator is renamed PARITY because NXOR conventionally means negated XOR (XNOR), the opposite of what it does, and XNOR is already an alias of EQUIVALENT (spec audit section D). Remove the NXOR spelling entirely in DSL, JSON/YAML (op name), schema, builder, node and shape names, labels, printers, rewrites and analyzer; an attempt to use NXOR yields a did-you-mean hint pointing at PARITY. Semantics unchanged (Unknown if any operand is Unknown, otherwise True for an odd number of True). Update the XOR arity hint message to name PARITY. Owner decision recorded 2026-10-03.

**Blocked by:** 05

**Status:** done

- [x] PARITY compiles and evaluates identically to the former NXOR (oracle-checked up to 4 operands)
- [x] NXOR is rejected with a did-you-mean suggestion for PARITY, in DSL, JSON and YAML
- [x] XOR with 3 or more operands points at PARITY
- [x] All layers, tests, README, CONTEXT.md and ADR-0005 decision 4 updated
- [x] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

Source: [spec audit](../../k3-conformance/spec-audit.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

`NXOR` is renamed `PARITY` in every layer: `ParityExpression`/`ParityNode`, `NodeShape`/`OperatorInfo` (`Parity`, label `PARITY`), tree op `parity` (JSON/YAML, schema), `RuleBuilder.Parity`, canonical printer, evaluator label, analyzer, rewrites and the test oracle. Semantics are unchanged; `ParityTests` (renamed from `NxorTests`) still checks 2 to 4 operands against `K3Oracle.Parity` for every assignment. `NXOR` is rejected by one shared `NxorRejection` diagnostic (`SyntaxError` for DSL text, `MalformedTree` for JSON/YAML, with a `Replacement` suggestion `PARITY` / `parity`); it is an explicit rejection rather than the edit-distance suggester because the two words are too far apart for it. The word stays reserved in the DSL. The `XOR` arity message and hint now name `PARITY` and `ExactlyOne`. README, CONTEXT.md, ADR-0005 decision 4 (amended in place), the ADR-0001/0003 operator lists and the k3-conformance issues-log row 12 are updated.
