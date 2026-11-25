# Install from scratch

This repository bundles the **server source**, the **desktop launcher source**,
the pinned Windows launcher helpers, setup scripts and documentation. It does
**not** contain the game client, game assets, your saves/accounts, Wine, a Wine
prefix, or the media cache. This guide covers a Linux x86_64 host.

If you just want the short version:

```bash
./scripts/build-server.sh
./scripts/build-launcher.sh
./scripts/install.sh
./scripts/server.sh start
./scripts/run-launcher.sh
```

The rest explains the parts that need decisions or third-party software.

## What you supply vs what is bundled

| Needed | Where it comes from |
|---|---|
| Server binary | built here (`scripts/build-server.sh`) |
| Launcher binary + Windows helpers | built here (`scripts/build-launcher.sh`), helpers pinned in `vendor/launcher-windows/` |
| `gamecommon.json` | bundled in `vendor/server-data/`; copied by `install.sh` |
| Test TLS certificate (`site.pfx`) | bundled with the source; regenerable with `generate_ssl_cert.sh` |
| Helper scripts / docs | bundled (`scripts/`, `docs/`, `tools/`) |
| **NIKKE PC client** | official installer — you must own the game; not redistributable |
| **Wine 11.22+** | your system or a portable build |
| **winetricks + cabextract** | your distribution's package manager |
| **bubblewrap** (recommended) | your distribution (private hosts file) |
| **.NET 10 SDK** | your distribution / Microsoft |
| Saves, accounts, media cache | created locally on first use |

Disk: a few GB for builds, ~30 GiB for the client asset store, ~3 GB for the
server cache once media is warmed.

## 1. Build and install

```bash
# .NET 10 SDK must be on PATH (or set DOTNET=/path/to/dotnet)
./scripts/build-server.sh      # -> out/server
./scripts/build-launcher.sh    # -> out/launcher
./scripts/install.sh           # -> $EPINELPS_DATA (default ~/.local/share/EpinelPS)
```

`install.sh` keeps existing `server/gameconfig.json`, `server/gameversion.json`,
`server/db.json`, `server/epinelps.db`, `server/logs/`, `launcher/config.json`
and `launcher/tools/`. It is safe to re-run after rebuilding.

Defaults used everywhere:

```bash
DATA=${EPINELPS_DATA:-${XDG_DATA_HOME:-$HOME/.local/share}/EpinelPS}
```

## 2. Install the game client

Install the official NIKKE PC client yourself. You need this layout:

```
<GamePath>/NIKKE/game/nikke.exe
```

`GamePath` is a field in `$DATA/launcher/config.json` (or the launcher's
Settings dialog). The Wine prefix is created at `<GamePath>/wine_prefix`, so
that directory must be writable.

Version matters: `$DATA/server/gameconfig.json` ships with
`"TargetVersion": "153.6.10"` and `gameversion.json` describes the same build.
A different client version needs a matching server config. Either install the
matching client, or set `TargetVersion` to your client's version and run the
server's `update-server` command (this fetches current static data from the
official CDN). Mixing client and server versions can leave the client stuck or
missing data.

The launcher can show a Download/Update button; it works through the local
server (`www.jupiterlauncher.com` is mapped to `127.0.0.1`) and the
`gameversion.json` fixture. It only downloads the version described by that
fixture, so it is not a general "get the latest client" tool.

## 3. Wine and the prefix

Requirements:

- **Wine 11.22 or newer.** Wine 11.0 cannot open the HTTPS cutscene URLs; see
  [WINE-CUTSCENES.md](WINE-CUTSCENES.md).
- **winetricks** and **cabextract** on PATH.

Set these in `$DATA/launcher/config.json` (or the Settings dialog):

```json
"GamePath": "/path/to/game-root",
"GameResourcePath": "/home/you/.config/Unity",
"WinePath": "/path/to/wine-11.22/bin/wine",
"WineTricksPath": "/usr/bin/winetricks"
```

On first launch with no prefix at `<GamePath>/wine_prefix`, the launcher runs
`winetricks corefonts` and `winetricks quartz` and creates the prefix. That
needs internet and winetricks; if it fails, create the prefix manually:

```bash
WINEPREFIX=<GamePath>/wine_prefix winetricks -q corefonts quartz
touch <GamePath>/wine_prefix/.configured
```

If the game window stays black or crashes in D3D11, install DXVK into the
prefix (for example `WINEPREFIX=<GamePath>/wine_prefix winetricks -q dxvk`) or
use a Wine/Proton build that ships DXVK. This is the least automated part of
the setup; it depends on your GPU driver and Wine build.

## 4. TLS, the CDN hostname, and cutscenes

Follow [WINE-CUTSCENES.md](WINE-CUTSCENES.md) in full. Short version, after the
prefix exists and with no Wine process running:

```bash
# 1. Build the LD_PRELOAD shims
./tools/wine-shims/build.sh

# 2. Use Wine's Urlmon handler for http:/https:
WINEPREFIX=<GamePath>/wine_prefix wineserver -k
WINEPREFIX=<GamePath>/wine_prefix wine reg import tools/wine-shims/scheme-handlers.reg

# 3. Trust the CA that signed the server's site.pfx
openssl pkcs12 -in "$DATA/server/site.pfx" -nokeys -cacerts -passin pass: -out /tmp/epinelps-ca.pem
python3 tools/trust-epinelps-ca.py /tmp/epinelps-ca.pem > /tmp/epinelps-ca.reg
WINEPREFIX=<GamePath>/wine_prefix wine reg import /tmp/epinelps-ca.reg
```

Then always start the launcher through the wrapper, which loads the resolver
shim (and an optional offline shim):

```bash
./scripts/run-launcher.sh
# media from the real CDN instead of the local cache:
EPINELPS_REDIRECT=0 ./scripts/run-launcher.sh
# simulate offline (requires libofflinenet.so):
EPINELPS_OFFLINE=1 ./scripts/run-launcher.sh
```

If you skip the wrapper, cutscenes fall back to the real CDN and offline play
will not work.

## 5. Server and accounts

Start the server (it needs ports 80/443):

```bash
sudo setcap cap_net_bind_service=+ep "$DATA/server/EpinelPS"   # once, or run as root
./scripts/server.sh start
./scripts/server.sh status
./scripts/check-setup.sh
```

`server.sh start` uses a stdin FIFO on purpose: the console loop spins at EOF
and would burn a core and fill `server.log`.

Create the first account with the admin API. On an empty database this works
without a token, and it stores the same MD5 hash the launcher sends:

```bash
curl -sk 'https://127.0.0.1/adminapi/RegisterAccount?seq=1' \
  -H 'Content-Type: application/json' \
  -d '{"Email":"you@local","Password":"choose-a-password"}'
```

Then add that email/password in the launcher with server `127.0.0.1`.

Do **not** use the public `/account/register` endpoint: it stores the raw
password, while the launcher hashes the password before calling
`/account/login`, so the login will fail. Once an account exists, further
registrations need an authenticated admin call; the admin panel is at
`https://127.0.0.1/admin/` (self-signed certificate, `me@local` in a typical
setup).

Launcher account tokens expire every ~2 days; re-add the account if login
stops working. Tokens are per account, not per installation.

## 6. First login and downloads

With the server running and an account in the launcher, press Launch. On the
first run the server needs internet to fetch `StaticData.pack`; after that the
server caches what the client requests under `$DATA/server/cache/`:

- `prdenv/...` — patch packs (client updates)
- `media/...` — cutscene videos and subtitles
- `staticdata/...` — static data pack

Once the cache is warm the setup works offline (see
[WINE-CUTSCENES.md](WINE-CUTSCENES.md) and `EPINELPS_OFFLINE=1`).

## 7. Updating later

Nothing auto-updates. To move to a newer game version, set `TargetVersion` in
`$DATA/server/gameconfig.json`, restart the server with internet, and run the
built-in `update-server` command through the admin API; it fetches the current
static data and resource URLs. Then let the client patch. Back up
`$DATA/server/db.json` first, and test with a throwaway account.

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| `server.sh start` says cap_net_bind_service missing | run the `setcap` line above (or start as root) |
| Login fails while the server is up | wrong account/token; tokens expire after ~2 days |
| Lobby loads, cutscene is black | Wine version/Wine handler/CA/shims not set up — see [WINE-CUTSCENES.md](WINE-CUTSCENES.md) |
| `StaticData download fail` | server has no internet on first run, or `gameconfig.json` points at an unreachable URL |
| Game starts but renders black/crashes | DXVK missing in the prefix, or GPU driver issue |
| Launcher says no Wine | `WinePath` in `config.json` is wrong or not executable |
| Launcher starts with no accounts | create the first account via `adminapi/RegisterAccount` (section 5); the launcher only logs in |
| `gameserver` unreachable after reboot | the server does not autostart; run `./scripts/server.sh start` |

## What is not included, and legal notes

- **Game client and assets are not redistributed here.** You must own and
  install the game through official channels. Do not publish game files.
- Saves/accounts (`db.json`, `epinelps.db`) are yours and are not in the repo.
- `site.pfx` is a test certificate; do not reuse it for anything real.
- The bundled launcher source is unmodified AGPL code from
  `EpinelPS/EpinelPSLauncher`; see `launcher/BUNDLED.md` and
  `vendor/launcher-windows/README.md`.
- Upstream EpinelPS does not accept AI-generated contributions; this fork is
  not proposed upstream (see `README.md`).
