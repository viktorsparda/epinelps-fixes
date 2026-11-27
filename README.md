# EpinelPS — personal fork

An unofficial fork of [EpinelPS](https://github.com/EpinelPS/EpinelPS) (base
commit [`11fce36`](https://github.com/EpinelPS/EpinelPS/commit/11fce367effe04c14e029cc67419f03c5a9afbdf))
with local server fixes. It bundles the **server**, the **desktop launcher**,
the pinned Windows helper binaries, setup scripts and documentation.

The original project README is kept as [README.upstream.md](README.upstream.md).
This fork is not affiliated with the EpinelPS developers.

> **AI disclosure:** the changes in this fork were developed with AI assistance
> and tested by running the server against the 153.6.10 client. Upstream
> EpinelPS forbids AI-generated contributions, so this fork is **not** for
> upstream pull requests. Review the persistence and reward code before relying
> on it.

## Quick start

Prerequisites: Linux x86_64, .NET 10 SDK, Wine 11.22+, `winetricks`, and the
official game client. See [what you need](#what-you-need) below.

```bash
./scripts/build-server.sh
./scripts/build-launcher.sh
./scripts/install.sh

sudo setcap cap_net_bind_service=+ep \
  "${EPINELPS_DATA:-$HOME/.local/share/EpinelPS}/server/EpinelPS"
./scripts/server.sh start
./scripts/run-launcher.sh
```

## Simple guide

1. **Build** the server and launcher with the two `build-*.sh` scripts.
2. **Install** both into `~/.local/share/EpinelPS` with `install.sh`. Re-running
   it keeps saves, accounts and settings.
3. **Install the game** through official channels. Put the folder that contains
   `NIKKE/game/nikke.exe` wherever you like, then set `GamePath` to it in the
   launcher settings (`config.json`).
4. **Set Wine and winetricks** in the same settings: `WinePath` to a Wine 11.22+
   binary, `WineTricksPath` to `winetricks`. The launcher creates the prefix on
   first launch.
5. **Create an account** with the admin API (the launcher only logs in, it does
   not register):
   ```bash
   curl -sk 'https://127.0.0.1/adminapi/RegisterAccount?seq=1' \
     -H 'Content-Type: application/json' \
     -d '{"Email":"you@local","Password":"choose-a-password"}'
   ```
   Then add the same email/password in the launcher with server `127.0.0.1`.
6. **Fix cutscenes** — follow [WINE-CUTSCENES.md](docs/WINE-CUTSCENES.md). It is
   four steps: Wine 11.22, Urlmon scheme handlers, CA trust, and the resolver
   shim. The shim is loaded automatically by `run-launcher.sh`.
7. **Launch** with `./scripts/run-launcher.sh`, not the binary directly.

Full details, ordering and troubleshooting:
[**docs/INSTALL-FROM-SCRATCH.md**](docs/INSTALL-FROM-SCRATCH.md).

## What you need

| Needed | Source |
|---|---|
| **NIKKE PC client** | official installer; you must own the game, it is not redistributable |
| **Wine 11.22+** | your system or a portable build |
| **winetricks + cabextract** | distribution packages (needed to create the prefix) |
| **.NET 10 SDK** | to build the server and launcher |
| **bubblewrap** (recommended) | distribution package; gives the game a private hosts file |
| ~30 GiB disk | client asset store, plus ~3 GB server cache once media is warm |

## What is bundled

| Path | Contents |
|---|---|
| `EpinelPS/` | server source with the fixes listed below |
| `ServerSelector/` | upstream server-selector tools |
| `launcher/` | unmodified EpinelPSLauncher source at pinned `d219c64` |
| `vendor/launcher-windows/` | the 4 Windows helper binaries the launcher needs |
| `vendor/server-data/` | `gamecommon.json`, required at server startup |
| `scripts/` | build, install, server control, launcher wrapper, checks |
| `tools/` | resolver/offline shims, CA-trust helper, Media Foundation probe |
| `docs/` | installation and Wine cutscene guides |

## Changes in this fork

- **Crash fixes** — the console loop no longer spins on EOF (it burned a core
  and wrote ~640 KB/s to `server.log`); the newbie-package handler no longer
  makes the client throw.
- **Character fixes** — limit break / core upgrade use one grade helper
  (`CharacterGradeHelper`): exact spare-body validation, caps, no crossing
  from limit break into core, consistent Tid/grade in gacha duplicates and
  spare bodies; saved grades are normalized at startup. Owned costumes are
  returned instead of every costume in static data.
- **Event fixes** — the event-shop lookup resolves the real shop, including
  the original shops of archived events; box gacha is implemented
  (price/probability/reward tables, server-authoritative rolls) and rerun
  costume boxes drop a costume the commander already owns (the official
  nine-draw rule); challenge stages have enter/clear/fast-clear handlers,
  item tickets (3 granted once) and first-clear/repeat rewards; anomalous
  intercept returns non-null rewards; read mail is persisted.
- **Archive** — stage rewards are paid on the first clear; the six archive
  event-quest episodes work (activate, clear, stage clear, claim once,
  reset); every `/archive/minigame/*` route answers with its own response
  type, with real state for Red Ash NKS (content manager 2), the Volt Road
  dice board, tower defense, text adventure, battle simulator, PlaySoda and
  island adventure (fishing, shop, photos, missions, step-up rewards; also
  the live `/event/minigame/islandadventure/*` routes). The archive field
  and DateSim handlers persist their state.
- **Missing handlers** — character toggles, battle-log team types, logout,
  archive minigame JSON.
- **Persistence** — new per-user fields and collections, all defaulting to
  empty so older `db.json` saves still load.
- **Admin / testing** — optional `EPINELPS_HTTP_PORT` / `EPINELPS_HTTPS_PORT`
  test ports (defaults stay 80/443), a `resolve-name` command, and fixes for
  `add-locale` paths on Linux and path arguments in `RunCmd`.
- **Asset routes** — `/prdenv`, `/PC` and `/media` accept `HEAD`.

## Scripts

| Script | Purpose |
|---|---|
| `scripts/build-server.sh` | build the server to `out/server` |
| `scripts/build-launcher.sh` | build the bundled launcher to `out/launcher` |
| `scripts/install.sh` | install both, preserving saves and config |
| `scripts/server.sh` | `start` / `stop` / `status` / `restart` |
| `scripts/run-launcher.sh` | launcher wrapper with the environment it needs |
| `scripts/check-setup.sh` | read-only health check |
| `scripts/package-release.sh` | build a release tarball under `out/` |
| `scripts/regression.py` | server-contract regression suite (sandboxed copy on port 8443, safe beside a running server) |
| `tools/wine-shims/build.sh` | build the resolver and offline shims |
| `tools/trust-epinelps-ca.py` | write the CA trust blob into a prefix |

## Documentation

| Document | Contents |
|---|---|
| [INSTALL-FROM-SCRATCH.md](docs/INSTALL-FROM-SCRATCH.md) | end-to-end setup, dependencies, accounts, troubleshooting |
| [WINE-CUTSCENES.md](docs/WINE-CUTSCENES.md) | the four fixes that make story videos play |
| [README.upstream.md](README.upstream.md) | original project README |

## Build manually

```bash
dotnet publish EpinelPS/EpinelPS.csproj -c Release -r linux-x64 --self-contained true -o out/server
dotnet publish launcher/EpinelPSLauncher/EpinelPSLauncher.csproj -c Release -r linux-x64 --self-contained true -o out/launcher
```

Then copy the four binaries from `vendor/launcher-windows/` next to the launcher
binary; `scripts/build-launcher.sh` does this for you.

The server binds ports 80 and 443. Grant the capability once (or run as root):

```bash
sudo setcap cap_net_bind_service=+ep "$EPINELPS_DATA/server/EpinelPS"
```

## Notes and limits

- Tested against the **153.6.10** client. A different client version needs a
  matching `TargetVersion`/`gameconfig.json` and static data.
- `scripts/regression.py` checks the server contract of every fix against a
  sandboxed copy of an existing install (it needs your installed static data
  and an admin account: `EPINELPS_ADMIN_EMAIL` / `EPINELPS_ADMIN_PASSWORD`,
  default `me@local` / `password`). CI only builds. Some client-side issues
  remain, e.g. a failed fishing cast in the archive island adventure freezes
  the client.
- Nothing auto-updates. Server, content and client updates are manual; the
  client and assets are never redistributed here.
- The git history contains one **local-only** commit that redirects a single
  media file to loopback HTTP (a Wine workaround). Drop it for a clean build.
- Launcher tokens expire after ~2 days; re-add the account in the launcher.

## License

GNU Affero General Public License v3.0 — see [LICENSE](LICENSE). The bundled
launcher has its own AGPL `LICENSE` in `launcher/`. Modification and
third-party notices are in [NOTICE.md](NOTICE.md).
