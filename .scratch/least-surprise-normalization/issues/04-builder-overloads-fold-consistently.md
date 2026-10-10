# 04: RuleBuilder overloads follow one rule

**What to build:** A caller gets the same result from `And(params ...)` and `And(IEnumerable<...>)`. Where an identity exists (`And`, `Or`, `Any`, `All`, `None`) both fold: empty is the identity constant and one operand is itself. Where none exists (`ExactlyOne`, `Coalesce`, `Parity`, thresholds) both reject a short list with a compile diagnostic. `GreaterThan` and `LessThan` gain the `IEnumerable` overload that `AtLeast`, `AtMost` and `Exactly` have. `Xnor` stays.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] One test per operator proves the array and list forms give the same compiled rule or the same diagnostic
- [ ] The XML example that shows array and list differing is removed
- [ ] An unsupported literal type reaches the caller as a diagnostic, not an `ArgumentException` from `Compile`
- [ ] `docs/rulebuilder.md` states the rule
- [ ] The breaking change is recorded in `CHANGELOG.md` with a migration step
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
