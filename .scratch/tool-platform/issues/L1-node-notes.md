# L1: Node notes in rule text, JSON and YAML

Scope decisions (owner, 2026-10-11) are in the spec. The worker adds no project, package or dependency beyond them without asking.

**What to build:** Add an optional per-node note that round-trips through the three input forms. Rule text: a `//` line comment and a `/* */` block comment, attached to the node that follows (leading) or the node on the same line (trailing); JSON and YAML: a `note` property on a node (update `rule-tree.schema.json`, the tree readers and the printers). Notes are carried on the compiled tree as a side annotation: they do not affect equality of the evaluation tree, compile results, the BDD or evaluation; rewrites drop them; printers write them when asked (option on `PrintRuleText`, `PrintJson`, `PrintYaml`, default on for lossless conversion, off for canonical comparisons). Write an ADR (amend ADR-0005's notation section in place, dated) first. The `RuleBuilder` gets a way to attach a note. Update the TextMate grammar if ticket 17a has shipped.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] An ADR records the syntax, attachment rule and the decision that notes are not part of tree equality
- [ ] Text, JSON and YAML carry the same notes through every conversion (round-trip tests)
- [ ] A rule with and without notes compiles to equal trees and evaluates identically (property test)
- [ ] Rewrites drop notes, with the behavior documented; printers have the option
- [ ] The page for this feature is written or updated in the same change, `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
