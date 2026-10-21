# 32 - Registry and generator accept predicate and collection names that rule text cannot express

Status: needs-triage
Severity: medium
Lens: F8 Predicate+DataSource concepts
Verdict: PLAUSIBLE
Blocked by: none

## Finding F8.3: Registry and generator accept predicate and collection names that rule text cannot express, so a JSON-compiled rule prints unparsable DSL

Location: `src/TruthWeaver/Registry/PredicateRegistryBuilder.cs:124` (PLAUSIBLE, medium)

AddDescriptor rejects only reserved words and case duplicates. It does not check that the name is a DSL identifier (Lexer.cs:33-40: a letter or '_' first, then letters, digits and '_'). Empty names and names with '-', '.' or spaces register. TreeFormatReader.ReadTerm accepts any string, and CanonicalPrinter writes the name verbatim. AddCollection (line 89) has no reserved-word or identifier check at all. The generator (PredicateRegistrationGenerator.Distinct) checks only reserved words.

### Failure scenario

Register a predicate named "has-role". CompileJson({"predicate":"has-role"}) succeeds. CompiledRule.PrintRuleText() emits has-role, which re-parses as has - role and fails. RuleFormat promises that all three formats round-trip without loss. AddCollection("and", ...) registers a binding that no FORALL(x IN and, ...) can name, with no error.

### Expected behavior

Reject a predicate or collection name that is not a DSL identifier, or is reserved, at registration (ArgumentException), and add a matching generator diagnostic.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
