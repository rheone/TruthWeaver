# 78 - Printing rule text that holds rule("x") as a JSON or YAML tree throws

Status: done
Category: bug
Severity: medium
Lens: Follow-up to ticket 11b-2
Blocked by: none

## Finding

Ticket 11b-2 made `rule("x")` parse as a library reference. `RuleDefinitions.ReadText(...)` reads such text, but the raw tree emitter has no JSON or YAML shape for a library reference yet (that is ticket 11b-4). `RuleDefinitions.PrintJson()` and `YamlRuleDefinitions.PrintYaml()` on that text throw `InvalidOperationException` ("Unhandled rule node type"). Before 11b-2, the same text was a syntax error that `ReadText` reported as a diagnostic, so callers such as the tool's definitions printer never saw a throw.

## Agent Brief

**Category:** bug
**Summary:** Make `RuleDefinitions.PrintJson()` and `PrintYaml()` return `null` ("the text was not read") for a document that holds a library reference, instead of throwing, until ticket 11b-4 gives the reference a tree shape.

**Current behavior:** `ReadText("rule(\"x\")")` succeeds, and `PrintJson()` throws `InvalidOperationException` from the raw tree emitter.

**Desired behavior:** `WriteTree` returns `false` when the parsed document contains a library reference at any depth (the rule, a `let` body, a quantifier body), so `PrintJson` and `PrintYaml` return `null`, which their documentation already describes. A document without a reference prints as before.

**Acceptance criteria:**
- [x] A test fails before the fix and passes after: `ReadText` of text with a library reference, then `PrintJson()`, returns `null` and does not throw. The same for a reference nested inside a `let` body and a `FORALL` body.
- [x] Printing a document with no reference is unchanged.
- [x] The XML docs of `PrintJson` and `PrintYaml` say that a document with a library reference is not printed until the JSON and YAML shape exists.
- [x] The validation list in CLAUDE.md passes.

**Out of scope:** The JSON and YAML shape of `rule` and `call` (ticket 11b-4).

## Resolution (2026-11-01)

`RuleDefinitions.WriteTree` returns `false` when the parsed document holds a library reference at any depth. It reuses `DefinitionExpander.HasLibraryReference`, which walks the rule, the `let` bodies and the quantifier bodies. `PrintJson()` and `YamlRuleDefinitions.PrintYaml()` therefore return `null` and do not throw. Their XML docs say that a document with a library reference is not printed until the JSON and YAML shape exists (ticket 11b-4). CHANGELOG entry under Fixed.

Test: `RuleDefinitionsTests.PrintJson_DocumentWithLibraryReference_ReturnsNullInsteadOfThrowing_Test` (a theory: the bare reference, a pinned reference under `AND`, a reference in a `let` body, a reference in a `FORALL` body; each asserts that `PrintJson()` and `PrintYaml()` are `null`). Red first: all four cases failed with `InvalidOperationException: Unhandled rule node type 'TruthWeaver.Parsing.LibraryReferenceNode'`. The existing print tests (no reference) pass unchanged.
