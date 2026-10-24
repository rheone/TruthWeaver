#!/usr/bin/env bash
# Fixes ownership of the volumes and the workspace bind mount, both of which can arrive
# owned by root (volumes on first mount; the workspace mount from whatever process
# populates it before the container hands control to this user).
# Runs on every container create and start. .tmp/ is reference-only build output and is
# left untouched.
set -euo pipefail

sudo chown -R "$(id -u):$(id -g)" "$HOME/.nuget" "$HOME/.claude" 2>/dev/null || true

workspace="$PWD"
sudo chown "$(id -u):$(id -g)" "$workspace" 2>/dev/null || true
sudo find "$workspace" -mindepth 1 -path "$workspace/.tmp" -prune -o -exec chown "$(id -u):$(id -g)" {} + 2>/dev/null || true
