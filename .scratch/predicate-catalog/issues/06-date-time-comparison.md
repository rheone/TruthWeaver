# 06: Date/time comparison predicates

**What to build:** `DateTimeOffset` comparison predicates the catalog lacks become registerable: `After` (strict `>`), `Before` (strict `<`) and `Between` against `DateTimeOffset` literal arguments. There is no `DateTime` literal kind and no `DateTime` overload; a host with a `DateTime` converts at the selector, and the docs say so (catalog rule). Bounds inclusivity and the reversed-bounds result follow the owner's answers in ticket 02. A null selection returns `Unknown` and honours `NullBehavior`. Each predicate has XML docs, a schema description and README coverage. Clock predicates are a separate ticket.

**Blocked by:** 02

**Status:** ready-for-agent

- [ ] The failing test run is shown before the implementation
- [ ] `After`, `Before` and `Between` are registerable and correct at, just before and just after each boundary, including across offsets
- [ ] Bounds and reversed-bounds behavior match the recorded owner decision
- [ ] Null selections follow the catalog rules and `NullBehavior`
- [ ] No `DateTime` literal kind or overload is added, and the docs explain the host-side conversion
- [ ] README and the gap list show the predicates as present
- [ ] The full validation from CLAUDE.md passes

Source: [gap list, DateTimeOffset section](../k3-gap-list.md). Rules: [CONTEXT.md](../../../CONTEXT.md).
