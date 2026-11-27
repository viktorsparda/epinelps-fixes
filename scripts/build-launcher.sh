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
#
# launcher/ stays an unmodified upstream copy. The fork's launcher patches in
# patches/launcher-*.patch are applied to a temporary copy (out/launcher-src)
# before building; today that is launcher-private-hosts.patch, which makes the
# Linux launcher write its redirect block to the private hosts file prepared
# by scripts/run-launcher.sh instead of /etc/hosts (no pkexec prompt, and the
# official client's name resolution is untouched).
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

# Patch a scratch copy so the bundled source is never modified.
BUILD_SRC="$ROOT/out/launcher-src"
rm -rf "$BUILD_SRC"
mkdir -p "$BUILD_SRC"
cp -a "$SRC/." "$BUILD_SRC/"
rm -rf "$BUILD_SRC/EpinelPSLauncher/bin" "$BUILD_SRC/EpinelPSLauncher/obj"
PATCHES=()
for patch_file in "$ROOT"/patches/launcher-*.patch; do
    [[ -f "$patch_file" ]] || continue
    if command -v patch >/dev/null 2>&1; then
        patch --quiet --forward -p1 -d "$BUILD_SRC" < "$patch_file" \
            || die "cannot apply $(basename "$patch_file")"
    elif command -v git >/dev/null 2>&1; then
        git -C "$BUILD_SRC" apply --quiet "$patch_file" \
            || die "cannot apply $(basename "$patch_file")"
    else
        die "need 'patch' or 'git' to apply $(basename "$patch_file")"
    fi
    PATCHES+=("$(basename "$patch_file")")
    echo "== applied $(basename "$patch_file") =="
done

echo "== building launcher ($COMMIT) =="
rm -rf "$OUT"
"$DOTNET" publish "$BUILD_SRC/EpinelPSLauncher/EpinelPSLauncher.csproj" \
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
    echo "patches=${PATCHES[*]:-none}"
    echo "built=$(date -u '+%Y-%m-%dT%H:%M:%SZ')"
    echo "dotnet=$("$DOTNET" --version)"
    echo "runtime=linux-x64 self-contained"
} > "$OUT/BUILD-INFO"

echo "== built $OUT ($(du -sh "$OUT" | cut -f1)) =="
echo "Install with: ./scripts/install.sh"
