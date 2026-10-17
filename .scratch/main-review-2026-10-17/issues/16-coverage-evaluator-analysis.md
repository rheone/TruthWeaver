# 16: Coverage of evaluator, analysis, definitions and printer branches

**Parent:** [main review](../spec.md)

**Status:** ready-for-agent

**What to build:** Cover the untested branches with scenario tests.

**Evidence:** Evaluator.cs 95.2% (404-410, 606-613, 701-708); RuleCoverageAccumulator.cs 90.8% (484-505, 325-435); DefinitionExpander.cs 81.6% (136-171); ChangeSetSearch.cs 84.2% (242-262); CanonicalPrinter.cs 92.1% (472-483, 710-738, 277, 761, 784, 799, 815). Confirm what each range is before writing tests.

- [ ] Each file reaches 95%, or the remaining lines are shown to be unreachable guards
- [ ] Docs for any changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
