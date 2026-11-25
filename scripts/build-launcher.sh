#!/usr/bin/env bash
# Build the bundled EpinelPSLauncher (self-contained, linux-x64) into
# out/launcher, including the pinned Windows helper binaries.
#
#   ./scripts/build-launcher.sh
#   OUT=/some/dir ./scripts/build-launcher.sh
#
# Needs the .NET 10 SDK on PATH (or set DOTNET=/path/to/dotnet). The launcher
# is cross-built from the bundled source; the Windows helpers cannot be built
# on Linux and come from vendor/launcher-windows/.
set -euo pipefail

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
DOTNET="${DOTNET:-dotnet}"
OUT="${OUT:-$ROOT/out/launcher}"
SRC="$ROOT/launcher"
HELPERS="$ROOT/vendor/launcher-windows"

die() { echo "build-launcher: $*" >&2; exit 1; }

command -v "$DOTNET" >/dev/null 2>&1 || die "dotnet not found; install the .NET 10 SDK or set DOTNET=/path/to/dotnet"
[[ -f "$SRC/EpinelPSLauncher/EpinelPSLauncher.csproj" ]] || die "bundled launcher source missing under $SRC"
[[ -d "$HELPERS" ]] || die "missing pinned Windows helpers: $HELPERS"
( cd "$HELPERS" && sha256sum -c --quiet SHA256SUMS ) || die "pinned Windows helper hashes do not match"

COMMIT="$(cat "$SRC/UPSTREAM-COMMIT" 2>/dev/null || echo unknown)"

echo "== building launcher ($COMMIT) =="
rm -rf "$OUT"
"$DOTNET" publish "$SRC/EpinelPSLauncher/EpinelPSLauncher.csproj" \
    -c Release -r linux-x64 --self-contained true \
    -p:DebugType=None -p:DebugSymbols=false \
    -o "$OUT"

# Required at runtime by LaunchGame (Wine path); not produced by a Linux build.
install -m 0755 "$HELPERS/LauncherUtility.exe" "$OUT/LauncherUtility.exe"
install -m 0644 "$HELPERS/HelperDll.dll"         "$OUT/HelperDll.dll"
install -m 0644 "$HELPERS/UnityInit.dll"         "$OUT/UnityInit.dll"
install -m 0644 "$HELPERS/sail_api_impl64.dll"   "$OUT/sail_api_impl64.dll"

find "$OUT" -name '*.pdb' -delete

{
    echo "source=EpinelPSLauncher"
    echo "commit=$COMMIT"
    echo "built=$(date -u '+%Y-%m-%dT%H:%M:%SZ')"
    echo "dotnet=$("$DOTNET" --version)"
    echo "runtime=linux-x64 self-contained"
} > "$OUT/BUILD-INFO"

echo "== built $OUT ($(du -sh "$OUT" | cut -f1)) =="
echo "Install with: ./scripts/install.sh"
