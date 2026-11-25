#!/usr/bin/env bash
# Build the two LD_PRELOAD shims used for local cutscene playback and for
# offline testing. Needs gcc and libc headers.
set -euo pipefail
HERE="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
CC="${CC:-gcc}"

"$CC" -O2 -Wall -shared -fPIC -o "$HERE/libcloudredirect.so" "$HERE/cloudredirect.c" -ldl
"$CC" -O2 -Wall -shared -fPIC -o "$HERE/libofflinenet.so" "$HERE/offline-net.c" -ldl

echo "built $HERE/libcloudredirect.so"
echo "built $HERE/libofflinenet.so"
