# 10: Root glossary

**What to build:** A reader of the project finds new and confusing vocabulary in one place. `docs/glossary.md` is an alphabetical list with a one-sentence definition per term and a link to the page that defines it. It covers project-wide terms, including terms outside the K3 reference (for example predicate, term, Decision, fault, rewrite, rule format, data source, trace) and K3 terms that a first-time reader meets (for example Operation, Kind, Strong Kleene connective, external operator, strongest extension). Definitions keep the meanings already in `CONTEXT.md` and `specification/terminology.md` and add no rules. The root `README.md` replaces its Glossary section with a link to the page, and its links into `docs/strong-k3/` are updated for the files the cleanup removes or renames. Nothing else in `README.md` changes in this ticket.

**Blocked by:** 02

**Status:** ready-for-agent

- [ ] `docs/glossary.md` follows the documentation standard and its terms agree with `CONTEXT.md` and `terminology.md`
- [ ] Every K3 term links into `docs/strong-k3/`, and no K3 page links back to the glossary or to `README.md`
- [ ] The README Glossary section is a link, and every README link into `docs/strong-k3/` resolves
- [ ] The doctest checks on `README.md` and `CONTEXT.md` still pass
- [ ] The lint test passes, with `docs/glossary.md` outside the baseline
