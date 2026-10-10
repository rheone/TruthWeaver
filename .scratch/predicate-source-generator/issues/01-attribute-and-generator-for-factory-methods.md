# 01: Attribute and generator for predicate factory methods

**What to build:** A host marks static predicate methods with an attribute and calls the generated `Register` method. The generator builds each `PredicateSchema` from the method signature.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] The attribute carries the predicate name, label and description
- [ ] The generated `Register` output is equivalent to hand-written registration, proven by a test that compares schemas
- [ ] Unsupported parameter types, duplicate names and wrong return types produce generator diagnostics with tests
- [ ] The generated code passes the AOT and trim analyzer gate
- [ ] A sample or README section shows the registration with and without the generator
- [ ] Public API has XML docs and the full validation set from CLAUDE.md passes, including the K3 reference sync rule if a predicate document changes

See also [spec](../spec.md).
