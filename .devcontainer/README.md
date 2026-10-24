# Dev container

This folder defines the TruthWeaver development container. It is an Alpine Linux image with the .NET 11 SDK and the tools that the repository scripts need. Use it to build, test and format the project without installing the toolchain on your machine.

| File                                         | Purpose                                                     |
| :------------------------------------------- | :---------------------------------------------------------- |
| [Dockerfile](Dockerfile)                     | Builds the image.                                           |
| [devcontainer.json](devcontainer.json)       | Tells VS Code how to build and open the container.          |
| [compose.yaml](compose.yaml)                 | Runs the same image from a terminal, without VS Code.       |
| [post-create.sh](post-create.sh)             | Runs once after the container is created.                   |

## Requirements

- Docker (tested with Docker 29).
- For the VS Code route: the Dev Containers extension.

## Start the container

### VS Code

1. Open the repository folder.
2. Run **Dev Containers: Reopen in Container** from the command palette.
3. Wait for the first build and for `post-create.sh` to finish.

### Terminal

```bash
docker compose -f .devcontainer/compose.yaml run --rm dev
```

Set `CLAUDE_HOME` first if your host `.claude` folder is not at `~/.claude`. On Windows, use `%USERPROFILE%\.claude`.

## Check your work

Run the validation steps from the repository `CLAUDE.md` inside the container:

```bash
dotnet restore --locked-mode
dotnet build
dotnet test
dotnet csharpier check .
dotnet format --verify-no-changes --severity info
dotnet roslynator analyze TruthWeaver.slnx --ignore-compiler-diagnostics
```

## Inventory

| Tool                    | Version               | Source                                      |
| :---------------------- | :-------------------- | :------------------------------------------ |
| .NET SDK                | 11.0.100-rc.1         | Base image `mcr.microsoft.com/dotnet/sdk`   |
| Alpine Linux            | 3.24                  | Base image                                  |
| zsh (default shell)     | Alpine package        | `apk`                                       |
| bash, git, openssh, gnupg, less | Alpine packages | `apk`                                     |
| ripgrep (`rg`)          | Alpine package        | `apk`                                       |
| GitHub CLI (`gh`)       | Alpine package        | `apk`                                       |
| PowerShell (`pwsh`)     | 7.6.8                 | GitHub release, musl build                  |
| jq, yq                  | Alpine packages       | `apk`                                       |
| Node.js and npm         | Alpine packages       | `apk`. Needed by `scripts/format-data.cs`.  |
| mermaid-cli (`mmdc`)    | npm latest at build   | npm. Uses Alpine Chromium with `--no-sandbox` (see below). |
| Graphviz (`dot`)        | Alpine package        | `apk`. Image export in the CLI and TUI.     |
| Noto fonts              | Alpine package        | `apk`. Text in rendered images.             |
| CodeGraph               | 1.6.2                 | npm. Uses the glibc layer below.            |
| ReportGenerator         | Latest at build       | .NET global tool                            |
| dotnet-coverage         | Latest at build       | .NET global tool                            |
| Repository tools        | See `dotnet-tools.json` | Restored by `post-create.sh`              |

The repository tools are CSharpier, Husky.Net, Roslynator, Stryker, dotnet-outdated and csharp-ls.

## Mounts and volumes

| Path in the container     | Type                  | Content                                              |
| :------------------------ | :-------------------- | :--------------------------------------------------- |
| `/workspaces/TruthWeaver` | Workspace             | The repository.                                      |
| `~/.nuget`                | Named volume          | NuGet cache. It survives rebuilds.                   |
| `~/.claude`               | Named volume          | Writable Claude Code state: sessions and sign-in.    |
| `/mnt/host-claude`        | Bind mount, read-only | Your host `~/.claude` folder.                        |

`post-create.sh` links `CLAUDE.md`, `AGENTS.md`, `skills`, `agents` and `commands` from `/mnt/host-claude` into `~/.claude`. It does not link `settings.json` or credentials. Those files can hold Windows paths and secrets.

> [!NOTE]
> Sign in to Claude Code once inside the container. The sign-in is stored in the `~/.claude` volume, not on your host.

## Update the image

- **SDK:** change the tag in the `FROM` line of the Dockerfile and `global.json` together. The `devcontainer` CI job fails if they differ.
- **PowerShell:** change the `POWERSHELL_VERSION` build argument.
- **CodeGraph:** change the version in the `npm install` line.
- **Rebuild:** run **Dev Containers: Rebuild Container**, or `docker compose -f .devcontainer/compose.yaml build`.

Dependabot watches the base image tag. It cannot change `global.json`.

## Known limits

- **Alpine uses musl, not glibc.** Native binaries built for glibc can fail. CodeGraph is one: its bundled runtime needs glibc. The Dockerfile installs the community `sgerrand` glibc package next to musl, and copies `libstdc++` and `libgcc_s` from Ubuntu 22.04. This is an unofficial source. If it breaks after an update, switch the base image to a Debian slim image.
- **Diagram images.** The CLI and TUI call `mmdc` and `dot` from the `PATH` with fixed arguments (`TWT0016` if one is missing). Chromium cannot use its sandbox in the container, so `PUPPETEER_EXECUTABLE_PATH` points to `/usr/local/bin/chromium-no-sandbox`, a wrapper that adds `--no-sandbox`. Do not run untrusted Mermaid input through it on a shared host.
- **Image size.** Chromium for mermaid-cli is the largest single addition. Remove it from the Dockerfile if you do not render diagrams.
- **x64 only.** The PowerShell download and the glibc layer target `x86_64`. An ARM host needs changes to both.
- **Globalization.** The image turns off .NET invariant globalization, because PowerShell needs ICU. Culture-sensitive behavior matches a normal machine.
- **Not covered by the tests.** The repository test suite does not check this folder. Only the `devcontainer` CI job builds it.

## Troubleshooting

| Symptom                                           | Cause and fix                                                                               |
| :------------------------------------------------ | :------------------------------------------------------------------------------------------ |
| `codegraph: node: not found`                      | The glibc layer is missing. Rebuild the image without cache.                                |
| Bind mount error for `.claude`                    | The host folder does not exist. Create it, or set `CLAUDE_HOME` for compose.                |
| Git reports "dubious ownership"                   | Run `git config --global --add safe.directory "$PWD"`. `post-create.sh` does this for you.  |
| Restore fails in `--locked-mode`                  | Lock files are out of date. Restore without the flag, then commit the updated lock files.   |
| Cannot write to `~/.nuget`                        | The new volume is root-owned. Run `sudo chown -R vscode:vscode ~/.nuget`.                   |
