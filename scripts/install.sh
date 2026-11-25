#!/usr/bin/env bash
# Install a built server and launcher into the EpinelPS data directory.
#
#   ./scripts/build-server.sh
#   ./scripts/build-launcher.sh
#   ./scripts/install.sh
#
# Data directory: $EPINELPS_DATA, else $XDG_DATA_HOME/EpinelPS, else
# ~/.local/share/EpinelPS.
#
# Installs over build-produced files only. It preserves:
#   server/gameconfig.json, server/gameversion.json  (your target version/URLs)
#   server/db.json, server/epinelps.db, server/logs  (saves, accounts, logs)
#   launcher/config.json, launcher/tools/            (accounts, Wine paths)
set -euo pipefail

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
DATA="${EPINELPS_DATA:-${XDG_DATA_HOME:-$HOME/.local/share}/EpinelPS}"
SERVER_OUT="${SERVER_OUT:-$ROOT/out/server}"
LAUNCHER_OUT="${LAUNCHER_OUT:-$ROOT/out/launcher}"

die() { echo "install: $*" >&2; exit 1; }

[[ -x "$SERVER_OUT/EpinelPS" ]] || die "server build not found at $SERVER_OUT; run ./scripts/build-server.sh"
[[ -x "$LAUNCHER_OUT/EpinelPSLauncher" ]] || die "launcher build not found at $LAUNCHER_OUT; run ./scripts/build-launcher.sh"

mkdir -p "$DATA/server" "$DATA/launcher"

# ---------------------------------------------------------------- server
TMPKEEP="$(mktemp -d)"
for f in gameconfig.json gameversion.json; do
    if [[ -f "$DATA/server/$f" ]]; then cp -a "$DATA/server/$f" "$TMPKEEP/$f"; fi
done

cp -a "$SERVER_OUT/." "$DATA/server/"

for f in gameconfig.json gameversion.json; do
    if [[ -f "$TMPKEEP/$f" ]]; then cp -a "$TMPKEEP/$f" "$DATA/server/$f"; fi
done
rm -rf "$TMPKEEP"
chmod +x "$DATA/server/EpinelPS"

# Required at runtime but not produced by dotnet publish.
if [[ ! -f "$DATA/server/gamecommon.json" ]]; then
    if [[ -f "$ROOT/vendor/server-data/gamecommon.json" ]]; then
        install -m 0644 "$ROOT/vendor/server-data/gamecommon.json" "$DATA/server/gamecommon.json"
        echo "installed gamecommon.json"
    else
        echo "warning: vendor/server-data/gamecommon.json missing; the server will not start" >&2
    fi
fi

# ---------------------------------------------------------------- launcher
TMPCFG="$(mktemp)"
HAVE_CFG=0
if [[ -f "$DATA/launcher/config.json" ]]; then
    cp -a "$DATA/launcher/config.json" "$TMPCFG"
    HAVE_CFG=1
fi

cp -a "$LAUNCHER_OUT/." "$DATA/launcher/"
if [[ "$HAVE_CFG" = 1 ]]; then cp -a "$TMPCFG" "$DATA/launcher/config.json"; fi
rm -f "$TMPCFG"
chmod +x "$DATA/launcher/EpinelPSLauncher"

echo "== installed server   -> $DATA/server =="
echo "== installed launcher -> $DATA/launcher =="
echo
echo "Next:"
echo "  1. Point the launcher at Wine and winetricks (Settings, or config.json):"
echo "       WinePath      -> your Wine 11.22 binary (or a wrapper)"
echo "       WineTricksPath-> winetricks"
echo "       GamePath      -> the directory containing NIKKE/game/nikke.exe"
echo "  2. Follow docs/INSTALL-FROM-SCRATCH.md for the Wine, TLS and cutscene setup."
echo "  3. Start the server:  ./scripts/server.sh start"
echo "  4. Check everything:  ./scripts/check-setup.sh"
