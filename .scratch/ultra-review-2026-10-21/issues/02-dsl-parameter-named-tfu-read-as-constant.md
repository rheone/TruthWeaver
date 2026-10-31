# 02 - A DSL definition parameter named T, F, U, True, False or Unknown is read as a constant; JSON, YAML and the builder read it as the parameter

Status: ready-for-agent
Category: bug
Severity: high
Lens: F2 Parsing; L3 Externals+If+constants
Verdict: CONFIRMED
Blocked by: none

## Finding F2.1: DSL definition parameter named T/F/U/True/False/Unknown is silently read as a constant (JSON reads it as the parameter)

Location: `src/TruthWeaver/Parsing/DslParser.cs:569` (CONFIRMED, high)

ParsePrimary checks the constant keywords (TRUE/FALSE/UNKNOWN at 552-565, T/F/U at 569-585) before it checks the current definition's parameters (688-698). ExpectParameterName (799) accepts any identifier, and DefinitionExpander does not check parameter names: it has no reserved-word or unused-parameter check. So a parameter called t, f, u, true, false or unknown is never referenced, and its argument is silently dropped. TreeFormatReader (237-247) reads the same definition as a ParameterReferenceNode, so the DSL and JSON/YAML give different trees for the same document. rule-text.md:80-81 says each parameter can be used 'wherever a term, constant or sub-expression could appear'.

### Failure scenario

`let gate(t) = t AND isAdult; gate(isBanned)` compiles with no diagnostic to `True AND isAdult`, so a banned adult gets True. The JSON form `{"definitions":{"gate":{"parameters":["t"],"body":{"op":"and","operands":[{"predicate":"t"},{"predicate":"isAdult"}]}}},"op":"use","name":"gate","operands":[{"predicate":"isBanned"}]}` compiles to `isBanned AND isAdult`. The code looks wrong here: either reject reserved parameter names as DefinitionNameCollision, or check parameters before the constants.

### Expected behavior

A parameter name that is a reserved word or T/F/U is a compile error, or the parameter always wins inside the body, the same in every format.

### Verification

I traced `let gate(t) = t AND isAdult; gate(isBanned)` through the code.

1. `ParseDefinition` (DslParser.cs:761-797) reads parameters with `ExpectParameterName` (799-804). That method only requires an Identifier token. It does no reserved-word, T/F/U or duplicate check. The parameter set is stored in `currentParameters` (790).
2. While parsing the body, `ParsePrimary` reaches the bare identifier `t`. It is not followed by `(`, so the T/F/U branch at 569-575 matches first (`IsKeyword("T")`, which is case-insensitive) and returns `ConsumeConstant(True)`.
3. The parameter-reference branch (688-698) comes later, so it is never reached. The same ordering applies to TRUE/FALSE/UNKNOWN at 552-565, which are also checked before the parameter branch.
4. `DefinitionExpander.ExpandUse` (292-299) binds `t` to the argument, but the body holds no `ParameterReferenceNode`, so the argument is dropped. The only checks are argument count (262), cycles, depth, and definition-name reserved words (line 45 checks `definition.Name` only). No parameter names are validated, and the arity check passes. The result is `True AND isAdult` with no diagnostic.
5. TreeFormatReader (237-247) reads the same definition as a `ParameterReferenceNode`, so JSON/YAML give `isBanned AND isAdult`. The DSL and JSON therefore differ for the same document.

The failure needs a parameter named t, f, u, true, false or unknown. A single letter like `t` is a plausible name, so the trigger is realistic. The result is a wrong authorization result with no diagnostic, so I rate it high rather than critical.

## Finding L3.1: DSL reads a definition parameter named T, F or U as a constant; JSON/YAML/builder read it as the parameter

Location: `src/TruthWeaver/Parsing/DslParser.cs:571` (CONFIRMED, high)

ParsePrimary checks the letter constants T/F/U (lines 569-585) before it checks the current definition's parameters (line 688), and ExpectParameterName (799-804) accepts any identifier, so a parameter spelled t, f or u is silently compiled as True/False/Unknown and the use's argument is discarded. TreeFormatReader.ReadNodeCore (237-247) and RuleBuilder.Define take the predicate branch first, so the same document in JSON, YAML or the builder expands the parameter correctly. ADR-0008 decision 4 requires the four forms to give an identical tree, and ADR-0008 decision 6 lists T/F/U only for definition names, not parameters.

### Failure scenario

Rule text `let pick(c, t, f) = c ? t : f; pick(isAdmin, canEdit, canView)` compiles with no diagnostic to `If(isAdmin, True, False)`: with isAdmin True and canEdit False the result is True instead of False. The JSON form `{"definitions":{"pick":{"parameters":["c","t","f"],"body":{"op":"if","operands":[{"predicate":"c"},{"predicate":"t"},{"predicate":"f"}]}}},"op":"use","name":"pick","operands":[...]}` compiles to `If(isAdmin, canEdit, canView)`, so the two formats disagree. The code side is wrong: parameter names need the same reserved-word/letter guard as definition names (or the parameter check must precede the constant check).

### Expected behavior

A parameter named T, F or U is either rejected with DefinitionNameCollision-style diagnostic in every format, or is read as the parameter in rule text exactly as JSON, YAML and RuleBuilder read it, so the compiled tree is identical across formats.

### Verification

Traced in src/TruthWeaver/Parsing/DslParser.cs. ExpectParameterName (799-804) accepts any Identifier, so `let pick(c, t, f) = ...` declares parameters t and f. Line 790 puts them in currentParameters, case-insensitively. In ParsePrimary, the T/F/U check (569-585) runs when the identifier is not followed by '('. IsKeyword is the same case-insensitive check used for the other keywords. In the body `c ? t : f`, `t` is followed by ':' and `f` by ';', so each returns ConsumeConstant at 573-583 and ParsePrimary returns early. The parameter-reference branch (688-698) is never reached, so the parameter becomes a constant and the use's argument is dropped. DefinitionExpander.cs:45 applies the reserved-word check (DslParser.IsReservedWord, ReservedWords.cs includes "T") to definition names only. No parameter name is checked for reserved words in the expander, so no DefinitionNameCollision or other diagnostic appears. I did not re-read TreeFormatReader or RuleBuilder. The cross-format divergence rests on the claim. The DSL half is traced in full: it gives a silent wrong tree, `If(isAdmin, True, False)`. Severity is high rather than critical. The trigger needs a parameter literally named t, f or u, which is plausible for ternary-style definitions. The failure is a silent wrong decision, but it is limited to the DSL and has a simple workaround of renaming the parameter.

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.

## Comments

> *This was generated by AI during triage.*

Decision: reject the name at compile time, not "parameter wins over the constant." A parameter named `t`/`f`/`u`/`true`/`false`/`unknown` (case-insensitive) becomes a compile error in every format, the same family as the existing reserved-word check on definition names — rather than changing DSL parse order so the parameter shadows the constant inside its own body.

## Agent Brief

**Category:** bug
**Summary:** A DSL definition parameter named `t`, `f`, `u`, `true`, `false` or `unknown` (case-insensitive) is silently read as that constant instead of a parameter reference, so the argument bound to it is dropped with no diagnostic; JSON, YAML and the builder read the same definition correctly as a parameter reference, so the four authoring formats disagree on the compiled tree for the same document.

**Current behavior:** Declaring a definition parameter with one of these reserved-letter/word spellings compiles successfully in DSL text with no diagnostic. Inside the body, every reference to that parameter name is read as the corresponding constant, not the parameter, so the use's argument for that position is silently discarded. The same document read as JSON, YAML, or built with `RuleBuilder` expands the parameter correctly, so the DSL gives a different (wrong) compiled tree for cross-format-identical input.

**Desired behavior:** Declaring a definition parameter named `t`, `f`, `u`, `true`, `false` or `unknown` (case-insensitive) is a compile error, in every authoring format (DSL, JSON, YAML, builder) — consistent with how a reserved word is already rejected for a *definition's* own name. No format silently drops an argument for this reason.

**Key interfaces:**
- Parameter-name validation for a definition's parameter list — needs the same reserved-word rejection that definition names already get, extended to parameter names, applied uniformly across every format that declares parameters (DSL parser, JSON/YAML tree reader, `RuleBuilder`'s definition-building API).
- A new or reused diagnostic code in the same family as the existing definition-name-collision/reserved-word diagnostic.

**Acceptance criteria:**
- [ ] `let gate(t) = t AND isAdult; gate(isBanned)` (and the JSON/YAML/builder equivalents) fail to compile with a diagnostic naming the reserved parameter, in every format — none of them silently compile.
- [ ] The diagnostic identifies the offending parameter name and points at its declaration.
- [ ] A parameter name that is merely a prefix or contains one of these words (e.g. `truthy`, `total`) is unaffected — only an exact case-insensitive match to `t`/`f`/`u`/`true`/`false`/`unknown` is rejected.
- [ ] The validation list in `CLAUDE.md` passes.

**Out of scope:**
- Changing how T/F/U/True/False/Unknown are read as constants anywhere else (term position, definition names, etc.) — this is scoped to parameter declarations only.
- Making the parameter shadow the constant instead of rejecting it (considered and explicitly not chosen).

### Resolution

`DefinitionExpander` rejects a parameter spelled `t`, `f`, `u`, `true`, `false` or `unknown` (case-insensitive) with `DefinitionNameCollision` (TRE0037), on the definition's name span. The check sits beside the existing duplicate-parameter check, so DSL, JSON, YAML and `RuleBuilder` all get it. Tests: `DefinitionsTests` and `RuleBuilderDefinitionsTests`. Docs: `docs/rule-text.md`, `docs/strong-k3/specification/diagnostics.md`.
