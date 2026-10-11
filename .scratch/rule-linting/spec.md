# Rule linting: follow-ups

**Status:** grilled 2026-10-10, ready for implementation

Source: [library-roadmap](../library-roadmap/spec.md) ("Rule linting" residual and "Lint follow-ups"), and the "Not done" notes in
[k3-hardening 09](../k3-hardening/issues/09-analyzer-lint-rules-for-new-operators.md).

## Problem statement

The opt-in K3 lints (`LintRules`, `TRE0017` to `TRE0023`) find constructs that are provably redundant. Four gaps remain:

- One mistake can produce several findings. An `If` and the inspection inside it are each reported, with nothing that links them.
- No lint warns when a rule is close to the depth or node limits, or when a chain is wide.
- No lint tells an author that a rule is not in canonical form.
- A finding does not say where the construct is. The tree nodes keep no location.

## Solution

Four tickets, in this order. Tickets 01 to 03 need no change to `Expression`. Ticket 04 does, so it stays deferred.

| Ticket | Change | Status |
| --- | --- | --- |
| [01](issues/01-relate-nested-findings.md) | Mark a nested finding as related to the finding that encloses it. | ready |
| [02](issues/02-depth-and-wide-chain-lints.md) | Two new lint rules: depth near `MaxDepth`, and a wide `AND`/`OR` chain. | ready |
| [03](issues/03-not-canonical-lint.md) | A lint that fires when `Canonicalize()` would change the rule. | ready |
| [04](issues/04-finding-spans-and-paths.md) | A location on every finding. | deferred |

## Decisions

- Every lint stays opt-in, `Info` severity, and never changes the compiled rule.
- A lint that proposes a replacement fires only when the replacement is K3-equivalent (existing rule, see `Linter`).
- New codes follow `TRE0027` in `DiagnosticCodes`. New `LintRules` flags follow `DoubleNegation = 64`.
- Each new code is documented in the diagnostics reference and in `DiagnosticCodes`.

## Resolved questions (grilled 2026-10-10)

1. **Canonical-form lint in `All`.** Yes. `All` stays "every lint rule".
2. **Wide chain.** `CompilerOptions` holds the threshold, default 16 operands.
3. **Near the depth limit.** `CompilerOptions` holds a fraction of `MaxDepth`, default 0.75.
4. **Nested findings.** Nothing is dropped. The inner finding is marked as related to the outer one, so a UI can group them.
   This needs a relation field on `Diagnostic`. Ticket 01 adds it, and it must not conflict with the deferred
   `Diagnostic.Properties` of k3-followups 26.

## Out of scope

- Auto-fix. A finding carries a replacement suggestion only.
- A lint that rewrites the rule. Use `Simplify()` and `Canonicalize()`.
- `Diagnostic.Properties` and `ToJsonPointer()`. They stay in [k3-followups 26](../k3-followups/issues/26-diagnostic-properties-and-json-pointer.md), deferred until a consumer asks.

## Further notes

- Every ticket carries XML docs, tests named per CLAUDE.md with a `<summary>`, and the full validation set from CLAUDE.md.
- No predicate or operation is added, so the K3 reference sync does not apply.
