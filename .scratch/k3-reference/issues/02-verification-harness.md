# 02: Verification harness for the reference

**What to build:** An automated check that keeps the documentation honest. It extracts the truth tables, evaluation tables and canonical forms from the Markdown reference and verifies them against an independent Strong K3 oracle over all T/F/U inputs (operand counts up to 4 for variadic operations), reports mismatches with file and line, checks that every operation has exactly one primary category, an arity, domains and a Kind, and validates relative links. Reuse the existing K3 test oracle where it fits; place the check where the repository already runs its gates (a test or a documented script) so CI can run it. Choose the smallest design and record the choice in the ticket Comments.

**Blocked by:** 01

**Status:** done

- [x] A deliberately wrong table, canonical form or link in a fixture is detected and reported with file and line
- [x] The harness is runnable locally and wired into the gates the repo already uses (or documented why not)
- [x] It adds no new package dependency without a recorded reason
- [ ] It passes on the (initially empty) reference and on the skeleton from ticket 04

Source: the Phase 8 validation list in the brief. See also [spec](../spec.md).

## Comments

- 2026-10-03: Design choice (smallest that fits): an xUnit test, so `dotnet test` (and therefore the Husky `test` task and CI) runs it with no new script or package. `K3ReferenceChecker` (tests/TruthWeaver.Tests/ReferenceDocs) mirrors the existing `DocExampleChecker`: marker comments (`k3:truth`, `k3:eval`, `k3:canonical`) above tables and fenced blocks, failures returned as `path:line: message` so fixtures can prove it fails. The oracle is the existing `K3Oracle`, not the engine; `K3Operation.cs` binds the 27-operation inventory (name, category directory, Kind, parameters, valid parameter ranges) to it, and `K3Expression.cs` evaluates canonical forms (function-call expressions, `...` splices operands, `+`/`-` on integers) over every assignment, operand count and valid parameter. It also checks operation documents (inventory membership, directory, Kind, `Category:` line equals the directory's label, required sections non-empty, table sections carry a verifying marker) and relative links including heading anchors. 16 tests: the real tree plus deliberately wrong fixtures (cell, missing row, evaluation result, canonical form, parity form valid only for 2 operands, missing file, missing anchor, Kind, category, inventory, missing marker). Conventions are documented in docs/doc-examples.md. No new dependency.
- Assumptions recorded for later tickets: the `Category:` label per directory is `Gates / Operators`, `Derived Logical Operations` (PROPOSAL open question 1A), `Cardinality Functions`, `Functions`, `Result Transformations`; canonical forms use function-call syntax (`NOT(XOR(a, b))`, not `NOT XOR(a, b)`); the threshold family accepts 1 operand (open question 10A). The last acceptance criterion (passes on the ticket 04 skeleton) is checked off in ticket 04.
- Validation: restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze (results in the commit's run).
