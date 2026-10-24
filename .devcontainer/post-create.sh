#!/usr/bin/env bash
# Runs once after the container is created.
set -euo pipefail

bash "$(dirname "${BASH_SOURCE[0]}")/fix-ownership.sh"

host=/mnt/host-claude
dest="$HOME/.claude"

# Link the shareable parts of the read-only host config into the writable ~/.claude.
# settings.json and credentials are left out: they carry Windows paths and secrets.
if [ -d "$host" ]; then
  for item in CLAUDE.md AGENTS.md skills agents commands; do
    if [ -e "$host/$item" ] && [ ! -e "$dest/$item" ]; then
      ln -s "$host/$item" "$dest/$item"
    fi
  done
fi

git config --global --add safe.directory "$PWD"

dotnet tool restore
dotnet restore --locked-mode
