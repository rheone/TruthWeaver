# 44 - The generator does not detect argument names that differ only in case; Register throws at run time

Status: needs-triage
Severity: low
Lens: F8 Predicate+DataSource concepts
Verdict: PLAUSIBLE
Verified 2026-10-24: reproduced - a [Predicate] method with parameters `role` and `Role` (default "x") produced zero generator diagnostics and zero compiler errors from PredicateRegistrationGenerator, while PredicateRegistryBuilder.RejectCaseVariantArguments (PredicateRegistryBuilder.cs:109-122) still throws ArgumentException when the generated Register call reaches it, so the collision surfaces only at run time.
Blocked by: none

## Finding F8.6: The generator does not detect argument names that differ only in case; Register then throws at run time

Location: `src/TruthWeaver.Generators/PredicateMethodReader.cs:114` (PLAUSIBLE, low)

C# parameter names are case-sensitive, so a [Predicate] method can declare string role and string Role. The generator emits both as arguments with no diagnostic. PredicateRegistryBuilder.RejectCaseVariantArguments then throws ArgumentException when Register runs. GeneratorDiagnostics.cs:6-7 and docs/predicate-source-generator.md:132 say every problem is a build error and never reaches run time.

### Failure scenario

[Predicate("hasRole","Has role","...")] static TruthValue HasRole(User u, string role, string Role = "x"). The project builds. OrderPredicates.Register(builder) throws ArgumentException at host startup.

### Expected behavior

Add a TWG diagnostic for argument names that collide when case is ignored, and register no method that has one.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
