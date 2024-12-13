# syntax=docker/dockerfile:1

ARG DOTNET_VERSION=7.0
ARG DEBIAN_VERSION=bookworm

FROM mcr.microsoft.com/devcontainers/dotnet:1-${DOTNET_VERSION}-${DEBIAN_VERSION}

RUN <<EOT
    set -eu

    DEBIAN_FRONTEND=noninteractive

    apt-get update
    apt-get install -y \
        dos2unix

    apt-get autoremove -y
    apt-get clean
    rm -rf /var/lib/apt/lists/*
EOT

ENTRYPOINT ["/bin/bash"]
