# 73 - A definition parameter named after a DSL keyword (`and`, `not`, `if`, `any` ...) is not checked

Status: needs-triage
Category: bug
Severity: unverified
Lens: F2 Parsing
Blocked by: none

## Finding

Follow-up to [02](02-dsl-parameter-named-tfu-read-as-constant.md). Ticket 02 rejects a definition parameter spelled as a constant (`t`, `f`, `u`, `true`, `false`, `unknown`). It left every other reserved word alone, as its scope required.

`ParsePrimary` in `src/TruthWeaver/Parsing/DslParser.cs` checks keywords before it checks the enclosing definition's parameters. A parameter spelled like any other entry in `ReservedWords` (`and`, `or`, `not`, `if`, `any`, `all`, `none`, `between`, `coalesce`, `exactly`, `let`, ...) may therefore be read as the keyword in the body, not as the parameter. Depending on the keyword the result may be a syntax error or a dropped argument. JSON, YAML and `RuleBuilder` read the same name as a parameter.

Not verified: the exact behavior per keyword has not been traced or tested. Triage first.

## Questions for triage

- Which keywords silently change the tree, and which only produce a parse error?
- Reject every `ReservedWords` entry as a parameter name (reuse `DslParser.IsReservedWord`, same `DefinitionNameCollision` family as 02), or only those that change the tree?

## Acceptance criteria

- Triage records the per-keyword behavior and a decision.
- A test fails before the fix and passes after, in every format.
- The page that describes the behavior states the current truth.
- The validation list in CLAUDE.md passes.
