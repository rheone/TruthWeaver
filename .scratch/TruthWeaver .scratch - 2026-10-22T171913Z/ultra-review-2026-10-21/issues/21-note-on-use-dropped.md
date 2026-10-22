# 21 - A note written on a use node is dropped when the use expands

Status: needs-triage
Severity: medium
Lens: L3 Externals+If+constants
Verdict: PLAUSIBLE
Blocked by: none

## Finding L3.3: A note written on a `use` node is dropped when the use expands

Location: `src/TruthWeaver/Compilation/DefinitionExpander.cs:299` (PLAUSIBLE, medium)

ExpandUse returns ExpandNode(definition.Body, ...) and never carries the UseNode's own Note onto the expanded root (Carry is applied only to the body's nodes), so the note is lost before NoteCollector runs. rule-tree.schema.json lists `note` on useNode, rule-formats.md:209 says any JSON node can have one and rule-formats.md:16 promises `parse(print(x))` keeps the same notes; a use whose body root has no note of its own loses the annotation with no diagnostic.

### Failure scenario

JSON `{"definitions":{"adult":{"body":{"predicate":"isAdult"}}},"op":"use","name":"adult","note":"age gate"}` (or rule text `let adult = isAdult; /* age gate */ adult`) compiles, but CompiledRule.Notes is empty and PrintJson()/PrintRuleText() of the compiled rule no longer contain 'age gate'. The code side is wrong: the use's note should land on the expanded root (or the loss should be documented and the key rejected).

### Expected behavior

A note on a use node is kept on the node the use expands to (or joined with that node's own note), so CompiledRule.Notes and the printers preserve it.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
