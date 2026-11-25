#!/usr/bin/env bash
# Run the installed EpinelPS launcher with the environment it needs:
#   * EPINELPS_DATA so the launcher's tools find wine-path and the hosts file
#   * a private hosts file for the game (bubblewrap), so /etc/hosts is untouched
#   * the .NET ICU fallback for minimal systems
#   * optional LD_PRELOAD shims for local cutscenes / offline testing
#
#   ./scripts/run-launcher.sh
#   EPINELPS_OFFLINE=1 ./scripts/run-launcher.sh   # block non-loopback connects
#   EPINELPS_REDIRECT=0 ./scripts/run-launcher.sh  # stream media from the CDN
#
# The launcher must be reachable at $EPINELPS_DATA/launcher/EpinelPSLauncher.
set -euo pipefail

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
DATA="${EPINELPS_DATA:-${XDG_DATA_HOME:-$HOME/.local/share}/EpinelPS}"
LAUNCHER_DIR="$DATA/launcher"
LAUNCHER="$LAUNCHER_DIR/EpinelPSLauncher"
HOSTS="$DATA/hosts"
SHIMS="${EPINELPS_SHIMS:-$ROOT/tools/wine-shims}"

[[ -x "$LAUNCHER" ]] || {
    echo "run-launcher: launcher not found: $LAUNCHER" >&2
    echo "               run ./scripts/build-launcher.sh && ./scripts/install.sh" >&2
    exit 1
}

# .NET needs ICU unless globalization-invariant mode is on.
if [[ -z "${DOTNET_SYSTEM_GLOBALIZATION_INVARIANT:-}" ]]; then
    if ! { ldconfig -p 2>/dev/null | grep -q 'libicuuc\.so'; } && \
       ! ls /usr/lib*/libicuuc.so* /usr/lib/*/libicuuc.so* >/dev/null 2>&1; then
        export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
    fi
fi

export EPINELPS_DATA="$DATA"

# Optionally load the resolver shim (local media over TLS) and the offline shim.
PRELOAD=""
if [[ "${EPINELPS_REDIRECT:-1}" != 0 && -f "$SHIMS/libcloudredirect.so" ]]; then
    PRELOAD="$SHIMS/libcloudredirect.so"
fi
if [[ "${EPINELPS_OFFLINE:-0}" = 1 ]]; then
    [[ -f "$SHIMS/libofflinenet.so" ]] || {
        echo "run-launcher: EPINELPS_OFFLINE=1 but $SHIMS/libofflinenet.so is missing" >&2
        echo "              build it with $ROOT/tools/wine-shims/build.sh" >&2
        exit 1
    }
    PRELOAD="${PRELOAD:+$PRELOAD:}$SHIMS/libofflinenet.so"
fi
if [[ -n "$PRELOAD" ]]; then
    export LD_PRELOAD="${PRELOAD}${LD_PRELOAD:+:$LD_PRELOAD}"
fi

# Build the private hosts file the game sees through bwrap: the real /etc/hosts
# plus the launcher's ServerSelector block kept from last time.
prepare_hosts() {
    command -v bwrap >/dev/null 2>&1 || return 1
    bwrap --dev-bind / / --ro-bind /etc/hosts /etc/hosts -- true 2>/dev/null || return 1
    local block="" tmp
    tmp="$(mktemp "${TMPDIR:-/tmp}/epinelps-hosts.XXXXXX")" || return 1
    [[ -f "$HOSTS" ]] && block="$(sed -n '/^# begin ServerSelector entries/,/^# end ServerSelector entries/p' "$HOSTS")"
    {
        sed '/^# begin ServerSelector entries/,/^# end ServerSelector entries/d' /etc/hosts
        [[ -n "$block" ]] && printf '\n%s\n' "$block"
    } > "$tmp" || { rm -f "$tmp"; return 1; }
    if mv -f "$tmp" "$HOSTS" 2>/dev/null; then
        export EPINELPS_HOSTS_FILE="$HOSTS"
        return 0
    fi
    rm -f "$tmp"
    if [[ -r "$HOSTS" ]]; then
        export EPINELPS_HOSTS_FILE="$HOSTS"
        return 0
    fi
    return 1
}

if ! prepare_hosts; then
    echo "warning: could not prepare $HOSTS (bubblewrap missing or denied)." >&2
    echo "         The game will use the system /etc/hosts." >&2
fi

cd "$LAUNCHER_DIR"
exec ./EpinelPSLauncher "$@"
