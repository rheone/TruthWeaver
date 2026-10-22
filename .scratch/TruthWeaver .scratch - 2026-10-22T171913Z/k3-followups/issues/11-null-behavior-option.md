# 11: NullBehavior option on built-in predicates

**What to build:** The string, regex and collection predicate factories accept a null-handling option (default False, keeping today's behaviour) that lets a host choose Unknown for a null selected value. Findings: research item 7.1.

**Blocked by:** 10

**Status:** done

- [x] Default behaviour unchanged for every existing member
- [x] Unknown option returns Unknown with no fault, tested for each factory
- [x] README documents the option
- [x] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

Source: [research findings](../../k3-conformance/research-findings.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

Added public `NullBehavior` enum (`False` default, `Unknown`) and an optional trailing `nullBehavior` parameter on `StringPredicates` (Equals, EqualsIgnoreCase, StartsWith, EndsWith, Contains, EqualsConfigurable), `RegexPredicates.Matches` and `CollectionPredicates.SetEquals`. `IsNullOrEmpty` is a null test and takes no option. For `SetEquals`, only a null collection becomes Unknown; an empty one is still compared as a set. Tests in `NullBehaviorTests`; README documents the option.
