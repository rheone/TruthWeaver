# Dev image for TruthWeaver: Alpine, .NET 11 SDK. Keep the tag in step with global.json (the CI "devcontainer" job checks it).
# The tag is literal, not an ARG, so Dependabot can read it.
FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.1-alpine3.24

ARG POWERSHELL_VERSION=7.6.8
ARG USERNAME=vscode
ARG USER_UID=1000
ARG USER_GID=1000

# PowerShell on musl needs ICU, so the SDK image's invariant-globalization default is turned off.
# Chromium is only for mermaid-cli; Puppeteer must not download its own glibc build.
# DOTNET_ROLL_FORWARD=LatestMajor: this image only carries the .NET 11 RC runtime. Several
# framework-dependent global/local tools (husky, csharp-ls) are still built against net8/9/10
# and have no rollForward of their own, so without this they fail with "You must install or
# update .NET" even though a newer runtime is present. dotnet-coverage and reportgenerator
# already set their own rollForward and are unaffected.
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_ROLL_FORWARD=LatestMajor \
    PUPPETEER_SKIP_DOWNLOAD=true \
    PUPPETEER_EXECUTABLE_PATH=/usr/local/bin/chromium-no-sandbox

RUN apk add --no-cache \
        bash zsh git openssh-client gnupg less curl ca-certificates tzdata \
        ripgrep github-cli jq yq nodejs npm chromium graphviz font-noto \
        icu-libs icu-data-full krb5-libs libgcc libintl libssl3 libstdc++ \
        ncurses-terminfo-base userspace-rcu zlib lttng-ust shadow sudo \
        zoxide zoxide-zsh-completion

# PowerShell is not in the Alpine repositories; take the official musl build.
RUN mkdir -p /opt/microsoft/powershell/7 \
    && curl -fsSL "https://github.com/PowerShell/PowerShell/releases/download/v${POWERSHELL_VERSION}/powershell-${POWERSHELL_VERSION}-linux-musl-x64.tar.gz" \
        | tar -xz -C /opt/microsoft/powershell/7 \
    && chmod +x /opt/microsoft/powershell/7/pwsh \
    && ln -s /opt/microsoft/powershell/7/pwsh /usr/local/bin/pwsh

# PowerShell is the container's default shell; chsh requires it to be a listed login shell.
RUN echo /usr/local/bin/pwsh >> /etc/shells

# CodeGraph bundles a glibc-only node runtime, and gcompat is not enough for it. Install real glibc beside musl
# from the community sgerrand package (unofficial source; the key below is the one the project publishes).
ARG GLIBC_VERSION=2.35-r1
RUN curl -fsSL -o /etc/apk/keys/sgerrand.rsa.pub https://alpine-pkgs.sgerrand.com/sgerrand.rsa.pub \
    && curl -fsSL -o /tmp/glibc.apk "https://github.com/sgerrand/alpine-pkg-glibc/releases/download/${GLIBC_VERSION}/glibc-${GLIBC_VERSION}.apk" \
    && apk add --no-cache --force-overwrite /tmp/glibc.apk \
    && rm /tmp/glibc.apk \
    && mkdir -p /lib64 \
    && ln -sf /usr/glibc-compat/lib/ld-linux-x86-64.so.2 /lib64/ld-linux-x86-64.so.2 \
    && echo "/usr/glibc-compat/lib" > /etc/ld.so.conf

# The glibc node also needs a glibc build of libstdc++ and libgcc_s; take them from Ubuntu 22.04, which matches the glibc 2.35 above.
COPY --from=ubuntu:22.04 /usr/lib/x86_64-linux-gnu/libstdc++.so.6* /usr/lib/x86_64-linux-gnu/libgcc_s.so.1 /usr/glibc-compat/lib/

RUN npm install --global @mermaid-js/mermaid-cli @colbymchenry/codegraph@1.6.2

# Chromium's sandbox does not work in the container, and the repo tools call `mmdc` with fixed arguments, so they cannot
# pass a Puppeteer config. A wrapper adds the flag instead.
RUN printf '#!/bin/sh\nexec /usr/bin/chromium-browser --no-sandbox "$@"\n' > /usr/local/bin/chromium-no-sandbox \
    && chmod +x /usr/local/bin/chromium-no-sandbox

# PowerShell is the login/default shell; zsh remains available as an alternate profile.
RUN addgroup -g ${USER_GID} ${USERNAME} \
    && adduser -D -u ${USER_UID} -G ${USERNAME} -s /usr/local/bin/pwsh ${USERNAME} \
    && echo "${USERNAME} ALL=(root) NOPASSWD:ALL" > /etc/sudoers.d/${USERNAME} \
    && mkdir -p /home/${USERNAME}/.nuget /home/${USERNAME}/.claude /home/${USERNAME}/.dotnet/tools \
    && chown -R ${USERNAME}:${USERNAME} /home/${USERNAME}

USER ${USERNAME}
ENV PATH="${PATH}:/home/${USERNAME}/.dotnet/tools"

# Library release tooling. Repo tools (csharpier, husky, roslynator, stryker, dotnet-outdated)
# come from dotnet-tools.json via `dotnet tool restore`.
RUN dotnet tool install --global dotnet-reportgenerator-globaltool \
    && dotnet tool install --global dotnet-coverage

# oh-my-posh prompt, themed with project path, git status and the active .NET SDK version
# (theme.omp.json). Installed onto the existing PATH (/usr/local/bin) so every shell sees it
# without extra profile wiring. Wired into both PowerShell (the default shell) and zsh (the alternate).
RUN mkdir -p /home/${USERNAME}/.config/oh-my-posh \
    && curl -fsSL https://ohmyposh.dev/install.sh | sudo bash -s -- -d /usr/local/bin
COPY --chown=${USERNAME}:${USERNAME} oh-my-posh.omp.json /home/${USERNAME}/.config/oh-my-posh/theme.omp.json
RUN mkdir -p "$(dirname "$(pwsh -NoProfile -Command 'echo $PROFILE')")" \
    && echo 'oh-my-posh init pwsh --config ~/.config/oh-my-posh/theme.omp.json | Invoke-Expression' \
        >> "$(pwsh -NoProfile -Command 'echo $PROFILE')" \
    && echo 'eval "$(oh-my-posh init zsh --config ~/.config/oh-my-posh/theme.omp.json)"' >> /home/${USERNAME}/.zshrc

# zoxide: a frecency-based `z`/`zi` alternative to `cd`, useful for jumping between the repo's
# many projects (src/tests/samples/benchmarks each hold a dozen+ sibling folders).
RUN echo '(zoxide init powershell | Out-String) | Invoke-Expression' >> "$(pwsh -NoProfile -Command 'echo $PROFILE')" \
    && echo 'eval "$(zoxide init zsh)"' >> /home/${USERNAME}/.zshrc

WORKDIR /workspaces/TruthWeaver
CMD ["pwsh"]
