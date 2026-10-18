# 11b-4: rule and call in JSON, YAML and RuleBuilder

**Parent:** [11b](11b-rule-library.md)

**What to build:** The same two references in the JSON and YAML rule trees (with `rule-tree.schema.json`) and in `RuleBuilder` (`RuleBuilder.Rule(name, hash)` and `RuleBuilder.Call(name, hash)`), compiling to the same `Expression` tree as rule text. The tree-format writers, printers and `OperatorDefinitions` entries cover both.

**Blocked by:** 11b-3

**Status:** on-hold (do not start or dispatch until the owner releases the sub-tickets)

- [ ] A reference round-trips through rule text, JSON, YAML and the builder to equal trees
- [ ] `rule-tree.schema.json` accepts a valid reference and rejects a malformed one
- [ ] The pages are updated in the same change (JSON and YAML formats, builder), `CHANGELOG.md` has an entry, and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
