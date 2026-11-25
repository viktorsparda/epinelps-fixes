#!/usr/bin/env bash
# Read-only health check for a local EpinelPS setup.
#
#   ./scripts/check-setup.sh
#
# Verifies the server, launcher, game path, Wine, prefix registry (scheme
# handler + trusted CA), shims and media cache. Prints PASS/FAIL/WARN lines and
# exits non-zero if a required check fails.
set -uo pipefail

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
DATA="${EPINELPS_DATA:-${XDG_DATA_HOME:-$HOME/.local/share}/EpinelPS}"
SHIMS="${EPINELPS_SHIMS:-$ROOT/tools/wine-shims}"

FAIL=0
WARN=0
ok()   { printf '  [ok]   %s\n' "$*"; }
bad()  { printf '  [FAIL] %s\n' "$*"; FAIL=$((FAIL + 1)); }
warn() { printf '  [warn] %s\n' "$*"; WARN=$((WARN + 1)); }

echo "== EpinelPS setup check =="
echo "data: $DATA"

# ---------------------------------------------------------------- server
if [[ -x "$DATA/server/EpinelPS" ]]; then ok "server binary present"; else bad "server binary missing ($DATA/server/EpinelPS)"; fi
if curl -skf --max-time 3 -o /dev/null https://127.0.0.1/ 2>/dev/null; then
    ok "server responds on https://127.0.0.1/"
else
    bad "server not responding (start it; see docs/INSTALL-FROM-SCRATCH.md)"
fi

# ---------------------------------------------------------------- launcher + config
if [[ -x "$DATA/launcher/EpinelPSLauncher" ]]; then ok "launcher binary present"; else bad "launcher binary missing"; fi
CFG="$DATA/launcher/config.json"
GAMEPATH=""
WINEPATH=""
WINETRICKS=""
if [[ -f "$CFG" ]]; then
    ok "launcher config present"
    if command -v python3 >/dev/null 2>&1; then
        mapfile -t CFGVALS < <(python3 - "$CFG" <<'PY'
import json, sys
c = json.load(open(sys.argv[1]))
print(c.get("GamePath", ""))
print(c.get("WinePath", ""))
print(c.get("WineTricksPath", ""))
PY
)
        GAMEPATH="${CFGVALS[0]:-}"
        WINEPATH="${CFGVALS[1]:-}"
        WINETRICKS="${CFGVALS[2]:-}"
    else
        warn "python3 not found; cannot read config.json"
    fi
else
    bad "launcher config missing ($CFG)"
fi

if [[ -n "$GAMEPATH" && -f "$GAMEPATH/NIKKE/game/nikke.exe" ]]; then
    ok "game client found ($GAMEPATH)"
elif [[ -n "$GAMEPATH" ]]; then
    bad "game client not found at $GAMEPATH/NIKKE/game/nikke.exe"
fi

if [[ -n "$WINEPATH" && -x "$WINEPATH" ]]; then
    ok "Wine path exists ($WINEPATH)"
    WINEVER="$(EPINELPS_DATA="$DATA" timeout 15 "$WINEPATH" --version 2>/dev/null || true)"
    if [[ -n "$WINEVER" ]]; then
        echo "         $WINEVER"
        case "$WINEVER" in
            *11.2[2-9]*|*11.[3-9]*|*1[2-9].*) ;;
            *) warn "Wine 11.22 or newer is needed for HTTPS cutscenes" ;;
        esac
    fi
else
    bad "Wine path missing or not executable (set WinePath in config.json)"
fi

if [[ -n "$WINETRICKS" && -x "$WINETRICKS" ]]; then
    ok "winetricks path exists"
elif [[ -n "$WINETRICKS" ]]; then
    warn "winetricks path does not exist; only needed to create a prefix"
fi

if [[ -n "$GAMEPATH" ]]; then
    PREFIX="$GAMEPATH/wine_prefix"
    if [[ -f "$PREFIX/.configured" ]]; then ok "Wine prefix configured"; else warn "Wine prefix not configured yet ($PREFIX)"; fi

    if [[ -f "$PREFIX/system.reg" ]]; then
        if grep -q '9ec4b4f9-3029-45ad-947b-344de2a249e2' "$PREFIX/system.reg"; then
            ok "Urlmon http/https scheme handler registered"
        else
            bad "Urlmon scheme handler missing (see docs/WINE-CUTSCENES.md)"
        fi

        if command -v openssl >/dev/null 2>&1 && [[ -f "$DATA/server/site.pfx" ]]; then
            THUMB="$(openssl pkcs12 -in "$DATA/server/site.pfx" -nokeys -cacerts -passin pass: 2>/dev/null \
                     | openssl x509 -outform der 2>/dev/null | sha1sum | awk '{print toupper($1)}')"
            if [[ -n "$THUMB" ]] && grep -qi "$THUMB" "$PREFIX/system.reg"; then
                ok "server CA trusted in prefix ($THUMB)"
            else
                bad "server CA not trusted in prefix (see docs/WINE-CUTSCENES.md)"
            fi
        else
            warn "openssl or site.pfx missing; cannot verify CA trust"
        fi
    else
        bad "prefix registry missing ($PREFIX/system.reg)"
    fi
fi

# ---------------------------------------------------------------- helpers
if command -v bwrap >/dev/null 2>&1; then ok "bubblewrap present (private hosts)"; else warn "bubblewrap missing; the game will use /etc/hosts"; fi
if [[ -f "$SHIMS/libcloudredirect.so" ]]; then ok "resolver shim built"; else warn "resolver shim not built ($SHIMS/libcloudredirect.so)"; fi
if [[ -f "$SHIMS/libofflinenet.so" ]]; then ok "offline shim built"; else warn "offline shim not built (optional)"; fi

# ---------------------------------------------------------------- media
if [[ -d "$DATA/server/cache/media" ]]; then
    COUNT="$(find "$DATA/server/cache/media" -type f 2>/dev/null | wc -l)"
    if [[ "$COUNT" -gt 0 ]]; then ok "media cache: $COUNT files"; else warn "media cache empty; videos need internet on first play"; fi
else
    warn "media cache directory missing; videos will be fetched from the CDN"
fi

echo
if [[ "$FAIL" -gt 0 ]]; then
    echo "result: $FAIL required check(s) failed, $WARN warning(s)"
    exit 1
fi
echo "result: all required checks passed, $WARN warning(s)"
