# 02: Devcontainer auto-register for the rule-text extension

**What to build:** Auto-install `editors/vscode/` into the devcontainer's VS Code Server so it's available on a fresh container without a manual copy or `F5` Extension Development Host.

**Status:** needs-triage — deferred during the 2026-10-24 grilling session. Do not start or dispatch until explicitly triggered.

**Research findings (2026-10-24):**

- `~/.vscode-server/extensions/` exists once the Remote extension host attaches, but is only populated with real extension folders and a matching `extensions.json` entry by the client-driven install step for `customizations.vscode.extensions` — observed roughly 7 minutes after server bootstrap in this container. That step runs after `postCreateCommand` completes, so a symlink dropped by `postCreateCommand` races it and may be ignored or clobbered.
- `postStartCommand` (already used here for `fix-ownership.sh`, and reruns on every start) is safer than `postCreateCommand`, but still races the initial attach on the very first container create.
- Neither `code` nor `code-server` is on `PATH`, but a real `code-server` binary exists at `~/.vscode-server/bin/<commit-hash>/bin/code-server`. It is usable for `--install-extension`, but the commit-hash segment changes with the VS Code Server version, so a script must resolve the path dynamically (e.g. `find ~/.vscode-server/bin -maxdepth 2 -name code-server`) rather than hardcoding it.
- Two mechanisms were evaluated:
  - **(a) Package + install.** Build `editors/vscode` into a `.vsix` and install it via the dynamically-resolved `code-server --install-extension`. Goes through the real install path, so `extensions.json` stays consistent. Preferred if/when this is picked up.
  - **(b) Symlink.** Symlink the folder directly into `extensions/` under a `<publisher>.<name>-<version>` name. Lower effort, but doesn't update `extensions.json` and may need a manual rescan or window reload to be picked up.

**Blocked by:** None (research complete; implementation not started)

- [ ] Extension is installed and recognized by VS Code on a fresh container create, with no manual step (`.truthweaver`/`.tw` files get highlighting; the extension appears in the extensions list)
- [ ] Re-running the install hook on container restart is idempotent (no duplicate installs, no breakage)
- [ ] The chosen mechanism is recorded in `editors/vscode/README.md`
