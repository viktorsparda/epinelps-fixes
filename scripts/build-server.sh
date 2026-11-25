#!/usr/bin/env bash
# Build the EpinelPS server (self-contained, linux-x64) into out/server.
#
#   ./scripts/build-server.sh
#   OUT=/some/dir ./scripts/build-server.sh
#
# Needs the .NET 10 SDK on PATH (or set DOTNET=/path/to/dotnet). The first
# build restores NuGet packages, which needs internet.
set -euo pipefail

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
DOTNET="${DOTNET:-dotnet}"
OUT="${OUT:-$ROOT/out/server}"

die() { echo "build-server: $*" >&2; exit 1; }

command -v "$DOTNET" >/dev/null 2>&1 || die "dotnet not found; install the .NET 10 SDK or set DOTNET=/path/to/dotnet"
[[ -f "$ROOT/EpinelPS/EpinelPS.csproj" ]] || die "server project not found under $ROOT/EpinelPS"

mkdir -p "$OUT"
echo "== building server with $("$DOTNET" --version) =="
"$DOTNET" publish "$ROOT/EpinelPS/EpinelPS.csproj" \
    -c Release -r linux-x64 --self-contained true \
    -p:DebugType=None -p:DebugSymbols=false \
    -o "$OUT"

echo "== built $OUT ($(du -sh "$OUT" | cut -f1)) =="
echo "Install with: ./scripts/install.sh"
