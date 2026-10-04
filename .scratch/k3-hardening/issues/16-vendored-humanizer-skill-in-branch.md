# 16: Decide whether the vendored humanizer skill belongs on this branch

**What to build:** `git diff main...HEAD` adds `.agents/skills/humanizer/` (nine files: a third-party skill, its LICENSE, a GitHub workflow `validate.yml`, `validate-package.py`, plugin manifests). It is unrelated to the Strong K3 work and widens the merge review ([report](../07-review-report.md), finding 6). Confirm it is intentional. Options: (1) keep it but move it to its own commit or PR (recommended); (2) drop it from the branch; (3) keep as is. Verify that nothing in CI or Husky reads `.agents/`.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [ ] The owner picks 1, 2 or 3
- [ ] The licence terms of the vendored files are acceptable for the repository
- [ ] `skills-lock.json` matches whatever is kept

See also [spec](../spec.md).

## Comments

- 2026-10-03: Owner chose to remove the skill from the repo and keep it at user level. Work tracked by repo-hygiene 03.
- 2026-10-04: Owner reversed this: the humanizer skill stays committed in the repo (`.claude/skills/humanizer`). repo-hygiene 03 was removed.
