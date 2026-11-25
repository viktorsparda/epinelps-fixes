#!/usr/bin/env bash
# Build server and launcher and pack them into a release tarball under out/.
#
#   ./scripts/package-release.sh
#
# The tarball contains the built server, the built launcher, the helper tools,
# docs and scripts. It does NOT contain the game client, saves, accounts, Wine,
# a prefix, or the media cache; see docs/INSTALL-FROM-SCRATCH.md.
set -euo pipefail

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
STAMP="$(date -u '+%Y%m%d')"
NAME="epinelps-fixes-$STAMP-linux-x64"

"$ROOT/scripts/build-server.sh"
"$ROOT/scripts/build-launcher.sh"

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
STAGE="$WORK/$NAME"
mkdir -p "$STAGE"

cp -a "$ROOT/out/server"   "$STAGE/server"
cp -a "$ROOT/out/launcher" "$STAGE/launcher"
cp -a "$ROOT/tools"        "$STAGE/tools"
cp -a "$ROOT/docs"         "$STAGE/docs"
cp -a "$ROOT/scripts"      "$STAGE/scripts"
cp "$ROOT/LICENSE" "$ROOT/NOTICE.md" "$ROOT/README.md" "$STAGE/"

tar -czf "$ROOT/out/$NAME.tar.gz" -C "$WORK" "$NAME"
( cd "$ROOT/out" && sha256sum "$NAME.tar.gz" > "$NAME.tar.gz.sha256" )

echo "== $ROOT/out/$NAME.tar.gz ($(du -sh "$ROOT/out/$NAME.tar.gz" | cut -f1)) =="
echo "   $(cat "$ROOT/out/$NAME.tar.gz.sha256")"
